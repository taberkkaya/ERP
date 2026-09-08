using ERPServer.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERPServer.Infrastructure;

/// <summary>
/// Ana veritabaninin migration'larini uygular. Context Infrastructure'a kapali
/// oldugu icin WebAPI katmani buraya cagiriyor.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task MigrateMainDatabaseAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        // Istek disinda calisiyoruz; baglanti cozumleyici HttpContext bulamayinca
        // ana veritabanina dusuyor, yani burada her zaman ana veritabani acilir.
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }
}
