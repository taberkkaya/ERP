using ERPServer.Domain.Demo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERPServer.Infrastructure.Demo;

/// <summary>
/// Sandbox havuzunu açılışta bir kez kurar, sonra düzenli aralıklarla toparlar:
/// süresi dolan, boşta kalan ve bellek baskısı yaratan oturumlar geri veriliyor,
/// böylece gözetimsiz bir demo sabit bir alanda kalıyor.
/// </summary>
internal sealed class DemoHostedService(
    DemoSessionService demoSessionService,
    IOptions<DemoOptions> demoOptions,
    ILogger<DemoHostedService> logger
    ) : BackgroundService
{
    private readonly DemoOptions _options = demoOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Demo modu kapalı.");
            return;
        }

        try
        {
            await demoSessionService.InitializeAsync(stoppingToken);
        }
        catch (Exception exception)
        {
            // Başarısız bir hazırlık API'yi de yanında götürmemeli; demo uçları
            // süreç yeniden başlayana kadar kullanılamaz bildirir.
            logger.LogError(exception, "Demo hazırlığı başarısız oldu.");
            return;
        }

        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(5, _options.JanitorIntervalSeconds));
        using PeriodicTimer timer = new(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await demoSessionService.ReclaimAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Demo oturum toparlaması başarısız; sonraki turda yeniden denenecek.");
            }
        }
    }
}
