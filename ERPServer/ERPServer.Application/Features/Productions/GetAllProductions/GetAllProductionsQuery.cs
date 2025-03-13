using ERPServer.Domain.Entities;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Productions.GetAllProductions;

public sealed record GetAllProductionsQuery() : IRequest<Result<List<Production>>>;

