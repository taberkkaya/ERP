using Ardalis.SmartEnum;

namespace ERPServer.Domain.Enums;

public sealed class ProductTypeEnum : SmartEnum<ProductTypeEnum>
{
    public static readonly ProductTypeEnum Mamul = new("Mamul", 1);
    public static readonly ProductTypeEnum YariMamul = new("YariMamul", 2);

    public ProductTypeEnum(string name, int value) : base(name, value)
    {
    }
}
