using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password,
    bool IsAdmin) : IRequest<Result<string>>;
