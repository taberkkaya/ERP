using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Products.GetAllProducts;

internal sealed class GetAllProductsQueryHandler(
    IProductRepository productRepository
    ) : IRequestHandler<GetAllProductsQuery, Result<List<Product>>>
{
    public async Task<Result<List<Product>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        List<Product> products = await productRepository.GetAll().OrderBy(p => p.Name).ToListAsync(cancellationToken);

        return products;
    }
}
