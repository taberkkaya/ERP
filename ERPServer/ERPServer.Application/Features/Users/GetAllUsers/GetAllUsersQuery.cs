using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Users.GetAllUsers;

public sealed record GetAllUsersQuery() : IRequest<Result<List<GetAllUsersQueryResponse>>>;

/// <summary>Parola hash bilgisi ve guvenlik damgasi gibi alanlar disari cikmiyor.</summary>
public sealed record GetAllUsersQueryResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
}
