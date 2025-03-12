using AutoMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Orders.UpdateOrder;

internal sealed class UpdateOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderDetailRepository orderDetailRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateOrderCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository
            .Where(p => p.Id == request.Id)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return Result<string>.Failure("Sipariş bulunamadı!");

        orderDetailRepository.DeleteRange(order.Details);

        List<OrderDetail> newDetails = request.Details.Select(s => new OrderDetail
        {
            OrderId = order.Id,
            Price = s.Price,
            Quantity = s.Quantity,
            ProductId = s.ProductId,
        }).ToList();

        await orderDetailRepository.AddRangeAsync(newDetails,cancellationToken);

        mapper.Map(request, order);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Sipariş başarıyla güncellendi.";
    }
}
