using System.Text.Json;
using Clinic.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clinic.Api.Middleware;

/// <summary>
/// Converts application exceptions into consistent JSON error responses so the
/// frontend gets structured errors (validation dictionary, 404, 403) instead of
/// leaking stack traces. Unhandled exceptions become a generic 500 in production.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        switch (ex)
        {
            case ValidationException vex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteAsync(context, new { message = ex.Message, errors = vex.Errors });
                break;

            case NotFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await WriteAsync(context, new { message = ex.Message });
                break;

            case ForbiddenException:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await WriteAsync(context, new { message = ex.Message });
                break;

            case AppException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteAsync(context, new { message = ex.Message });
                break;

            default:
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await WriteAsync(context, new
                {
                    message = "An unexpected error occurred.",
                    detail = env.IsDevelopment() ? ex.Message : null
                });
                break;
        }
    }

    private static Task WriteAsync(HttpContext context, object payload) =>
        context.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
