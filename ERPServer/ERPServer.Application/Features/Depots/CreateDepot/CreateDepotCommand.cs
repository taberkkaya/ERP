using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Depots.CreateDepot;

public sealed record CreateDepotCommand(
    string Name,
    string City,
    string Town,
    string Address
    ): IRequest<Result<string>>;
