using ERPServer.Domain.Demo;

namespace ERPServer.Application.Services;

public sealed record DemoSessionStartResult(string AccessToken, DemoSessionStatus Status);

public interface IDemoSessionService
{
    bool Enabled { get; }

    /// <summary>Bir sandbox veritabanını kiralar ve ona bağlı bir jeton üretir.</summary>
    Task<DemoSessionStartResult> StartAsync(CancellationToken cancellationToken = default);

    DemoSessionStatus? GetStatus(Guid sessionId);

    bool IsAlive(Guid sessionId);

    /// <summary>Etkinliği işaretler; boşta kalma süresi son gerçek istekten sayılır.</summary>
    void Touch(Guid sessionId);

    /// <summary>
    /// Oturumun kotasından bir yazma düşer. Kota bittiyse false döner ve çağıran
    /// tarafın isteği reddetmesi gerekir.
    /// </summary>
    bool TryRegisterWrite(Guid sessionId);

    /// <summary>Oturumun sandbox'ını bırakır ve sıradaki ziyaretçi için sıfırlar.</summary>
    Task<bool> EndAsync(Guid sessionId, DemoSessionEndReason reason, CancellationToken cancellationToken = default);
}
