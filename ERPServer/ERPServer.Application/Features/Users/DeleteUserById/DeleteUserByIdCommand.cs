using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Users.DeleteUserById;

public sealed record DeleteUserByIdCommand(Guid Id) : IRequest<Result<string>>;
