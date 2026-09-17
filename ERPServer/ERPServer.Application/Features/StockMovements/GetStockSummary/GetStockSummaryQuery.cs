using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.StockMovements.GetStockSummary;

/// <summary>
/// Panodaki stok özeti. Fatura Defter'e taşındıktan sonra panonun "bu depoya ne
/// girdi, ne çıktı" sorusunu kendi verisiyle cevaplaması gerekiyordu.
/// </summary>
public sealed record GetStockSummaryQuery() : IRequest<Result<StockSummaryResponse>>;

/// <param name="Source">Hareketi doğuran iş: Üretim, Alış Faturası, Satış Faturası, Elle.</param>
public sealed record StockSummaryLine(string Source, decimal EntryValue, decimal ExitValue, int MovementCount);

public sealed record StockSummaryResponse(
    decimal EntryValue,
    decimal ExitValue,
    int MovementCount,
    List<StockSummaryLine> BySource);
