using FCG.Notifications.Domain.Enums;

namespace FCG.Notifications.Domain.Dto;

public sealed class PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public PaymentStatus Status { get; init; }
}
