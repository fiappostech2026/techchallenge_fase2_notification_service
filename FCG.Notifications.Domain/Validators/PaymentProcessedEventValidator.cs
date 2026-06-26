using FCG.Notifications.Domain.Dto;
using FluentValidation;

namespace FCG.Notifications.Domain.Validators;

public sealed class PaymentProcessedEventValidator : AbstractValidator<PaymentProcessedEvent>
{
    public PaymentProcessedEventValidator()
    {
        RuleFor(evt => evt.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.GameId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.Status)
            .IsInEnum();
    }
}
