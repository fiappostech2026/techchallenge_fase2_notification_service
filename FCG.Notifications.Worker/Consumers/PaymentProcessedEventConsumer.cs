using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Domain.Enums;
using FluentValidation;
using MassTransit;

namespace FCG.Notifications.Worker.Consumers;

public sealed class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly IValidator<PaymentProcessedEvent> _validator;
    private readonly ILogger<PaymentProcessedEventConsumer> _logger;

    public PaymentProcessedEventConsumer(
        IValidator<PaymentProcessedEvent> validator,
        ILogger<PaymentProcessedEventConsumer> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("PaymentProcessedEvent validation failed: {Errors}", result.ToString());
            return;
        }

        if (context.Message.Status != PaymentStatus.Approved)
            return;

        _logger.LogInformation("Purchase confirmation for {UserId} game {GameId}", context.Message.UserId, context.Message.GameId);
    }
}
