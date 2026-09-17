using ERPServer.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TS.Result;

namespace ERPServer.Application.Features.Users.GetAllUsers;

internal sealed class GetAllUsersQueryHandler(
    UserManager<AppUser> userManager
    ) : IRequestHandler<GetAllUsersQuery, Result<List<GetAllUsersQueryResponse>>>
{
    public async Task<Result<List<GetAllUsersQueryResponse>>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        List<GetAllUsersQueryResponse> users = await userManager.Users
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .Select(p => new GetAllUsersQueryResponse
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                FullName = p.FirstName + " " + p.LastName,
                UserName = p.UserName ?? string.Empty,
                Email = p.Email ?? string.Empty,
                IsAdmin = p.IsAdmin
            })
            .ToListAsync(cancellationToken);

        return users;
    }
}
