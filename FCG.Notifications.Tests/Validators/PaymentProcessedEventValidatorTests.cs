using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Domain.Enums;
using FCG.Notifications.Domain.Validators;
using Xunit;

namespace FCG.Notifications.Tests.Validators;

public sealed class PaymentProcessedEventValidatorTests
{
    private readonly PaymentProcessedEventValidator _sut = new();

    [Fact]
    public void PaymentProcessedEventValidator_AllFieldsValid_ValidationPasses()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved
        };

        var result = _sut.Validate(evt);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PaymentProcessedEventValidator_EmptyUserId_Fails_WithUserIdError()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.Empty,
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentProcessedEvent.UserId));
    }

    [Fact]
    public void PaymentProcessedEventValidator_EmptyGameId_Fails_WithGameIdError()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.Empty,
            Status = PaymentStatus.Approved
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentProcessedEvent.GameId));
    }

    [Fact]
    public void PaymentProcessedEventValidator_InvalidEnumValue_Fails_WithStatusError()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = (PaymentStatus)99
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentProcessedEvent.Status));
    }
}
