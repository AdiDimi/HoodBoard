using Serilog.Core;
using Serilog.Events;

namespace AdsApi.Infrastructure.Logging;

public sealed class InfoSuccessSamplingFilter : ILogEventFilter
{
    private readonly double _rate;
    public InfoSuccessSamplingFilter(double rate)
    {
        _rate = (rate <= 0) ? 0 : (rate >= 1) ? 1 : rate;
    }

    public bool IsEnabled(LogEvent logEvent)
    {
        if (logEvent.Level != LogEventLevel.Information) return true;
        if (!TryGetInt(logEvent, "StatusCode", out var status)) return true;
        if (status >= 400) return true;
        var key = TryGetString(logEvent, "requestId") ?? TryGetString(logEvent, "RequestPath");
        if (key is null) return true;
        uint h = Fnv1a32(key);
        double bucket = (h / (double)uint.MaxValue);
        return bucket < _rate;
    }

    private static bool TryGetInt(LogEvent e, string name, out int value)
    {
        value = 0;
        if (e.Properties.TryGetValue(name, out var p) && p is ScalarValue sv && sv.Value is int i) { value = i; return true; }
        return false;
    }
    private static string? TryGetString(LogEvent e, string name)
        => e.Properties.TryGetValue(name, out var v) && v is ScalarValue sv ? sv.Value?.ToString() : null;

    private static uint Fnv1a32(string s)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (var ch in s) { hash ^= ch; hash *= prime; }
        return hash;
    }
}
