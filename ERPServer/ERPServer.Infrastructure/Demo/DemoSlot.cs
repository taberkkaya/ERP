namespace ERPServer.Infrastructure.Demo;

/// <summary>
/// Bir ziyaretçinin oturum boyunca ödünç alabileceği, önceden migrate edilmiş
/// sandbox veritabanı. Slotlar ziyaretçi başına yaratılmıyor, sabit tutulup geri
/// dönüştürülüyor: demo hiçbir istek yolunda DDL çalıştırmıyor ve kapladığı yer
/// SlotCount ile sınırlı kalıyor.
/// </summary>
internal sealed class DemoSlot
{
    public required int Index { get; init; }
    public required string DatabaseName { get; init; }

    public Guid? SessionId { get; set; }

    public bool IsFree => SessionId is null;
}
