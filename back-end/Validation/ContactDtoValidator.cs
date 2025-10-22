using FluentValidation;

namespace AdsApi.Validation;

public class ContactDtoValidator : AbstractValidator<AdsApi.ContactDto>
{
    public ContactDtoValidator()
    {
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Matches(@"^\+?[0-9\s\-]{6,20}$").When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
