using ERPServer.Domain.Entities;
using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Recipes.GetAllRecipes;

public sealed record GetAllRecipesQuery() : IRequest<Result<List<Recipe>>>;
