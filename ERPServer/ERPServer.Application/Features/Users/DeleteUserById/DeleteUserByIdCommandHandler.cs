using ERPServer.Application.Services;
using ERPServer.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Users.DeleteUserById;

internal sealed class DeleteUserByIdCommandHandler(
    UserManager<AppUser> userManager,
    ICurrentUserService currentUser
    ) : IRequestHandler<DeleteUserByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteUserByIdCommand request, CancellationToken cancellationToken)
    {
        // Kendi hesabini silen kisi bir sonraki istekte disarida kalir.
        if (currentUser.UserId == request.Id)
            return Result<string>.Failure("Kendi hesabınızı silemezsiniz!");

        AppUser? user = await userManager.Users
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (user is null)
            return Result<string>.Failure("Kullanıcı bulunamadı!");

        // Son kullanici da silinirse sisteme hic kimse giremez.
        if (await userManager.Users.CountAsync(cancellationToken) <= 1)
            return Result<string>.Failure("Sistemdeki son kullanıcı silinemez!");

        IdentityResult result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(e => e.Description).ToList());

        return "Kullanıcı başarıyla silindi.";
    }
}
