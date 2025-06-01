using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

// Consumidor RabbitMQ para processar alertas
var factory = new ConnectionFactory() { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var routingKey = ea.RoutingKey;

    Console.WriteLine($"[NotificationService] Received alert: {routingKey} - {message}");

    // Aqui você processaria o alerta (enviar email, SMS, push notification, etc.)
    ProcessAlert(message, routingKey);
};

channel.BasicConsume(queue: "waterwise.alerts", autoAck: true, consumer: consumer);

app.MapGet("/health", () => "Notification Service is running");

app.Run();

static void ProcessAlert(string message, string routingKey)
{
    try
    {
        var alert = JsonSerializer.Deserialize<object>(message);
        // Implementar lógica de notificação
        Console.WriteLine($"Processing alert of type: {routingKey}");
        // Exemplo: enviar email, SMS, push notification
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing alert: {ex.Message}");
    }
}