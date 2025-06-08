using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

// Contadores para estatísticas
var alertsProcessed = 0;
var sensorDataProcessed = 0;
var startTime = DateTime.UtcNow;

Console.WriteLine("🚀 WaterWise NotificationService iniciando...");

// ✅ Configurar RabbitMQ connection e consumer em background
IConnection? connection = null;
IModel? channel = null;

try
{
    Console.WriteLine("🔍 Conectando ao RabbitMQ localhost:5672...");

    var factory = new ConnectionFactory()
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/",
        RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
    };

    connection = factory.CreateConnection("WaterWise-NotificationService");
    channel = connection.CreateModel();

    Console.WriteLine("✅ RabbitMQ conectado com sucesso!");

    // Configurar exchange e filas
    channel.ExchangeDeclare("waterwise.exchange", ExchangeType.Topic, durable: true);
    channel.QueueDeclare("waterwise.alerts", durable: true, exclusive: false, autoDelete: false);
    channel.QueueDeclare("waterwise.sensor.data", durable: true, exclusive: false, autoDelete: false);

    // Bind filas
    channel.QueueBind("waterwise.alerts", "waterwise.exchange", "alerts.*");
    channel.QueueBind("waterwise.sensor.data", "waterwise.exchange", "sensor.data.*");

    Console.WriteLine("📋 Filas e exchanges configurados");

    // ✅ Consumer para alertas
    var alertConsumer = new EventingBasicConsumer(channel);
    alertConsumer.Received += (model, ea) =>
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"🚨 ALERTA RECEBIDO - {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"📂 Routing Key: {routingKey}");
            Console.WriteLine($"📄 Conteúdo: {message}");
            Console.WriteLine(new string('=', 60));

            alertsProcessed++;
            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao processar alerta: {ex.Message}");
            channel.BasicReject(ea.DeliveryTag, true);
        }
    };

    // ✅ Consumer para dados de sensor
    var sensorConsumer = new EventingBasicConsumer(channel);
    sensorConsumer.Received += (model, ea) =>
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"📊 [SENSOR] {DateTime.Now:HH:mm:ss} - Dados recebidos");
            Console.WriteLine($"📄 Dados: {message}");

            sensorDataProcessed++;
            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao processar dados do sensor: {ex.Message}");
            channel.BasicReject(ea.DeliveryTag, true);
        }
    };

    // Configurar QoS e iniciar consumo
    channel.BasicQos(0, 10, false);
    channel.BasicConsume("waterwise.alerts", false, alertConsumer);
    channel.BasicConsume("waterwise.sensor.data", false, sensorConsumer);

    Console.WriteLine("🎯 Consumidores ativos nas filas:");
    Console.WriteLine("   📢 waterwise.alerts");
    Console.WriteLine("   📊 waterwise.sensor.data");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erro ao conectar RabbitMQ: {ex.Message}");
    Console.WriteLine("🔄 Continuando sem RabbitMQ...");
}

// ✅ Health Check Endpoint
app.MapGet("/health", () => new
{
    status = "running",
    service = "WaterWise NotificationService",
    timestamp = DateTime.UtcNow,
    uptime = DateTime.UtcNow - startTime,
    stats = new
    {
        alertsProcessed,
        sensorDataProcessed,
        rabbitMqConnected = connection?.IsOpen ?? false
    }
});

// ✅ Stats Endpoint
app.MapGet("/stats", () => new
{
    alertsProcessed,
    sensorDataProcessed,
    uptime = DateTime.UtcNow - startTime,
    lastCheck = DateTime.UtcNow,
    rabbitMqStatus = connection?.IsOpen ?? false
});

// ✅ Test Alert Endpoint
app.MapPost("/test-alert", (TestAlertDto alert) =>
{
    Console.WriteLine("\n" + new string('*', 50));
    Console.WriteLine($"🧪 TESTE MANUAL - {DateTime.Now:HH:mm:ss}");
    Console.WriteLine($"📝 Mensagem: {alert.Message}");
    Console.WriteLine($"🏷️ Tipo: {alert.Type}");
    Console.WriteLine($"⚠️ Severidade: {alert.Severity}");
    Console.WriteLine(new string('*', 50));

    return Results.Ok(new
    {
        success = true,
        message = "Teste de alerta processado",
        timestamp = DateTime.UtcNow
    });
});

// ✅ Root endpoint (resolver 404)
app.MapGet("/", () => new
{
    service = "WaterWise NotificationService",
    version = "1.0.0",
    status = "running",
    endpoints = new[]
    {
        "GET /health - Status do serviço",
        "GET /stats - Estatísticas",
        "POST /test-alert - Teste manual"
    }
});

// Cleanup ao finalizar
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("🛑 Finalizando NotificationService...");
    try
    {
        channel?.Close();
        connection?.Close();
        Console.WriteLine("✅ Conexões RabbitMQ fechadas");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erro ao fechar conexões: {ex.Message}");
    }
});

Console.WriteLine("🌟 NotificationService pronto!");
Console.WriteLine("🔗 URLs disponíveis:");
Console.WriteLine("   📊 http://localhost:5086/health");
Console.WriteLine("   📈 http://localhost:5086/stats");
Console.WriteLine("   🧪 POST http://localhost:5086/test-alert");
Console.WriteLine("   🏠 http://localhost:5086/");

app.Run();

// DTO para teste de alertas
public record TestAlertDto(string Message, string Type = "manual", string Severity = "info");