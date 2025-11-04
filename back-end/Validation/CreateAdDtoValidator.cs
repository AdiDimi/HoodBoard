using FluentValidation;

namespace AdsApi.Validation;

public class CreateProductDtoValidator : AbstractValidator<AdsApi.CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(3, 120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Category).MaximumLength(50).When(x => x.Category != null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).MaximumLength(1000).When(x => x.ImageUrl != null);
    }
}
