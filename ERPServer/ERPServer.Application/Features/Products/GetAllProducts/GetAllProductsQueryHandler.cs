using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Products.GetAllProducts;

internal sealed class GetAllProductsQueryHandler(
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository
    ) : IRequestHandler<GetAllProductsQuery, Result<List<GetAllProductsQueryResponse>>>
{
    public async Task<Result<List<GetAllProductsQueryResponse>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        List<Product> products = await productRepository.GetAll().OrderBy(p => p.Name).ToListAsync(cancellationToken);

        List<GetAllProductsQueryResponse> response = products
            .Select(s => new GetAllProductsQueryResponse
            {
                Id = s.Id,
                Name = s.Name,
                ProductType = s.ProductType,
                Stock = 0
            }).ToList();

        foreach(var item in response)
        {
            decimal stock = await stockMovementRepository
                .Where(p => p.ProductId == item.Id)
                .SumAsync(s => s.NumberOfEntries - s.NumberOfOutputs, cancellationToken);

            item.Stock = stock;
        }

        return response;
    }
}
