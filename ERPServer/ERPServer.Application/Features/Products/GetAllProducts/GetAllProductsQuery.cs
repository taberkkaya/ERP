using ERPServer.Domain.Entities;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Products.GetAllProducts;

public sealed record GetAllProductsQuery() : IRequest<Result<List<Product>>>;
