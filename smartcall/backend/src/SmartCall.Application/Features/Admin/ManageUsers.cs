using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Application.Features.Admin;

public record AdminUserDto(Guid Id, string Email, string DisplayName, bool IsSuperAdmin, bool IsActive, DateTime CreatedAt, long TotalTokensUsed);

public record GetAdminUsersQuery(int Page = 1, int PageSize = 20) : IRequest<(List<AdminUserDto> Items, int Total)>;

public class GetAdminUsersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAdminUsersQuery, (List<AdminUserDto>, int)>
{
    public async Task<(List<AdminUserDto>, int)> Handle(GetAdminUsersQuery request, CancellationToken ct)
    {
        var total = await db.Users.CountAsync(ct);
        var items = await db.Users.AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new AdminUserDto(
                u.Id, u.Email, u.DisplayName, u.IsSuperAdmin, u.IsActive, u.CreatedAt,
                u.TokenUsages.Sum(t => (long?)t.TotalTokens) ?? 0))
            .ToListAsync(ct);
        return (items, total);
    }
}

public record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;

public class SetUserActiveCommandHandler(IAppDbContext db) : IRequestHandler<SetUserActiveCommand>
{
    public async Task Handle(SetUserActiveCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException("User not found.");
        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
    }
}

// ---- Token usage report: per user, per call, and system-wide ----

public record TokenUsageReportDto(
    long SystemTotalTokens,
    List<TokenUsageByUserDto> ByUser,
    List<TokenUsageByCallDto> ByCall);

public record TokenUsageByUserDto(Guid? UserId, string? Email, long InputTokens, long OutputTokens, long TotalTokens);
public record TokenUsageByCallDto(Guid? CallId, string? LinkCode, long TotalTokens);

public record GetTokenUsageReportQuery(DateTime? From = null, DateTime? To = null) : IRequest<TokenUsageReportDto>;

public class GetTokenUsageReportQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTokenUsageReportQuery, TokenUsageReportDto>
{
    public async Task<TokenUsageReportDto> Handle(GetTokenUsageReportQuery request, CancellationToken ct)
    {
        var logs = db.TokenUsageLogs.AsNoTracking();
        if (request.From.HasValue) logs = logs.Where(l => l.CreatedAt >= request.From.Value);
        if (request.To.HasValue) logs = logs.Where(l => l.CreatedAt <= request.To.Value);

        var systemTotal = await logs.SumAsync(l => (long?)l.TotalTokens, ct) ?? 0;

        // Group by scalar keys only (navigation properties inside a GroupBy
        // key are not translatable); resolve display names afterwards.
        var byUserRaw = await logs
            .GroupBy(l => l.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Input = g.Sum(l => l.InputTokens),
                Output = g.Sum(l => l.OutputTokens),
                Total = g.Sum(l => l.TotalTokens)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        var userIds = byUserRaw.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).ToList();
        var emails = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        var byUser = byUserRaw
            .Select(x => new TokenUsageByUserDto(
                x.UserId,
                x.UserId.HasValue && emails.TryGetValue(x.UserId.Value, out var email) ? email : null,
                x.Input, x.Output, x.Total))
            .ToList();

        var byCallRaw = await logs
            .GroupBy(l => l.CallId)
            .Select(g => new { CallId = g.Key, Total = g.Sum(l => l.TotalTokens) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        var callIds = byCallRaw.Where(x => x.CallId.HasValue).Select(x => x.CallId!.Value).ToList();
        var linkCodes = await db.Calls.AsNoTracking()
            .Where(c => callIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.LinkCode, ct);

        var byCall = byCallRaw
            .Select(x => new TokenUsageByCallDto(
                x.CallId,
                x.CallId.HasValue && linkCodes.TryGetValue(x.CallId.Value, out var code) ? code : null,
                x.Total))
            .ToList();

        return new TokenUsageReportDto(systemTotal, byUser, byCall);
    }
}
