using FluentValidation;

namespace AdsApi.Validation;

public class CreateAdDtoValidator : AbstractValidator<AdsApi.CreateAdDto>
{
    public CreateAdDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().Length(3, 120);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Category).MaximumLength(50).When(x => x.Category != null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleForEach(x => x.Tags).NotEmpty().MaximumLength(30);
        When(x => x.Contact != null, () => RuleFor(x => x.Contact!).SetValidator(new ContactDtoValidator()!));
        When(x => x.Location != null, () => RuleFor(x => x.Location!).SetValidator(new LocationDtoValidator()!));
    }
}
