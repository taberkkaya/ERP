using System.Security.Claims;
using ERPServer.Application.Services;
using Microsoft.AspNetCore.Http;

namespace ERPServer.Infrastructure.Demo;

internal sealed class DemoContext(IHttpContextAccessor httpContextAccessor) : IDemoContext
{
    public bool IsDemoRequest => SessionId is not null;

    public Guid? SessionId =>
        Guid.TryParse(Read(DemoClaimTypes.SessionId), out Guid sessionId) ? sessionId : null;

    public string? DatabaseName => Read(DemoClaimTypes.Database);

    private string? Read(string claimType) =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
}
