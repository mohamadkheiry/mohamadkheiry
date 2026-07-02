using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Auth;

public record AuthResultDto(string Token, Guid UserId, string Email, string DisplayName, bool IsSuperAdmin);

public record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<AuthResultDto>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public class RegisterCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("A user with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            DisplayName = request.DisplayName.Trim()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = jwt.CreateToken(user.Id, user.Email, user.IsSuperAdmin);
        return new AuthResultDto(token, user.Id, user.Email, user.DisplayName, user.IsSuperAdmin);
    }
}
