using ERPServer.Domain.Dtos;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Orders.RequirementsPlanningByOrderId;

/// <summary>
/// Siparişi karşılamak için eksik olan bileşenleri çıkarır:
/// stoğu yetmeyen kalemler üretilecek miktar kadar reçeteleriyle patlatılır,
/// bileşenlerin brüt ihtiyacı toplanır ve en sonda bir kez stoktan düşülür.
/// </summary>
internal sealed class RequirementsPlanningByOrderIdCommandHandler(
    IOrderRepository orderRepository,
    IStockMovementRepository stockMovementRepository,
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork
    ) : IRequestHandler<RequirementsPlanningByOrderIdCommand, Result<RequirementsPlanningByOrderIdCommandResponse>>
{
    public async Task<Result<RequirementsPlanningByOrderIdCommandResponse>> Handle(
        RequirementsPlanningByOrderIdCommand request,
        CancellationToken cancellationToken)
    {
        Order? order = await orderRepository
            .Where(p => p.Id == request.OrderId)
            .Include(p => p.Details!)
            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return Result<RequirementsPlanningByOrderIdCommandResponse>.Failure("Sipariş bulunamadı!");

        // Bileşen kimliği -> (ad, brüt ihtiyaç). Aynı bileşen birden çok reçetede
        // geçebiliyor; ihtiyaçlar önce toplanıp stok bir kez düşülüyor, aksi hâlde
        // her reçete aynı stoğu yeniden sayıp eksiği olduğundan büyük gösteriyordu.
        Dictionary<Guid, ProductDto> requirements = new();

        foreach (OrderDetail item in order.Details ?? [])
        {
            decimal stock = await StockOfAsync(item.ProductId, cancellationToken);

            if (stock >= item.Quantity) continue;

            decimal toBeProduced = item.Quantity - stock;

            Recipe? recipe = await recipeRepository
                .Where(p => p.ProductId == item.ProductId)
                .Include(p => p.Details!)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(cancellationToken);

            if (recipe?.Details is null) continue;

            foreach (RecipeDetail detail in recipe.Details)
            {
                // Reçete bir birim için yazılır; üretilecek adetle ölçekleniyor.
                decimal required = detail.Quantity * toBeProduced;

                if (requirements.TryGetValue(detail.ProductId, out ProductDto? existing))
                {
                    existing.Quantity += required;
                    continue;
                }

                requirements.Add(detail.ProductId, new ProductDto
                {
                    Id = detail.ProductId,
                    Name = detail.Product!.Name,
                    Quantity = required
                });
            }
        }

        List<ProductDto> missing = new();

        foreach (ProductDto requirement in requirements.Values)
        {
            decimal stock = await StockOfAsync(requirement.Id, cancellationToken);

            if (requirement.Quantity <= stock) continue;

            requirement.Quantity -= stock;
            missing.Add(requirement);
        }

        order.Status = OrderStatusEnum.RequirementsPlanWorked;

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequirementsPlanningByOrderIdCommandResponse(
            DateOnly.FromDateTime(DateTime.Now),
            order.Number + " No'lu Siparişin İhtiyaç Planlaması",
            [.. missing.OrderBy(p => p.Name)]);
    }

    private async Task<decimal> StockOfAsync(Guid productId, CancellationToken cancellationToken) =>
        await stockMovementRepository
            .Where(p => p.ProductId == productId)
            .SumAsync(s => s.NumberOfEntries - s.NumberOfOutputs, cancellationToken);
}
