using FluentValidation;

namespace ERPServer.Application.Features.Depots.CreateDepot;

public sealed class CreateDepotCommandValidator : AbstractValidator<CreateDepotCommand>
{
    public CreateDepotCommandValidator()
    {
        RuleFor(p => p.Name)
         .MinimumLength(3);
    }
}