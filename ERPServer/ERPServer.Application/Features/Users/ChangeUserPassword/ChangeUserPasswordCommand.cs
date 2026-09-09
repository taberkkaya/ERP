using MediatR;
using TS.Result;

namespace ERPServer.Application.Features.Users.ChangeUserPassword;

public sealed record ChangeUserPasswordCommand(Guid Id, string Password) : IRequest<Result<string>>;
