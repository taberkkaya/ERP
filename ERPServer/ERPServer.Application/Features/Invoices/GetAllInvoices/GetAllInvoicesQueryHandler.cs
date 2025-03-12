﻿using ERPServer.Domain.Entities;
using ERPServer.Domain.Enums;
using ERPServer.Domain.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Invoices.GetAllInvoices;

internal sealed class GetAllInvoicesQueryHandler(
    IInvoiceRepository invoiceRepository
    ) : IRequestHandler<GetAllInvoicesQuery, Result<List<Invoice>>>
{
    public async Task<Result<List<Invoice>>> Handle(GetAllInvoicesQuery request, CancellationToken cancellationToken)
    {
        List<Invoice> invoices = await invoiceRepository
            .Where(p => p.Type == InvoiceTypeEnum.FromValue(request.Type))
            .Include(p => p.Customer)
            .Include(p => p.Details!)
            .ThenInclude(p => p.Product)
            .Include(p => p.Details!)
            .ThenInclude(p => p.Depot)
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);

        return invoices;
    }
}
