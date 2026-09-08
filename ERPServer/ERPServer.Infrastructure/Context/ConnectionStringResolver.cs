using ERPServer.Application.Services;
using ERPServer.Domain.Demo;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ERPServer.Infrastructure.Context;

/// <summary>
/// İsteğin hangi veritabanına bağlanacağını çözer.
///
/// Demo jetonuyla gelen istek kendi sandbox'ına, diğer her istek ana veritabanına
/// bağlanır. Uygulamada tek bir <see cref="ApplicationDbContext"/> var; çok kiracılı
/// bir yapı kurmak yerine bağlantı dizesi istek başına burada üretiliyor.
/// </summary>
internal sealed class ConnectionStringResolver
{
    private readonly IDemoContext _demoContext;
    private readonly DemoOptions _demoOptions;

    public ConnectionStringResolver(
        IConfiguration configuration,
        IDemoContext demoContext,
        IOptions<DemoOptions> demoOptions)
    {
        _demoContext = demoContext;
        _demoOptions = demoOptions.Value;

        Main = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:SqlServer tanımlı değil; uygulama veritabanına bağlanamaz.");
    }

    /// <summary>Ana veritabanı. Migration ve gerçek kullanıcı istekleri buraya gider.</summary>
    public string Main { get; }

    public string Resolve()
    {
        string? database = _demoContext.DatabaseName;

        // Jeton bizim imzamızı taşıyor, yine de ada bir sınır koyuyoruz: eski bir
        // yapılandırmadan kalan jeton, havuza ait olmayan bir veritabanını açamasın.
        if (string.IsNullOrWhiteSpace(database) || !IsSandboxName(database))
            return Main;

        return ForDatabase(database);
    }

    public string ForDatabase(string databaseName) =>
        new SqlConnectionStringBuilder(Main) { InitialCatalog = databaseName }.ConnectionString;

    private bool IsSandboxName(string databaseName) =>
        databaseName.StartsWith(_demoOptions.DatabaseNamePrefix, StringComparison.OrdinalIgnoreCase);
}
