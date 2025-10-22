using AdsApi.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AdsApi.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var traceId = ctx.TraceIdentifier;
        ctx.Response.Headers["X-Request-ID"] = traceId;

        ProblemDetails problem;
        var status = StatusCodes.Status500InternalServerError;
        var code = ErrorCodes.Unexpected;

        switch (ex)
        {
            case DomainValidationException dv:
                status = StatusCodes.Status400BadRequest;
                code = ErrorCodes.Validation;
                problem = new HttpValidationProblemDetails(dv.Errors) {
                    Title = dv.Message, Status = status, Type = "https://httpstatuses.com/400"
                };
                break;
            case NotFoundException nf:
                status = StatusCodes.Status404NotFound;
                code = ErrorCodes.NotFound;
                problem = new ProblemDetails { Title = nf.Message, Status = status, Type = "https://httpstatuses.com/404" };
                break;
            case ConflictException cf:
                status = StatusCodes.Status409Conflict;
                code = ErrorCodes.Conflict;
                problem = new ProblemDetails { Title = cf.Message, Status = status, Type = "https://httpstatuses.com/409" };
                break;
            default:
                problem = new ProblemDetails { Title = "An error occurred while processing your request.", Status = status, Type = "https://httpstatuses.com/500" };
                break;
        }

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;

        _logger.LogError(ex, "Unhandled error: code={Code} status={Status} trace={TraceId} path={Path}", code, status, traceId, ctx.Request.Path);
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem);
    }
}
