using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Domain.Validators;
using Xunit;

namespace FCG.Notifications.Tests.Validators;

public sealed class UserCreatedEventValidatorTests
{
    private readonly UserCreatedEventValidator _sut = new();

    [Fact]
    public void UserCreatedEventValidator_AllFieldsValid_ValidationPasses()
    {
        var evt = new UserCreatedEvent { UserId = Guid.NewGuid(), Email = "valid@example.com" };

        var result = _sut.Validate(evt);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void UserCreatedEventValidator_EmptyUserId_Fails_WithUserIdError()
    {
        var evt = new UserCreatedEvent { UserId = Guid.Empty, Email = "valid@example.com" };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UserCreatedEvent.UserId));
    }

    [Fact]
    public void UserCreatedEventValidator_EmptyEmail_Fails_WithEmailError()
    {
        var evt = new UserCreatedEvent { UserId = Guid.NewGuid(), Email = "" };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UserCreatedEvent.Email));
    }

    [Fact]
    public void UserCreatedEventValidator_InvalidEmailFormat_Fails_WithEmailError()
    {
        var evt = new UserCreatedEvent { UserId = Guid.NewGuid(), Email = "not-an-email" };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UserCreatedEvent.Email));
    }

    [Fact]
    public void UserCreatedEventValidator_EmailOf300Characters_Fails_WithLengthError()
    {
        var evt = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Email = new string('a', 292) + "@example.com"
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UserCreatedEvent.Email));
    }
}
