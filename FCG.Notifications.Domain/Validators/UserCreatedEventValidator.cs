using FCG.Notifications.Domain.Dto;
using FluentValidation;

namespace FCG.Notifications.Domain.Validators;

public sealed class UserCreatedEventValidator : AbstractValidator<UserCreatedEvent>
{
    public UserCreatedEventValidator()
    {
        RuleFor(evt => evt.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}
