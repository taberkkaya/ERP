using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Users.UpdateUser;

public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    bool IsAdmin) : IRequest<Result<string>>;
