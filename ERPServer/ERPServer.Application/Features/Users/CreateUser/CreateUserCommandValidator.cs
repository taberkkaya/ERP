using FluentValidation;

namespace ERPServer.Application.Features.Users.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(p => p.FirstName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(p => p.LastName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(p => p.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(p => p.Email).NotEmpty().EmailAddress();
        RuleFor(p => p.Password).NotEmpty().MinimumLength(4);
    }
}
