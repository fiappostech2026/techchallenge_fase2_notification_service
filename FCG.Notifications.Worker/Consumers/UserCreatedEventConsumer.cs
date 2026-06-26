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
        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("UserCreatedEvent validation failed: {Errors}", result.ToString());
            return;
        }

        _logger.LogInformation("Welcome email simulation for {UserId} with {Email}", context.Message.UserId, context.Message.Email);
    }
}
