using ERPServer.Domain.Entities;
using ERPServer.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace ERPServer.WebAPI.Middlewares;

public static class ExtensionsMiddleware
{
    /// <summary>
    /// Acilista veritabanini hazirlar: migration'lari uygular ve ilk yonetici
    /// kullanicisini olusturur. Konteynerde elle migration calistiracak bir kabuk
    /// olmadigi icin bu adim acilisin parcasi.
    /// </summary>
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        bool migrateOnStartup = app.Configuration.GetValue("Database:MigrateOnStartup", true);

        if (migrateOnStartup)
        {
            await DatabaseInitializer.MigrateMainDatabaseAsync(app.Services);
        }

        await CreateFirstUserAsync(app);
    }

    private static async Task CreateFirstUserAsync(WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        const string userName = "admin";

        if (await userManager.FindByNameAsync(userName) is not null) return;

        // Parola yapilandirmadan geliyor; depoda sabit bir parola tutulmuyor.
        string password = app.Configuration["Seed:AdminPassword"] ?? "1";

        AppUser user = new()
        {
            UserName = userName,
            Email = "admin@tezgah.local",
            FirstName = "Sistem",
            LastName = "Yoneticisi",
            EmailConfirmed = true
        };

        IdentityResult result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            app.Logger.LogError(
                "Ilk kullanici olusturulamadi: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
