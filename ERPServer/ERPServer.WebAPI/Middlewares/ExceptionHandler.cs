using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using TS.Result;

namespace ERPServer.WebAPI.Middlewares
{
    public class ExceptionHandler : IExceptionHandler
    {
        /// <summary>
        /// Denetleyicilerden donen yanitlar camelCase; elle serilestirilen hata
        /// govdesi PascalCase kaliyordu ve istemci hata mesajini bulamiyordu.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            httpContext.Response.ContentType = "application/json";

            Result<string> errorResult;

            if (exception is ValidationException validationException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

                errorResult = Result<string>.Failure(
                    403,
                    validationException.Errors.Select(s => s.PropertyName).ToList());
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                errorResult = Result<string>.Failure(exception.Message);
            }

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(errorResult, SerializerOptions),
                cancellationToken);

            return true;
        }
    }
}
