using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Application.Features.Auth;

// ---- Forgot password: issue token + email a reset link ----

public record ForgotPasswordCommand(string Email, string ResetUrlBase) : IRequest;

public class ForgotPasswordCommandHandler(IAppDbContext db, IEmailService email)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
        // Do not reveal whether the email exists.
        if (user is null) return;

        user.PasswordResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(2);
        await db.SaveChangesAsync(ct);

        var link = $"{request.ResetUrlBase.TrimEnd('/')}/reset-password?token={user.PasswordResetToken}&email={Uri.EscapeDataString(user.Email)}";
        var body = $"""
            <p>برای بازنشانی گذرواژهٔ حساب SmartCall خود روی پیوند زیر کلیک کنید (تا ۲ ساعت معتبر است):</p>
            <p>To reset your SmartCall password, click the link below (valid for 2 hours):</p>
            <p><a href="{link}">{link}</a></p>
            """;
        await email.SendAsync(user.Email, "SmartCall — بازنشانی گذرواژه / Password reset", body, ct);
    }
}

// ---- Reset password with token ----

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Token).NotEmpty();
    }
}

public class ResetPasswordCommandHandler(IAppDbContext db, IPasswordHasher hasher)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct)
            ?? throw new NotFoundException("Invalid reset request.");

        if (user.PasswordResetToken is null
            || !string.Equals(user.PasswordResetToken, request.Token, StringComparison.Ordinal)
            || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new ForbiddenException("Reset link is invalid or has expired.");

        user.PasswordHash = hasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        await db.SaveChangesAsync(ct);
    }
}
