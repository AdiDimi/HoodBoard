using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using static AdsApi.Endpoints.AdsEndpoints;

namespace AdsApi.Repositories;

public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async context =>
        {
            var details = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            try
            {
                var mux = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                var db = mux.GetDatabase();
                var latency = await db.PingAsync();
                details["redis"] = new { status = "ok", latency = latency.TotalMilliseconds };
                await context.Response.WriteAsJsonAsync(new { status = "healthy", details });
               
            }
            catch (Exception ex)
            {
                details["error"] = ex.Message;
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { status = "unhealthy", details });
            }
           
        })
        .WithName("HealthCheck")
        .WithSummary("Checks API and Redis connectivity")
        .WithOpenApi();

        return app;
    }
}
