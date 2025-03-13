using ERPServer.Application.Features.Productions.CreateProduction;
using ERPServer.Application.Features.Productions.DeleteProductionById;
using ERPServer.Application.Features.Productions.GetAllProductions;
using ERPServer.WebAPI.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ERPServer.WebAPI.Controllers
{
    public sealed class ProductionsController : ApiController
    {
        public ProductionsController(IMediator mediator) : base(mediator)
        {
        }

        [HttpPost]
        public async Task<IActionResult> GetAll(GetAllProductionsQuery request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(request);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductionCommand request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(request);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteById(DeleteProductionByIdCommand request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(request);
            return StatusCode(response.StatusCode, response);
        }
    }
}
