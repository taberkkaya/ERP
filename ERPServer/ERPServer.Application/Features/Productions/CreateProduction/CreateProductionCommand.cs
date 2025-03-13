using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Productions.CreateProduction;

public sealed record CreateProductionCommand(
    Guid ProductId,
    decimal Quantity,
    Guid DepotId
    ) : IRequest<Result<string>>;
