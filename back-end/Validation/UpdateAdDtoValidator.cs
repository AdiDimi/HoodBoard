using FluentValidation;

namespace AdsApi.Validation;

public class UpdateAdDtoValidator : AbstractValidator<AdsApi.UpdateAdDto>
{
    public UpdateAdDtoValidator()
    {
        When(x => x.Title != null, () => RuleFor(x => x.Title!).Length(3, 120));
        When(x => x.Body != null, () => RuleFor(x => x.Body!).MaximumLength(5000));
        When(x => x.Category != null, () => RuleFor(x => x.Category!).MaximumLength(50));
        When(x => x.Price != null, () => RuleFor(x => x.Price!.Value).GreaterThanOrEqualTo(0));
        RuleForEach(x => x.Tags).MaximumLength(30);
        When(x => x.Contact != null, () => RuleFor(x => x.Contact!).SetValidator(new ContactDtoValidator()!));
        When(x => x.Location != null, () => RuleFor(x => x.Location!).SetValidator(new LocationDtoValidator()!));
    }
}
