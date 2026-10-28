using System.Text.Json;
using FluentValidation;
using QueueSystem.API.Common;

namespace QueueSystem.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            object response = new
            {
                error = validationException.Message,
                statusCode = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            object response = new
            {
                error = unauthorizedAccessException.Message,
                statusCode = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (ApiException apiException)
        {
            context.Response.StatusCode = apiException.StatusCode;
            context.Response.ContentType = "application/json";

            object response = new
            {
                error = apiException.Message,
                statusCode = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            object response = new
            {
                error = "Įvyko klaida. Bandykite dar kartą.",
                statusCode = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
