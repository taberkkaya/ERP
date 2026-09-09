using System.Security.Claims;
using ERPServer.Application.Services;
using Microsoft.AspNetCore.Http;

namespace ERPServer.Infrastructure.Services;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue("Id"), out Guid id)
            ? id
            : null;
}
