using FCG.Notifications.Domain.Interfaces.IService;
using FCG.Notifications.Domain.Services;
using FCG.Notifications.Domain.Validators;
using FluentValidation;

namespace FCG.Notifications.Worker.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        services.AddTransient<INotificationService, NotificationService>();
        services.AddValidatorsFromAssemblyContaining<UserCreatedEventValidator>();

        return services;
    }
}
