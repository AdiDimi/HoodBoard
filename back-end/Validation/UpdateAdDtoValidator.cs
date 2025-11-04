using FluentValidation;

namespace AdsApi.Validation;

public class UpdateProductDtoValidator : AbstractValidator<AdsApi.UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).Length(3, 120));
        When(x => x.Description != null, () => RuleFor(x => x.Description!).MaximumLength(5000));
        When(x => x.Category != null, () => RuleFor(x => x.Category!).MaximumLength(50));
        When(x => x.Price != null, () => RuleFor(x => x.Price!.Value).GreaterThanOrEqualTo(0));
        When(x => x.Stock != null, () => RuleFor(x => x.Stock!.Value).GreaterThanOrEqualTo(0));
        When(x => x.ImageUrl != null, () => RuleFor(x => x.ImageUrl!).MaximumLength(1000));
    }
}
