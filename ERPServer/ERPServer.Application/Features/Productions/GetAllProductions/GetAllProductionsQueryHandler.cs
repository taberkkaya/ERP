using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Productions.GetAllProductions;

internal sealed class GetAllProductionsQueryHandler(IProductionRepository productionRepository) : IRequestHandler<GetAllProductionsQuery, Result<List<Production>>>
{
    public async Task<Result<List<Production>>> Handle(GetAllProductionsQuery request, CancellationToken cancellationToken)
    {
        List<Production> productions = await productionRepository
            .GetAll()
            .Include(p => p.Product)
            .Include(p => p.Depot)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return productions;
    }
}

