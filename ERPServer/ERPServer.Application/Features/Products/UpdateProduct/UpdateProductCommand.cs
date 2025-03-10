using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ERPServer.Domain.Enums;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    int ProductTypeValue) : IRequest<Result<string>>;

