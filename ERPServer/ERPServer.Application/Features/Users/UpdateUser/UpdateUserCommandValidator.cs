using FluentValidation;

namespace ERPServer.Application.Features.Users.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(p => p.FirstName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(p => p.LastName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(p => p.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(p => p.Email).NotEmpty().EmailAddress();
    }
}
