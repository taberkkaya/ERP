using ERPServer.Application.Features.Recipes.CreateRecipe;
using ERPServer.Application.Features.Recipes.DeleteRecipeById;
using ERPServer.Application.Features.Recipes.GetAllRecipes;
using ERPServer.WebAPI.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ERPServer.WebAPI.Controllers
{
    public sealed class RecipesController : ApiController
    {
        public RecipesController(IMediator mediator) : base(mediator)
        {
        }

        [HttpPost]
        public async Task<IActionResult> GetAll(GetAllRecipesQuery request, CancellationToken cancellationToken) {
            var response = await _mediator.Send(request);
            return StatusCode(response.StatusCode,response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRecipeCommand request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(request);
            return StatusCode(response.StatusCode, response);
        }  
        
        [HttpPost]
        public async Task<IActionResult> DeleteById(DeleteRecipeByIdCommand request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(request);
            return StatusCode(response.StatusCode, response);
        }
    }
}
