using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Domain.Enums;
using FCG.Notifications.Domain.Interfaces.IService;

namespace FCG.Notifications.Domain.Services;

public sealed class NotificationService : INotificationService
{
    public string BuildWelcomeEmailMessage(UserCreatedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return $"Welcome email — UserId: {evt.UserId}, Email: {evt.Email}";
    }

    public string? BuildPurchaseConfirmationMessage(PaymentProcessedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (evt.Status != PaymentStatus.Approved)
            return null;

        return $"Purchase confirmed — UserId: {evt.UserId}, GameId: {evt.GameId}";
    }
}
