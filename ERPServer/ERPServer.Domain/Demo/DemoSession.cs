namespace ERPServer.Domain.Demo;

public enum DemoSessionEndReason
{
    None = 0,
    WriteLimitReached = 1,
    Expired = 2,
    Idle = 3,
    MemoryPressure = 4,
    SlotReclaimed = 5,
    VisitorReset = 6
}

/// <summary>
/// Bir ziyaretçinin demo turu. Oturumlar bilinçli olarak bellekte: yeniden
/// başlatmadan sonra hiçbir değeri kalmıyor ve kalıcı tutmak, kimsenin okumadığı
/// bir veri için ana veritabanına yazma trafiği eklemek olurdu.
/// </summary>
public sealed class DemoSession
{
    public required Guid Id { get; init; }
    public required int SlotIndex { get; init; }
    public required string DatabaseName { get; init; }
    public required string WorkspaceName { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset LastSeenAt { get; set; }
    public int WriteCount { get; set; }
    public DemoSessionEndReason EndReason { get; set; } = DemoSessionEndReason.None;
}

/// <summary>İstemcinin demo şeridini ve iletişim penceresini çizmek için ihtiyaç duyduğu bilgi.</summary>
public sealed record DemoSessionStatus(
    Guid SessionId,
    string WorkspaceName,
    int WritesUsed,
    int WriteLimit,
    int NudgeAfterWrites,
    DateTimeOffset ExpiresAt,
    int SecondsRemaining,
    string ContactUrl,
    bool IsActive,
    string? EndReason);
