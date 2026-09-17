using System.Collections.Concurrent;
using System.Diagnostics;
using ERPServer.Application.Services;
using ERPServer.Domain.Demo;
using ERPServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERPServer.Infrastructure.Demo;

/// <summary>
/// Her anonim ziyaretçiye bir oturum boyunca kendi veritabanını verir; oturumun
/// kotası, süresi ya da sürecin bellek bütçesi dolduğunda geri alır.
///
/// Singleton olarak kayıtlı: slot havuzu süreç geneli bir durum.
/// </summary>
internal sealed class DemoSessionService(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoOptions> demoOptions,
    IDemoTelemetryPublisher telemetry,
    ILogger<DemoSessionService> logger
    ) : IDemoSessionService
{
    private readonly DemoOptions _options = demoOptions.Value;
    private readonly ConcurrentDictionary<Guid, DemoSession> _sessions = new();
    private readonly SemaphoreSlim _slotLock = new(1, 1);

    private DemoSlot[] _slots = [];
    private volatile bool _ready;

    public bool Enabled => _options.Enabled;

    public bool IsReady => _ready;

    #region hazırlık

    /// <summary>
    /// Sabit sayıda sandbox veritabanını kurar, migrate eder ve örnek veriyle
    /// doldurur. Açılışta bir kez çağrılır; hazırlanamayan slot atlanıyor, tek bir
    /// bozuk veritabanı demonun tamamını düşürmesin.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        List<DemoSlot> slots = new();

        for (int index = 1; index <= _options.SlotCount; index++)
        {
            string databaseName = $"{_options.DatabaseNamePrefix}{index:D2}";

            try
            {
                using ApplicationDbContext context = CreateContext(databaseName);

                await context.Database.MigrateAsync(cancellationToken);
                await DemoDataSeeder.ResetAsync(context, cancellationToken);

                slots.Add(new DemoSlot { Index = index, DatabaseName = databaseName });

                logger.LogInformation("Demo slot {Index} hazır: {Database}.", index, databaseName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Demo slot {Index} ({Database}) hazırlanamadı.", index, databaseName);
            }
        }

        _slots = [.. slots];
        _ready = _slots.Length > 0;

        // Havuz hazır olur olmaz panele haber ver: ilk ziyaretçiyi beklemeden
        // uygulamanın ayakta olduğu görünsün.
        telemetry.Heartbeat(Enabled, _slots.Length, 0);

        if (!_ready)
            logger.LogError("Hiçbir demo slotu hazırlanamadı; demo uçları kullanılamaz bildirecek.");
    }

    /// <summary>
    /// Belirtilen sandbox veritabanına bağlı bir context kurar. DI'daki context
    /// isteğin jetonuna göre çözülüyor; havuz işleri ise istek dışında çalıştığı
    /// için bağlantıyı burada elle kuruyoruz.
    /// </summary>
    private ApplicationDbContext CreateContext(string databaseName)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ConnectionStringResolver>();

        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(resolver.ForDatabase(databaseName))
                .Options;

        return new ApplicationDbContext(options);
    }

    #endregion

    #region oturum ömrü

    public async Task<DemoSessionStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!Enabled || !_ready)
            throw new InvalidOperationException(
                "Demo ortamı henüz hazır değil, birkaç saniye sonra tekrar deneyin.");

        DemoSlot slot = await LeaseSlotAsync(cancellationToken);

        DateTimeOffset now = DateTimeOffset.Now;
        DemoSession session = new()
        {
            Id = Guid.NewGuid(),
            SlotIndex = slot.Index,
            DatabaseName = slot.DatabaseName,
            WorkspaceName = _options.WorkspaceName,
            StartedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.AddMinutes(_options.AbsoluteTimeoutMinutes)
        };

        slot.SessionId = session.Id;
        _sessions[session.Id] = session;

        telemetry.SessionStarted(session.Id, now);

        string accessToken = CreateToken(session);

        logger.LogInformation(
            "Demo oturumu {SessionId} slot {Slot} üzerinde açıldı. Etkin: {Active}/{Total}.",
            session.Id, slot.Index, _sessions.Count, _slots.Length);

        return new DemoSessionStartResult(accessToken, BuildStatus(session));
    }

    private async Task<DemoSlot> LeaseSlotAsync(CancellationToken cancellationToken)
    {
        await _slotLock.WaitAsync(cancellationToken);

        DemoSlot slot;
        try
        {
            slot = _slots.FirstOrDefault(s => s.IsFree)
                   ?? await ReclaimOldestSlotAsync(cancellationToken);

            // Kilit içinde işaretleniyor ki iki ziyaretçi aynı slotu kiralayamasın;
            // gerçek oturum kimliği, oturum nesnesi kurulduğunda bunun yerine geçer.
            slot.SessionId = Guid.Empty;
        }
        finally
        {
            _slotLock.Release();
        }

        try
        {
            // Sıfırlama bırakırken değil kiralarken yapılıyor: önceki bırakma yarıda
            // kalmış olsa bile temiz bir başlangıcı garanti eden tek nokta burası.
            await ResetSlotAsync(slot, cancellationToken);
        }
        catch
        {
            // Slotu havuzdan sızdırmak yerine geri veriyoruz.
            slot.SessionId = null;
            throw;
        }

        return slot;
    }

    private async Task<DemoSlot> ReclaimOldestSlotAsync(CancellationToken cancellationToken)
    {
        DemoSession? oldest = _sessions.Values.OrderBy(s => s.LastSeenAt).FirstOrDefault();

        if (oldest is null)
            throw new InvalidOperationException("Şu anda boş demo alanı yok, birazdan tekrar deneyin.");

        logger.LogInformation("Tüm slotlar dolu; {SessionId} oturumu geri alınıyor.", oldest.Id);
        await EndAsync(oldest.Id, DemoSessionEndReason.SlotReclaimed, cancellationToken);

        return _slots.FirstOrDefault(s => s.IsFree)
               ?? throw new InvalidOperationException("Şu anda boş demo alanı yok, birazdan tekrar deneyin.");
    }

    private async Task ResetSlotAsync(DemoSlot slot, CancellationToken cancellationToken)
    {
        using ApplicationDbContext context = CreateContext(slot.DatabaseName);
        await DemoDataSeeder.ResetAsync(context, cancellationToken);
    }

    private string CreateToken(DemoSession session)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var jwtProvider = scope.ServiceProvider.GetRequiredService<IJwtProvider>();

        // Jeton oturumdan biraz uzun yaşıyor: tam sona ererken atılan istek
        // "oturum bitti" yerine "geçersiz jeton" hatası vermesin.
        return jwtProvider.CreateDemoToken(
            session.Id,
            session.DatabaseName,
            session.WorkspaceName,
            TimeSpan.FromMinutes(_options.AbsoluteTimeoutMinutes + 5));
    }

    public async Task<bool> EndAsync(
        Guid sessionId,
        DemoSessionEndReason reason,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryRemove(sessionId, out DemoSession? session)) return false;

        session.EndReason = reason;

        DemoSlot? slot = _slots.FirstOrDefault(s => s.SessionId == sessionId);
        if (slot is not null)
        {
            slot.SessionId = null;

            try
            {
                using ApplicationDbContext context = CreateContext(slot.DatabaseName);
                await DemoDataSeeder.WipeAsync(context, cancellationToken);
            }
            catch (Exception exception)
            {
                // Slot bir sonraki kiralamada zaten yeniden doldurulacak; başarısız
                // bir temizlik onu havuzun dışında bırakmamalı.
                logger.LogWarning(exception, "Demo slot {Slot} bırakılırken temizlenemedi.", slot.Index);
            }
        }

        telemetry.SessionEnded(sessionId, reason.ToString(), session.WriteCount, _options.WriteLimit);

        logger.LogInformation("Demo oturumu {SessionId} kapandı ({Reason}).", sessionId, reason);

        return true;
    }

    #endregion

    #region kota

    public bool IsAlive(Guid sessionId) => _sessions.ContainsKey(sessionId);

    public void Touch(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out DemoSession? session))
            session.LastSeenAt = DateTimeOffset.Now;
    }

    public bool TryRegisterWrite(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out DemoSession? session)) return false;

        lock (session)
        {
            if (session.WriteCount >= _options.WriteLimit) return false;

            session.WriteCount++;
            session.LastSeenAt = DateTimeOffset.Now;
        }

        return true;
    }

    public DemoSessionStatus? GetStatus(Guid sessionId) =>
        _sessions.TryGetValue(sessionId, out DemoSession? session) ? BuildStatus(session) : null;

    private DemoSessionStatus BuildStatus(DemoSession session)
    {
        int secondsRemaining = (int)Math.Max(0, (session.ExpiresAt - DateTimeOffset.Now).TotalSeconds);

        return new DemoSessionStatus(
            SessionId: session.Id,
            WorkspaceName: session.WorkspaceName,
            WritesUsed: session.WriteCount,
            WriteLimit: _options.WriteLimit,
            NudgeAfterWrites: _options.NudgeAfterWrites,
            ExpiresAt: session.ExpiresAt,
            SecondsRemaining: secondsRemaining,
            // Değişkeni boş bırakan bir kurulumda bile açılabilir bir bağlantı kalsın.
            ContactUrl: string.IsNullOrWhiteSpace(_options.ContactUrl)
                ? DemoOptions.DefaultContactUrl
                : _options.ContactUrl,
            IsActive: session.EndReason == DemoSessionEndReason.None,
            EndReason: session.EndReason == DemoSessionEndReason.None ? null : session.EndReason.ToString());
    }

    #endregion

    #region geri alma

    /// <summary>
    /// Süresi dolan, boşta kalan ya da süreç bellek bütçesini aştığı için yer açması
    /// gereken oturumları kapatır. Arka plan servisi çağırıyor.
    /// </summary>
    public async Task ReclaimAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        TimeSpan idleTimeout = TimeSpan.FromMinutes(_options.IdleTimeoutMinutes);

        foreach (DemoSession session in _sessions.Values.ToList())
        {
            if (now >= session.ExpiresAt)
                await EndAsync(session.Id, DemoSessionEndReason.Expired, cancellationToken);
            else if (now - session.LastSeenAt >= idleTimeout)
                await EndAsync(session.Id, DemoSessionEndReason.Idle, cancellationToken);
        }

        await ReclaimForMemoryAsync(cancellationToken);

        // Nabız temizlik turuna biniyor: ayrı bir zamanlayıcı kurmadan düzenli
        // aralıkla atıyor ve havuzun o anki doluluğunu taşıyor.
        telemetry.Heartbeat(Enabled, _slots.Length, _slots.Count(slot => !slot.IsFree));
    }

    private async Task ReclaimForMemoryAsync(CancellationToken cancellationToken)
    {
        if (_options.MemoryThresholdMegabytes <= 0 || _sessions.IsEmpty) return;

        long thresholdBytes = (long)_options.MemoryThresholdMegabytes * 1024 * 1024;
        if (CurrentMemoryBytes() <= thresholdBytes) return;

        // Toplamadan sonra yeniden ölçülüyor: çöp toplayıcının kendi başına
        // soğurabileceği bir sıçrama, ziyaretçiyi oturumun ortasında atmak için
        // yeterli bir sebep değil.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (CurrentMemoryBytes() <= thresholdBytes) return;

        logger.LogWarning(
            "{Threshold} MB bellek bütçesi aşıldı; en uzun süredir dokunulmayan oturumlar bırakılıyor.",
            _options.MemoryThresholdMegabytes);

        foreach (DemoSession session in _sessions.Values.OrderBy(s => s.LastSeenAt).ToList())
        {
            await EndAsync(session.Id, DemoSessionEndReason.MemoryPressure, cancellationToken);

            GC.Collect();
            if (CurrentMemoryBytes() <= thresholdBytes) return;
        }
    }

    private static long CurrentMemoryBytes()
    {
        using Process process = Process.GetCurrentProcess();
        return process.WorkingSet64;
    }

    #endregion
}
