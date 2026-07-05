using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;

namespace SmartCall.Application.Features.Calls;

public record CallInfoDto(Guid CallId, string LinkCode, CallStatus Status, string HostName, DateTime CreatedAt);

public record GetCallByLinkQuery(string LinkCode) : IRequest<CallInfoDto>;

public class GetCallByLinkQueryHandler(IAppDbContext db) : IRequestHandler<GetCallByLinkQuery, CallInfoDto>
{
    public async Task<CallInfoDto> Handle(GetCallByLinkQuery request, CancellationToken ct)
    {
        var call = await db.Calls
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.LinkCode == request.LinkCode, ct)
            ?? throw new NotFoundException("Call not found.");

        return new CallInfoDto(call.Id, call.LinkCode, call.Status, call.CreatedBy.DisplayName, call.CreatedAt);
    }
}
