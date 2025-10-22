using FluentValidation;

namespace AdsApi.Validation;

public class LocationDtoValidator : AbstractValidator<AdsApi.LocationDto>
{
    public LocationDtoValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.Address).MaximumLength(200).When(x => x.Address != null);
    }
}
