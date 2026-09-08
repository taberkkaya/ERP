using System.Security.Claims;
using ERPServer.Application.Services;
using ERPServer.Domain.Demo;
using TS.Result;

namespace ERPServer.WebAPI.Modules;

/// <summary>
/// Demo oturumunun uçları. Denetleyici yerine minimal API: hepsi tek bir dosyada
/// duran, MediatR'a hiç uğramayan altyapı işlemleri.
/// </summary>
public static class DemoModule
{
    private const string SessionClaim = "DemoSessionId";

    public static void RegisterDemoRoutes(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/demo").WithTags("demo");

        // Giriş ekranı demo düğmesini gösterip göstermeyeceğine buna bakarak karar
        // veriyor; kapalı bir kurulumda düğme hiç çizilmiyor.
        group.MapGet("config", (IDemoSessionService demoSessionService) =>
            Results.Ok((Result<DemoConfig>)new DemoConfig(demoSessionService.Enabled)))
            .AllowAnonymous()
            .Produces<Result<DemoConfig>>();

        // Bilerek anonim: sandbox'a açılan kapı burası.
        group.MapPost("start", async (
            IDemoSessionService demoSessionService,
            CancellationToken cancellationToken) =>
        {
            if (!demoSessionService.Enabled)
                return Results.NotFound(Result<string>.Failure(404, ["Demo modu kapalı."]));

            try
            {
                DemoSessionStartResult result = await demoSessionService.StartAsync(cancellationToken);
                return Results.Ok((Result<DemoSessionStartResult>)result);
            }
            catch (InvalidOperationException exception)
            {
                // Bütün sandbox'lar dolu ya da hazırlık hiç tamamlanmadı.
                return Results.Json(
                    Result<string>.Failure(503, [exception.Message]),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .AllowAnonymous()
        .RequireRateLimiting("demo-start")
        .Produces<Result<DemoSessionStartResult>>();

        group.MapGet("status", (HttpContext httpContext, IDemoSessionService demoSessionService) =>
        {
            Guid? sessionId = ReadSessionId(httpContext);

            if (sessionId is null)
                return Results.BadRequest(Result<string>.Failure("Bu bir demo oturumu değil."));

            DemoSessionStatus? status = demoSessionService.GetStatus(sessionId.Value);

            return status is null
                ? Results.Json(
                    Result<string>.Failure(409, ["Demo oturumunuz sona erdi."]),
                    statusCode: StatusCodes.Status409Conflict)
                : Results.Ok((Result<DemoSessionStatus>)status);
        })
        .RequireAuthorization()
        .Produces<Result<DemoSessionStatus>>();

        // Ziyaretçinin süre dolmasını beklemeden baştan başlamasını sağlıyor.
        group.MapPost("reset", async (
            HttpContext httpContext,
            IDemoSessionService demoSessionService,
            CancellationToken cancellationToken) =>
        {
            Guid? sessionId = ReadSessionId(httpContext);

            if (sessionId is not null)
                await demoSessionService.EndAsync(sessionId.Value, DemoSessionEndReason.VisitorReset, cancellationToken);

            try
            {
                DemoSessionStartResult result = await demoSessionService.StartAsync(cancellationToken);
                return Results.Ok((Result<DemoSessionStartResult>)result);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Json(
                    Result<string>.Failure(503, [exception.Message]),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .RequireAuthorization()
        .Produces<Result<DemoSessionStartResult>>();

        group.MapPost("end", async (
            HttpContext httpContext,
            IDemoSessionService demoSessionService,
            CancellationToken cancellationToken) =>
        {
            Guid? sessionId = ReadSessionId(httpContext);

            if (sessionId is not null)
                await demoSessionService.EndAsync(sessionId.Value, DemoSessionEndReason.VisitorReset, cancellationToken);

            return Results.Ok((Result<string>)"Demo oturumu kapatıldı.");
        })
        .RequireAuthorization()
        .Produces<Result<string>>();
    }

    private static Guid? ReadSessionId(HttpContext httpContext) =>
        Guid.TryParse(httpContext.User.FindFirstValue(SessionClaim), out Guid sessionId) ? sessionId : null;
}

public sealed record DemoConfig(bool Enabled);
