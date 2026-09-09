using FluentValidation;

namespace ERPServer.Application.Features.Users.ChangeUserPassword;

public sealed class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
{
    public ChangeUserPasswordCommandValidator()
    {
        RuleFor(p => p.Password).NotEmpty().MinimumLength(4);
    }
}
