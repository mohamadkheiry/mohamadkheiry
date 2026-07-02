using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Calls;

public record JoinCallResultDto(Guid CallId, Guid ParticipantId, ParticipantRole Role);

/// <summary>
/// Registers a participant on a call. Guests join via the invite link with just
/// a display name; the creator becomes Host; a super admin may join any live
/// call as a full participant without asking permission.
/// </summary>
public record JoinCallCommand(string LinkCode, string DisplayName) : IRequest<JoinCallResultDto>;

public class JoinCallCommandValidator : AbstractValidator<JoinCallCommand>
{
    public JoinCallCommandValidator()
    {
        RuleFor(x => x.LinkCode).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public class JoinCallCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<JoinCallCommand, JoinCallResultDto>
{
    public async Task<JoinCallResultDto> Handle(JoinCallCommand request, CancellationToken ct)
    {
        var call = await db.Calls
            .Include(c => c.Participants.Where(p => p.LeftAt == null))
            .FirstOrDefaultAsync(c => c.LinkCode == request.LinkCode, ct)
            ?? throw new NotFoundException("Call not found.");

        if (call.Status == CallStatus.Ended)
            throw new ConflictException("This call has already ended.");

        var role = currentUser.IsSuperAdmin && call.CreatedByUserId != currentUser.UserId
            ? ParticipantRole.SuperAdmin
            : currentUser.UserId == call.CreatedByUserId
                ? ParticipantRole.Host
                : ParticipantRole.Guest;

        // Calls are strictly 1:1 — super admin joins on top of the two parties.
        var activeRegulars = call.Participants.Count(p => p.Role != ParticipantRole.SuperAdmin);
        if (role != ParticipantRole.SuperAdmin && activeRegulars >= 2)
            throw new ConflictException("This call already has two participants.");

        var participant = new CallParticipant
        {
            Id = Guid.NewGuid(),
            CallId = call.Id,
            UserId = currentUser.UserId,
            DisplayName = request.DisplayName.Trim(),
            Role = role
        };

        if (call.Status == CallStatus.Waiting && role != ParticipantRole.SuperAdmin && activeRegulars >= 1)
        {
            call.Status = CallStatus.InProgress;
            call.StartedAt ??= DateTime.UtcNow;
        }

        db.CallParticipants.Add(participant);
        await db.SaveChangesAsync(ct);

        return new JoinCallResultDto(call.Id, participant.Id, role);
    }
}
