using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new ForbiddenException("Invalid email or password.");
        if (!user.IsActive)
            throw new ForbiddenException("This account has been disabled.");

        var token = jwt.CreateToken(user.Id, user.Email, user.IsSuperAdmin);
        return new AuthResultDto(token, user.Id, user.Email, user.DisplayName, user.IsSuperAdmin);
    }
}
