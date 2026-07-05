using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;

namespace SmartCall.Application.Features.Admin;

public record AdminCallDto(
    Guid Id,
    string LinkCode,
    CallStatus Status,
    string HostName,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    List<AdminParticipantDto> Participants,
    List<AdminRecordingDto> Recordings);

public record AdminParticipantDto(string DisplayName, ParticipantRole Role, string? TargetLanguageCode, DateTime JoinedAt, DateTime? LeftAt);
public record AdminRecordingDto(Guid Id, string FilePath, long FileSizeBytes, DateTime StartedAt, DateTime? EndedAt);

public record GetAdminCallsQuery(int Page = 1, int PageSize = 20, CallStatus? Status = null)
    : IRequest<(List<AdminCallDto> Items, int Total)>;

public class GetAdminCallsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAdminCallsQuery, (List<AdminCallDto>, int)>
{
    public async Task<(List<AdminCallDto>, int)> Handle(GetAdminCallsQuery request, CancellationToken ct)
    {
        var query = db.Calls.AsNoTracking();
        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new AdminCallDto(
                c.Id, c.LinkCode, c.Status, c.CreatedBy.DisplayName, c.CreatedAt, c.StartedAt, c.EndedAt,
                c.Participants.Select(p => new AdminParticipantDto(p.DisplayName, p.Role, p.TargetLanguageCode, p.JoinedAt, p.LeftAt)).ToList(),
                c.Recordings.Select(r => new AdminRecordingDto(r.Id, r.FilePath, r.FileSizeBytes, r.StartedAt, r.EndedAt)).ToList()))
            .ToListAsync(ct);

        return (items, total);
    }
}
