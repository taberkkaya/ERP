using ERPServer.Domain.Entities;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Invoices.GetAllInvoices;

public sealed record GetAllInvoicesQuery(
    int Type) : IRequest<Result<List<Invoice>>>;
