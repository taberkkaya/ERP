using System.Security.Claims;
using ERPServer.Application.Services;

namespace ERPServer.WebAPI.Middlewares;

/// <summary>
/// Yalnızca anonim demo ziyaretçileri için geçerli kuralları uygular: oturum hâlâ
/// yaşıyor olmalı ve her yazma işlemi, ziyaretçiyi eninde sonunda iletişim
/// sayfasına yönlendiren kotadan düşülmeli. Gerçek kullanıcı jetonu taşıyan
/// istekler doğrudan geçiyor.
/// </summary>
public sealed class DemoSessionMiddleware(RequestDelegate next)
{
    private const string SessionClaim = "DemoSessionId";

    /// <summary>
    /// Kotadan düşülen uçlar. İhtiyaç planlaması da burada: sorgu gibi görünse de
    /// siparişin durumunu değiştiriyor.
    /// </summary>
    private static readonly HashSet<string> WriteActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "create",
        "update",
        "deletebyid",
        "requirementsplanningbyorderid"
    };

    public async Task InvokeAsync(HttpContext context, IDemoSessionService demoSessionService)
    {
        string? claim = context.User.FindFirstValue(SessionClaim);

        if (claim is null || !Guid.TryParse(claim, out Guid sessionId))
        {
            await next(context);
            return;
        }

        // Demo uçları oturum ömrünü kendileri yönetiyor; canlı oturum şartı koymak,
        // biten bir oturumdan sonra yeniden başlamayı imkânsız kılardı.
        if (context.Request.Path.StartsWithSegments("/api/demo"))
        {
            await next(context);
            return;
        }

        if (!demoSessionService.IsAlive(sessionId))
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "session_ended",
                "Demo oturumunuz sona erdi. Yeni bir oturum başlatabilirsiniz.");
            return;
        }

        string? action = ReadAction(context.Request.Path);
        bool isWrite = action is not null && WriteActions.Contains(action);

        if (isWrite && !demoSessionService.TryRegisterWrite(sessionId))
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, "write_limit",
                "Bu demo oturumu için ayrılan işlem hakkı doldu.");
            return;
        }

        if (!isWrite) demoSessionService.Touch(sessionId);

        await next(context);
    }

    /// <summary>Her iş ucu /api/{controller}/{action} biçiminde.</summary>
    private static string? ReadAction(PathString path)
    {
        string[] segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];

        return segments.Length < 3 ? null : segments[^1];
    }

    private static Task WriteAsync(HttpContext context, int statusCode, string demoCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            data = (object?)null,
            errorMessages = new[] { message },
            isSuccessful = false,
            statusCode,
            demoCode
        });
    }
}
