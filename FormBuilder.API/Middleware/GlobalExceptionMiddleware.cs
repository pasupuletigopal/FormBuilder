using System.Net;
using System.Text.Json;

namespace FormBuilder.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = ex switch {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                InvalidOperationException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };
            var payload = JsonSerializer.Serialize(new {
                success = false,
                message = ex.Message,
                statusCode = ctx.Response.StatusCode
            });
            await ctx.Response.WriteAsync(payload);
        }
    }
}
