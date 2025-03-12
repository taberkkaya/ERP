using Ardalis.SmartEnum;

namespace ERPServer.Domain.Enums;

public sealed class InvoiceTypeEnum : SmartEnum<InvoiceTypeEnum>
{
    public static readonly InvoiceTypeEnum Purchase = new("Satın Alma Faturası", 1);
    public static readonly InvoiceTypeEnum Sales = new("Satış Faturası", 2);
    public InvoiceTypeEnum(string name, int value) : base(name, value)
    {
    }
}
