using ERPServer.Domain.Abstractions;
using ERPServer.Domain.Enums;

namespace ERPServer.Domain.Entities;

/// <summary>
/// Stoktaki tek bir giriş ya da çıkış. Ürünün eldeki miktarı hiçbir yerde alan
/// olarak tutulmuyor; her zaman bu satırların farkından hesaplanıyor.
///
/// İki kaynak var: içeride üretim, dışarıda Defter'de kesilen fatura. Fatura bu
/// uygulamada durmadığı için ona yabancı anahtarla değil kimliği ve numarasıyla
/// bağlanılıyor — iki uygulama ayrı veritabanlarında ve aralarında veritabanı
/// seviyesinde bir ilişki kurulamaz.
/// </summary>
public sealed class StockMovement : Entity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid DepotId { get; set; }
    public decimal NumberOfEntries { get; set; }
    public decimal NumberOfOutputs { get; set; }
    public decimal Price { get; set; }

    public StockMovementSourceEnum Source { get; set; } = StockMovementSourceEnum.Manual;

    public Guid? ProductionId { get; set; }
    public Production? Production { get; set; }

    /// <summary>
    /// Defter'deki belgenin kimliği. Aynı fatura iki kez işlenmesin ve fatura
    /// değiştiğinde eski hareketleri bulabilelim diye tutuluyor.
    /// </summary>
    public Guid? ExternalDocumentId { get; set; }

    /// <summary>Belgenin okunabilir numarası, örneğin SF2026000001.</summary>
    public string? ExternalDocumentNumber { get; set; }
}
