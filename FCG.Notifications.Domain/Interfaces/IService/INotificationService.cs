using FCG.Notifications.Domain.Dto;

namespace FCG.Notifications.Domain.Interfaces.IService;

public interface INotificationService
{
    string BuildWelcomeEmailMessage(UserCreatedEvent evt);
    string? BuildPurchaseConfirmationMessage(PaymentProcessedEvent evt);
}
