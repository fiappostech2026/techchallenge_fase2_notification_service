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
        // delay proposital para demo — dá tempo de ver a mensagem na fila no RabbitMQ Management UI
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("Falha na validação de PaymentProcessedEvent: {Errors}", result.ToString());
            return;
        }

        if (context.Message.Status != PaymentStatus.Approved)
            return;

        _logger.LogInformation("Confirmação de compra para {UserId} jogo {GameId}", context.Message.UserId, context.Message.GameId);
    }
}
