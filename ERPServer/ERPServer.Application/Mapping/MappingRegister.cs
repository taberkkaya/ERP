using ERPServer.Application.Features.Invoices.CreateInvoice;
using ERPServer.Application.Features.Invoices.UpdateInvoice;
using ERPServer.Application.Features.Orders.CreateOrder;
using ERPServer.Application.Features.Orders.UpdateOrder;
using ERPServer.Application.Features.Products.CreateProduct;
using ERPServer.Application.Features.Products.UpdateProduct;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using Mapster;

namespace ERPServer.Application.Mapping;

/// <summary>
/// Komuttan varlığa dönüşümler.
///
/// Aynı adı taşıyan alanları Mapster kendisi eşliyor; burada yalnızca ad üzerinden
/// çözülemeyen üç durum var: sayısal değerden SmartEnum'a çeviriler, kalem
/// listeleri ve güncellemede kalemlerin elle yönetildiği yerler.
/// </summary>
public sealed class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductCommand, Product>()
            .Map(dest => dest.ProductType, src => ProductTypeEnum.FromValue(src.ProductTypeValue));

        config.NewConfig<UpdateProductCommand, Product>()
            .Map(dest => dest.ProductType, src => ProductTypeEnum.FromValue(src.ProductTypeValue));

        config.NewConfig<CreateOrderCommand, Order>()
            .Map(dest => dest.Details, src => src.Details.Select(detail => new OrderDetail
            {
                ProductId = detail.ProductId,
                Quantity = detail.Quantity,
                Price = detail.Price
            }).ToList());

        // Güncellemede kalemler işleyicide siliniyor ve yeniden kuruluyor; buradan
        // da eşlenirse aynı satırlar iki kez ekleniyor.
        config.NewConfig<UpdateOrderCommand, Order>()
            .Ignore(nameof(Order.Details));

        config.NewConfig<CreateInvoiceCommand, Invoice>()
            .Map(dest => dest.Type, src => InvoiceTypeEnum.FromValue(src.TypeValue))
            .Map(dest => dest.Details, src => src.Details.Select(detail => new InvoiceDetail
            {
                ProductId = detail.ProductId,
                DepotId = detail.DepotId,
                Quantity = detail.Quantity,
                Price = detail.Price
            }).ToList());

        config.NewConfig<UpdateInvoiceCommand, Invoice>()
            .Ignore(nameof(Invoice.Details));
    }
}
