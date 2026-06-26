using FCG.Notifications.Worker.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();
builder.Services.AddDependencies();
builder.Services.AddMassTransitWithRabbitMQ(builder.Configuration);

var app = builder.Build();

app.UseErrorHandling();
app.Run();
