using FCG.Notifications.Domain.Dto;
using FluentValidation;
using MassTransit;

namespace FCG.Notifications.Worker.Consumers;

public sealed class UserCreatedEventConsumer : IConsumer<UserCreatedEvent>
{
    private readonly IValidator<UserCreatedEvent> _validator;
    private readonly ILogger<UserCreatedEventConsumer> _logger;

    public UserCreatedEventConsumer(
        IValidator<UserCreatedEvent> validator,
        ILogger<UserCreatedEventConsumer> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        // delay proposital para demo — dá tempo de ver a mensagem na fila no RabbitMQ Management UI
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("Falha na validação de UserCreatedEvent: {Errors}", result.ToString());
            return;
        }

        _logger.LogInformation("Simulação de e-mail de boas-vindas para {UserId} com {Email}", context.Message.UserId, context.Message.Email);
    }
}
