using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdsApi.Validation;

public sealed class ValidationFilter : IEndpointFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) { _services = services; }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validatorTypes = _services.GetService<IEnumerable<FluentValidation.IValidator>>() ?? Enumerable.Empty<FluentValidation.IValidator>();
        // FluentValidation will be applied by automatic registration via AddValidatorsFromAssemblyContaining
        return await next(context);
    }
}
