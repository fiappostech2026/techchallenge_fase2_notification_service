using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Domain.Enums;
using FCG.Notifications.Domain.Services;
using Xunit;

namespace FCG.Notifications.Tests.Services;

public sealed class NotificationServiceTests
{
    private readonly NotificationService _sut = new();

    [Fact]
    public void BuildWelcomeEmailMessage_ValidEvent_ReturnsNonEmptyString_ContainingUserId()
    {
        var evt = new UserCreatedEvent { UserId = Guid.NewGuid(), Email = "user@example.com" };

        var result = _sut.BuildWelcomeEmailMessage(evt);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(evt.UserId.ToString(), result);
    }

    [Fact]
    public void BuildWelcomeEmailMessage_ValidEvent_ReturnsString_ContainingEmail()
    {
        var evt = new UserCreatedEvent { UserId = Guid.NewGuid(), Email = "user@example.com" };

        var result = _sut.BuildWelcomeEmailMessage(evt);

        Assert.Contains("user@example.com", result);
    }

    [Fact]
    public void BuildPurchaseConfirmationMessage_ApprovedStatus_ReturnsNonNullString_ContainingUserIdAndGameId()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Approved
        };

        var result = _sut.BuildPurchaseConfirmationMessage(evt);

        Assert.NotNull(result);
        Assert.Contains(evt.UserId.ToString(), result);
        Assert.Contains(evt.GameId.ToString(), result);
    }

    [Fact]
    public void BuildPurchaseConfirmationMessage_RejectedStatus_ReturnsNull()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = PaymentStatus.Rejected
        };

        var result = _sut.BuildPurchaseConfirmationMessage(evt);

        Assert.Null(result);
    }

    [Fact]
    public void BuildWelcomeEmailMessage_NullEvent_Throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.BuildWelcomeEmailMessage(null!));
    }

    [Fact]
    public void BuildPurchaseConfirmationMessage_NullEvent_Throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.BuildPurchaseConfirmationMessage(null!));
    }

    [Fact]
    public void BuildWelcomeEmailMessage_EmailWithScriptTag_ReturnsStringWithoutExecution()
    {
        var evt = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "<script>alert('xss')</script>"
        };

        var result = _sut.BuildWelcomeEmailMessage(evt);

        Assert.NotNull(result);
        Assert.Contains("<script>alert('xss')</script>", result);
    }

    [Fact]
    public void BuildPurchaseConfirmationMessage_StatusIntegerOutOfEnumRange_ReturnsNull()
    {
        var evt = new PaymentProcessedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Status = (PaymentStatus)99
        };

        var result = _sut.BuildPurchaseConfirmationMessage(evt);

        Assert.Null(result);
    }
}
