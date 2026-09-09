using ERPServer.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Users.CreateUser;

internal sealed class CreateUserCommandHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<CreateUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        bool isUserNameTaken = await userManager.Users
            .AnyAsync(p => p.UserName == request.UserName, cancellationToken);

        if (isUserNameTaken)
            return Result<string>.Failure("Bu kullanıcı adı zaten kullanılıyor!");

        bool isEmailTaken = await userManager.Users
            .AnyAsync(p => p.Email == request.Email, cancellationToken);

        if (isEmailTaken)
            return Result<string>.Failure("Bu e-posta adresi zaten kayıtlı!");

        AppUser user = new()
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,

            // Kurulumda e-posta onayi zorunlu (SignIn.RequireConfirmedEmail).
            // Onayli isaretlenmezse kullanici olusuyor ama hic giris yapamiyor.
            EmailConfirmed = true
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(e => e.Description).ToList());

        return "Kullanıcı başarıyla oluşturuldu.";
    }
}
