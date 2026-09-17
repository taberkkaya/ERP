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

        // Giriş ekranı demo düğmesini gösterip göstermeyeceğine ve ziyaretçiden
        // e-posta isteyip istemeyeceğine buna bakarak karar veriyor.
        group.MapGet("config", (
            IDemoSessionService demoSessionService,
            IDemoVerificationService verification) =>
            Results.Ok((Result<DemoConfig>)new DemoConfig(
                demoSessionService.Enabled,
                verification.Required)))
            .AllowAnonymous()
            .Produces<Result<DemoConfig>>();

        // Anonim: ziyaretçinin adresine kod göndermek demoya girmenin ilk adımı.
        group.MapPost("request-code", async (
            DemoCodeRequest request,
            IDemoVerificationService verification,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!verification.Required)
                return Results.Ok((Result<DemoCodeResult>)new DemoCodeResult(
                    "Bu kurulumda doğrulama gerekmiyor.", AlreadyVerified: true));

            DemoVerificationResult result = await verification.SendCodeAsync(
                request.Email,
                httpContext.Connection.RemoteIpAddress?.ToString(),
                httpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return result.Succeeded
                ? Results.Ok((Result<DemoCodeResult>)new DemoCodeResult(result.Message, result.AlreadyVerified))
                : Results.BadRequest(Result<DemoCodeResult>.Failure(400, [result.Message]));
        })
        .AllowAnonymous()
        .RequireRateLimiting("demo-code")
        .Produces<Result<DemoCodeResult>>();

        // Bilerek anonim: sandbox'a açılan kapı burası.
        group.MapPost("start", async (
            DemoStartRequest request,
            IDemoSessionService demoSessionService,
            IDemoVerificationService verification,
            IDemoTelemetryPublisher telemetry,
            CancellationToken cancellationToken) =>
        {
            if (!demoSessionService.Enabled)
                return Results.NotFound(Result<string>.Failure(404, ["Demo modu kapalı."]));

            // Doğrulama kapalıysa (mail yapılandırılmamışsa) kapı eskisi gibi açık.
            if (verification.Required)
            {
                string email = request.Email ?? string.Empty;
                string code = request.Code ?? string.Empty;
                DemoVerificationResult check;

                if (string.IsNullOrWhiteSpace(code))
                {
                    // Kod boş geldiyse ziyaretçi adresini daha önce doğrulamış olmalı.
                    // Oturumunu kendi kapatıp geri dönen biri, elindeki kod tüketildiği
                    // için buraya kodsuz geliyor; onu yeniden kod turuna sokmuyoruz.
                    check = await verification.HasValidVerificationAsync(email, cancellationToken)
                        ? DemoVerificationResult.Ok("Adresiniz daha önce doğrulanmıştı.")
                        : DemoVerificationResult.Fail("Önce e-posta adresinize kod isteyin.");
                }
                else
                {
                    check = await verification.VerifyAsync(email, code, cancellationToken);
                }

                if (!check.Succeeded)
                    return Results.BadRequest(Result<string>.Failure(400, [check.Message]));
            }

            try
            {
                DemoSessionStartResult result = await demoSessionService.StartAsync(cancellationToken);

                // Sayaç yalnızca gerçekten açılan oturum için artıyor.
                if (verification.Required)
                {
                    DemoVisitorSnapshot? visitor = await verification.RecordSessionAsync(
                        request.Email ?? string.Empty, cancellationToken);

                    // Oturumu havuz açtı, ziyaretçiyi bu uç biliyor; ikisini panelde
                    // aynı kayda bağlayan çağrı burada.
                    if (visitor is not null)
                        telemetry.VisitorIdentified(
                            result.Status.SessionId, visitor.Email, visitor.Country, visitor.City);
                }

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

public sealed record DemoCodeRequest(string Email);

/// <summary>
/// Kod isteğinin sonucu. <paramref name="AlreadyVerified"/> true ise kod
/// gönderilmedi ve gerekmiyor; istemci kod adımını atlayıp demoyu başlatır.
/// </summary>
public sealed record DemoCodeResult(string Message, bool AlreadyVerified);

/// <summary>Doğrulama kapalıyken iki alan da boş gelebilir.</summary>
public sealed record DemoStartRequest(string? Email, string? Code);

public sealed record DemoConfig(bool Enabled, bool EmailVerificationRequired);
