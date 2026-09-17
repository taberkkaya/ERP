namespace ERPServer.Domain.Integration;

/// <summary>
/// Defter ile aradaki bağlantı. "Integration" yapılandırma bölümünden bağlanır.
///
/// Anahtar boş bırakılırsa entegrasyon uçları hiçbir isteği kabul etmez; tek
/// başına çalışan bir Tezgah kurulumu için doğru olan da budur.
/// </summary>
public sealed class IntegrationOptions
{
    public const string SectionName = "Integration";

    /// <summary>Defter ile paylaşılan gizli anahtar. Ortam değişkeni ile verilir.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
