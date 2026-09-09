using ERPServer.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Users.ChangeUserPassword;

internal sealed class ChangeUserPasswordCommandHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<ChangeUserPasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        AppUser? user = await userManager.Users
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (user is null)
            return Result<string>.Failure("Kullanıcı bulunamadı!");

        // Eski parola sorulmuyor: bu bir yonetici sifirlamasi. Sifirlama jetonu
        // uretip onunla degistirmek, parola ozetini elle yazmaktan guvenli.
        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult result = await userManager.ResetPasswordAsync(user, token, request.Password);

        if (!result.Succeeded)
            return Result<string>.Failure(result.Errors.Select(e => e.Description).ToList());

        return "Parola güncellendi.";
    }
}
