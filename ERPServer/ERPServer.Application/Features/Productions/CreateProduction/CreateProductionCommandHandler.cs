using MapsterMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Productions.CreateProduction;

internal sealed class CreateProductionCommandHandler(
    IProductionRepository productionRepository,
    IRecipeRepository recipeRepository,
    IStockMovementRepository stockMovementRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper
    ) : IRequestHandler<CreateProductionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateProductionCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Result<string>.Failure("Üretim miktarı sıfırdan büyük olmalı.");

        Production production = mapper.Map<Production>(request);

        Recipe? recipe = await recipeRepository
            .Where(p => p.ProductId == request.ProductId)
            .Include(p => p.Details!)
            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(cancellationToken);

        if (recipe?.Details is null || recipe.Details.Count == 0)
            return Result<string>.Failure("Bu ürünün reçetesi tanımlı değil, üretim yapılamaz.");

        List<StockMovement> newMovements = new();

        // Üretilen mamulün maliyeti, tüketilen bileşenlerin maliyetlerinin toplamı.
        decimal consumedCost = 0;

        foreach (RecipeDetail detail in recipe.Details)
        {
            // Reçete bir birim için yazılır; ihtiyaç üretim adediyle ölçeklenir.
            decimal required = detail.Quantity * request.Quantity;

            List<StockMovement> movements = await stockMovementRepository
                .Where(p => p.ProductId == detail.ProductId)
                .ToListAsync(cancellationToken);

            decimal stock = movements.Sum(p => p.NumberOfEntries - p.NumberOfOutputs);

            if (required > stock)
                return Result<string>.Failure(
                    $"{detail.Product!.Name} ürününden üretim için yeterli miktar yok. Eksik miktar: {required - stock}");

            decimal unitCost = AverageEntryPrice(movements);
            decimal remaining = required;

            // Stok birden çok depoya dağılmış olabiliyor; ihtiyaç karşılanana kadar
            // depolar sırayla düşülüyor.
            foreach (Guid depotId in movements.Select(p => p.DepotId).Distinct())
            {
                if (remaining <= 0) break;

                decimal available = movements
                    .Where(p => p.DepotId == depotId)
                    .Sum(s => s.NumberOfEntries - s.NumberOfOutputs);

                if (available <= 0) continue;

                decimal taken = Math.Min(remaining, available);

                newMovements.Add(new StockMovement
                {
                    Source = StockMovementSourceEnum.Production,
                    ProductionId = production.Id,
                    ProductId = detail.ProductId,
                    DepotId = depotId,
                    Price = unitCost,
                    NumberOfOutputs = taken
                });

                remaining -= taken;
                consumedCost += taken * unitCost;
            }
        }

        // Üretilen mamul stoğa giriyor. Bu hareket olmadan üretim kaydı açılıyor ama
        // ürünün stoğu hiç artmıyordu; sipariş de hiçbir zaman karşılanamıyordu.
        newMovements.Add(new StockMovement
        {
            Source = StockMovementSourceEnum.Production,
            ProductionId = production.Id,
            ProductId = request.ProductId,
            DepotId = request.DepotId,
            Price = consumedCost / request.Quantity,
            NumberOfEntries = request.Quantity
        });

        await stockMovementRepository.AddRangeAsync(newMovements, cancellationToken);
        await productionRepository.AddAsync(production, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Üretim kaydı oluşturuldu, mamul stoğa alındı.";
    }

    /// <summary>
    /// Girişlerin ağırlıklı ortalama birim maliyeti. Toplam tutarı hareket sayısına
    /// bölmek, farklı miktarlı girişlerde maliyeti tamamen yanlış veriyordu.
    /// </summary>
    private static decimal AverageEntryPrice(List<StockMovement> movements)
    {
        decimal enteredQuantity = movements.Sum(p => p.NumberOfEntries);

        if (enteredQuantity <= 0) return 0;

        decimal enteredAmount = movements.Sum(p => p.Price * p.NumberOfEntries);

        return enteredAmount / enteredQuantity;
    }
}
