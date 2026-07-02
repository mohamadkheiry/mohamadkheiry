using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;

namespace SmartCall.Application.Features.Calls;

public record LeaveCallCommand(Guid ParticipantId) : IRequest;

public class LeaveCallCommandHandler(IAppDbContext db) : IRequestHandler<LeaveCallCommand>
{
    public async Task Handle(LeaveCallCommand request, CancellationToken ct)
    {
        var participant = await db.CallParticipants
            .Include(p => p.Call).ThenInclude(c => c.Participants)
            .FirstOrDefaultAsync(p => p.Id == request.ParticipantId, ct)
            ?? throw new NotFoundException("Participant not found.");

        participant.LeftAt = DateTime.UtcNow;

        var stillActive = participant.Call.Participants
            .Any(p => p.Id != participant.Id && p.LeftAt == null && p.Role != ParticipantRole.SuperAdmin);
        if (!stillActive)
        {
            participant.Call.Status = CallStatus.Ended;
            participant.Call.EndedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
