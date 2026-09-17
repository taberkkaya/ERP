using Ardalis.SmartEnum;

namespace ERPServer.Domain.Enums;

/// <summary>
/// Stok hareketini doğuran iş.
///
/// Fatura artık bu uygulamada kesilmiyor; alış ve satış Defter'de yürüyor ve
/// oluşan hareket entegrasyon ucundan buraya yazılıyor. Kaynağın kayıtta
/// durması, "bu miktar nereden geldi" sorusunu faturanın kendisi başka bir
/// veritabanında olsa da cevaplanabilir tutuyor.
/// </summary>
public sealed class StockMovementSourceEnum : SmartEnum<StockMovementSourceEnum>
{
    /// <summary>Elle yapılan düzeltme: sayım farkı, fire, açılış bakiyesi.</summary>
    public static readonly StockMovementSourceEnum Manual = new("Elle", 1);

    /// <summary>Üretim kaydı: reçetedeki bileşenler çıkar, mamul girer.</summary>
    public static readonly StockMovementSourceEnum Production = new("Üretim", 2);

    /// <summary>Defter'de kesilen alış faturası; stoğa giriş.</summary>
    public static readonly StockMovementSourceEnum PurchaseInvoice = new("Alış Faturası", 3);

    /// <summary>Defter'de kesilen satış faturası; stoktan çıkış.</summary>
    public static readonly StockMovementSourceEnum SalesInvoice = new("Satış Faturası", 4);

    public StockMovementSourceEnum(string name, int value) : base(name, value)
    {
    }
}
