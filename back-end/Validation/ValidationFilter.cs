using FluentValidation;

namespace AdsApi.Validation;

public sealed class ValidationFilter : IEndpointFilter
{
    private readonly IServiceProvider _services;
    public ValidationFilter(IServiceProvider services) => _services = services;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        foreach (var arg in ctx.Arguments.Where(a => a is not null))
        {
            var t = arg!.GetType();
            if (t.IsPrimitive || t == typeof(string) || typeof(IFormFile).IsAssignableFrom(t)) continue;
            var vt = typeof(IValidator<>).MakeGenericType(t);
            var v = _services.GetService(vt);
            if (v is null) continue;
            var method = vt.GetMethod("ValidateAsync", new[] { t, typeof(CancellationToken) })!;
            var task = (Task)method.Invoke(v, new object[] { arg, ctx.HttpContext.RequestAborted })!;
            await task.ConfigureAwait(false);
            var resultProp = task.GetType().GetProperty("Result")!;
            var validationResult = (FluentValidation.Results.ValidationResult)resultProp.GetValue(task)!;
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return Results.ValidationProblem(errors);
            }
        }
        return await next(ctx);
    }
}
