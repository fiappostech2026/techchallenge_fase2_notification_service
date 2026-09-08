using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Domain.Interfaces.IService;
using FCG.Notifications.Domain.Services;
using FCG.Notifications.Domain.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FCG.Notifications.Function;

public sealed class Function
{
    // JSON no wire vem em PascalCase — mesma estrutura publicada pelo UsersAPI/PaymentsAPI via
    // MassTransit UseRawJsonSerializer, sem envelope. PropertyNameCaseInsensitive cobre variações.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<Function> _logger;
    private readonly INotificationService _notificationService = new NotificationService();
    private readonly IValidator<UserCreatedEvent> _userCreatedValidator = new UserCreatedEventValidator();
    private readonly IValidator<PaymentProcessedEvent> _paymentProcessedValidator = new PaymentProcessedEventValidator();

    public Function(ILogger<Function> logger)
    {
        _logger = logger;
    }

    [Function(nameof(HandleUserCreated))]
    public async Task HandleUserCreated(
        [RabbitMQTrigger("notifications-user-created-event", ConnectionStringSetting = "RabbitMqConnection")] string message)
    {
        var evt = JsonSerializer.Deserialize<UserCreatedEvent>(message, JsonOptions);
        if (evt is null)
            return;

        var result = await _userCreatedValidator.ValidateAsync(evt);
        if (!result.IsValid)
        {
            _logger.LogWarning("Falha na validação de UserCreatedEvent: {Errors}", result);
            return;
        }

        _logger.LogInformation("{Message}", _notificationService.BuildWelcomeEmailMessage(evt));
    }

    [Function(nameof(HandlePaymentProcessed))]
    public async Task HandlePaymentProcessed(
        [RabbitMQTrigger("notifications-payment-processed-event", ConnectionStringSetting = "RabbitMqConnection")] string message)
    {
        var evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(message, JsonOptions);
        if (evt is null)
            return;

        var result = await _paymentProcessedValidator.ValidateAsync(evt);
        if (!result.IsValid)
        {
            _logger.LogWarning("Falha na validação de PaymentProcessedEvent: {Errors}", result);
            return;
        }

        var confirmationMessage = _notificationService.BuildPurchaseConfirmationMessage(evt);
        if (confirmationMessage is not null)
            _logger.LogInformation("{Message}", confirmationMessage);
    }
}
