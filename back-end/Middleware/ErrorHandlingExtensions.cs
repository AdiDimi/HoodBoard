namespace AdsApi.Middleware;
public static class ErrorHandlingExtensions
{
    public static IApplicationBuilder UseGlobalErrorHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
