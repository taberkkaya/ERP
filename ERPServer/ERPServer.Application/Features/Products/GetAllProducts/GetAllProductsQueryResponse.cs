using ERPServer.Domain.Enums;

namespace ERPServer.Application.Features.Products.GetAllProducts;

public sealed record GetAllProductsQueryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProductTypeEnum ProductType { get; set; } = ProductTypeEnum.Product;
    public decimal Stock { get; set; }
}