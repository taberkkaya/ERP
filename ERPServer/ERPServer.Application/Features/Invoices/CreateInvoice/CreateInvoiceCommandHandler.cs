using AutoMapper;
using ERPServer.Domain.Entities;
using ERPServer.Domain.Repository;
using GenericRepository;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Invoices.CreateInvoice;

internal sealed class CreateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IStockMovementRepository stockMovementRepository
    ) : IRequestHandler<CreateInvoiceCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        Invoice invoice = mapper.Map<Invoice>(request);

        if (invoice.Details is not null)
        {
            List<StockMovement> movements = new();

            foreach (var detail in invoice.Details)
            {
                StockMovement stockMovement = new()
                {
                    InvoiceId = invoice.Id,
                    NumberOfEntries = request.TypeValue == 1 ? detail.Quantity : 0,
                    NumberOfOutputs = request.TypeValue == 2 ? detail.Quantity : 0,
                    DepotId = detail.DepotId,
                    Price = detail.Price,
                    ProductId = detail.ProductId
                };

                movements.Add(stockMovement);
            }
            await stockMovementRepository.AddRangeAsync(movements,cancellationToken);
        }

        await invoiceRepository.AddAsync(invoice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Sipariş başarıyla oluşturuldu.";
    }
}
