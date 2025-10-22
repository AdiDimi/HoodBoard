namespace AdsApi.Middleware;
public static class RequestIdemAccessor
{
    private static readonly AsyncLocal<string?> _curr = new();
    public static string? Current { get => _curr.Value; set => _curr.Value = value; }
}
