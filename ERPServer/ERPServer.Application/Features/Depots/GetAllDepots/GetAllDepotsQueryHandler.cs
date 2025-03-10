using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Depots.GetAllDepots;

internal sealed class GetAllDepotsQueryHandler(
    IDepotRepository depotRepository
    ) : IRequestHandler<GetAllDepotsQuery, Result<List<Depot>>>
{
    public async Task<Result<List<Depot>>> Handle(GetAllDepotsQuery request, CancellationToken cancellationToken)
    {
        List<Depot> depots = await depotRepository.GetAll().OrderBy(p => p.Name).ToListAsync(cancellationToken);

        return depots;
    }
}
