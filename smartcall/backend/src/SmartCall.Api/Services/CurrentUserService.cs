using System.Security.Claims;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var id = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public bool IsSuperAdmin
        => httpContextAccessor.HttpContext?.User.IsInRole("SuperAdmin") ?? false;
}
