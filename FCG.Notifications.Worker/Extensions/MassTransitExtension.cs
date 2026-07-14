using FCG.Notifications.Domain.Dto;
using FCG.Notifications.Worker.Consumers;
using MassTransit;

namespace FCG.Notifications.Worker.Extensions;

public static class MassTransitExtension
{
    public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:Host"] ?? "localhost";
        var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(bus =>
        {
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notifications", false));

            bus.AddConsumer<UserCreatedEventConsumer>();
            bus.AddConsumer<PaymentProcessedEventConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true);

                cfg.Message<UserCreatedEvent>(x => x.SetEntityName("user-created-event"));
                cfg.Message<PaymentProcessedEvent>(x => x.SetEntityName("payment-processed-event"));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
