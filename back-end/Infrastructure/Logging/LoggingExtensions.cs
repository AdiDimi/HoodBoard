using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;

namespace AdsApi.Infrastructure.Logging;

public static class LoggingExtensions
{
    public static void AddStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) =>
        {
            var rate = ctx.Configuration.GetValue<double?>("Logging:Sampling:InfoSuccessRate") ?? 0.10;
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .Enrich.FromLogContext()
               .Enrich.WithMachineName()
               .Enrich.WithProcessId()
               .Enrich.WithThreadId()
               .Filter.With(new InfoSuccessSamplingFilter(rate))
               .WriteTo.Console(new JsonFormatter());
        });
    }

    public static void UseStructuredRequestLogging(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var reqId = ctx.TraceIdentifier;
            ctx.Response.Headers["X-Request-ID"] = reqId;
            using (LogContext.PushProperty("requestId", reqId))
            using (LogContext.PushProperty("method", ctx.Request.Method))
            using (LogContext.PushProperty("path", ctx.Request.Path.ToString()))
            using (LogContext.PushProperty("queryString", ctx.Request.QueryString.Value))
            {
                await next();
            }
        });

        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms";
            opts.EnrichDiagnosticContext = (diag, http) =>
            {
                diag.Set("requestId", http.TraceIdentifier);
                diag.Set("clientIp", http.Connection.RemoteIpAddress?.ToString());
                if (http.Request.Headers.TryGetValue("Idempotency-Key", out var key))
                    diag.Set("idempotencyKey", (string)key);
                if (http.User?.Identity?.IsAuthenticated == true)
                    diag.Set("user", http.User.Identity!.Name);
            };
        });
    }
}
