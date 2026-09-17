using ERPServer.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Users.UpdateUser;

internal sealed class UpdateUserCommandHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<UpdateUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.Users
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (user is null)
            return Result<string>.Failure("Kullanıcı bulunamadı!");

        bool isUserNameTaken = await userManager.Users
            .AnyAsync(p => p.Id != request.Id && p.UserName == request.UserName, cancellationToken);

        if (isUserNameTaken)
            return Result<string>.Failure("Bu kullanıcı adı zaten kullanılıyor!");

        bool isEmailTaken = await userManager.Users
            .AnyAsync(p => p.Id != request.Id && p.Email == request.Email, cancellationToken);

        if (isEmailTaken)
            return Result<string>.Failure("Bu e-posta adresi zaten kayıtlı!");

        // Son yöneticinin yetkisini almak, hesabı silmek kadar kesin biçimde
        // kullanıcı yönetimini kilitler.
        if (user.IsAdmin && !request.IsAdmin)
        {
            bool anotherAdminExists = await userManager.Users
                .AnyAsync(p => p.IsAdmin && p.Id != user.Id, cancellationToken);

            if (!anotherAdminExists)
                return Result<string>.Failure(
                    "Sistemdeki son yöneticinin yetkisi kaldırılamaz. Önce başka bir yönetici tanımlayın.");
        }

        user.IsAdmin = request.IsAdmin;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.UserName = request.UserName;
        user.Email = request.Email;

        IdentityResult result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(e => e.Description).ToList());

        return "Kullanıcı bilgileri güncellendi.";
    }
}
