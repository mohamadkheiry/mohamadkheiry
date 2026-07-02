using System.Security.Cryptography;
using MediatR;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Calls;

public record CreateCallResultDto(Guid CallId, string LinkCode);

/// <summary>Only an authenticated user may create a call and get an invite link.</summary>
public record CreateCallCommand : IRequest<CreateCallResultDto>;

public class CreateCallCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateCallCommand, CreateCallResultDto>
{
    public async Task<CreateCallResultDto> Handle(CreateCallCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("Login is required to create a call.");

        var call = new Call
        {
            Id = Guid.NewGuid(),
            LinkCode = GenerateLinkCode(),
            CreatedByUserId = userId
        };

        db.Calls.Add(call);
        await db.SaveChangesAsync(ct);

        return new CreateCallResultDto(call.Id, call.LinkCode);
    }

    private static string GenerateLinkCode()
    {
        // URL-safe, unguessable 16-char code.
        var bytes = RandomNumberGenerator.GetBytes(12);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
