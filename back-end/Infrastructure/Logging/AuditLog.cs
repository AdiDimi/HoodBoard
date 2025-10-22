namespace AdsApi.Infrastructure.Logging;
public static class AuditLog
{
    public static IDisposable Begin() => Serilog.Context.LogContext.PushProperty("audit", true);
}
