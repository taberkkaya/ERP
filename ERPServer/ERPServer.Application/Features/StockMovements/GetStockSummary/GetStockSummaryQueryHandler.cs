using ERPServer.Domain.Enums;
using ERPServer.Domain.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.StockMovements.GetStockSummary;

internal sealed class GetStockSummaryQueryHandler(
    IStockMovementRepository stockMovementRepository
    ) : IRequestHandler<GetStockSummaryQuery, Result<StockSummaryResponse>>
{
    public async Task<Result<StockSummaryResponse>> Handle(
        GetStockSummaryQuery request, CancellationToken cancellationToken)
    {
        // Kaynak veritabanında sayı olarak duruyor; adı SmartEnum'dan sonradan
        // çözülüyor. Gruplama sunucuda: hareket tablosu satır satır çekilirse
        // uzun süredir çalışan bir sandbox'ta pano gözle görülür yavaşlıyor.
        var rows = await stockMovementRepository.GetAll()
            .GroupBy(p => p.Source)
            .Select(g => new
            {
                Source = g.Key,
                EntryValue = g.Sum(s => s.NumberOfEntries * s.Price),
                ExitValue = g.Sum(s => s.NumberOfOutputs * s.Price),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        List<StockSummaryLine> bySource = rows
            .Select(row => new StockSummaryLine(
                row.Source.Name, row.EntryValue, row.ExitValue, row.Count))
            .OrderByDescending(line => line.EntryValue + line.ExitValue)
            .ToList();

        return new StockSummaryResponse(
            rows.Sum(row => row.EntryValue),
            rows.Sum(row => row.ExitValue),
            rows.Sum(row => row.Count),
            bySource);
    }
}
