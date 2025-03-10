using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    int ProductType
    ) : IRequest<Result<string>>;
