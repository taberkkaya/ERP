using ERPServer.Domain.Entities;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Depots.GetAllDepots;

public sealed record GetAllDepotsQuery() : IRequest<Result<List<Depot>>>;
