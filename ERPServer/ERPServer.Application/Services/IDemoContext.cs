namespace ERPServer.Application.Services;

/// <summary>
/// İsteğin gerçek bir kullanıcıya mı yoksa anonim bir demo ziyaretçisine mi ait
/// olduğunu söyler. Veritabanı bağlantısı da buradan okunan sandbox adına göre
/// kuruluyor.
/// </summary>
public interface IDemoContext
{
    bool IsDemoRequest { get; }

    Guid? SessionId { get; }

    /// <summary>Bu isteğin bağlanacağı sandbox veritabanı; demo dışı isteklerde null.</summary>
    string? DatabaseName { get; }
}
