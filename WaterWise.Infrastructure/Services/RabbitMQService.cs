using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using WaterWise.Core.Services;

namespace WaterWise.Infrastructure.Services
{
  public class RabbitMQService : IRabbitMQService, IDisposable
  {
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string EXCHANGE_NAME = "waterwise.exchange";
    private const string ALERTS_QUEUE = "waterwise.alerts";
    private const string SENSOR_DATA_QUEUE = "waterwise.sensor.data";

    public RabbitMQService()
    {
      var factory = new ConnectionFactory()
      {
        HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
        UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
        Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest"
      };

      _connection = factory.CreateConnection();
      _channel = _connection.CreateModel();

      SetupExchangeAndQueues();
    }

    public async Task PublishAlertAsync(object message, string routingKey)
    {
      var json = JsonSerializer.Serialize(message);
      var body = Encoding.UTF8.GetBytes(json);

      _channel.BasicPublish(
          exchange: EXCHANGE_NAME,
          routingKey: $"alerts.{routingKey}",
          basicProperties: null,
          body: body);

      await Task.CompletedTask;
    }

    public async Task PublishSensorDataAsync(object sensorData)
    {
      var json = JsonSerializer.Serialize(sensorData);
      var body = Encoding.UTF8.GetBytes(json);

      _channel.BasicPublish(
          exchange: EXCHANGE_NAME,
          routingKey: "sensor.data.received",
          basicProperties: null,
          body: body);

      await Task.CompletedTask;
    }

    private void SetupExchangeAndQueues()
    {
      // Declarar exchange
      _channel.ExchangeDeclare(EXCHANGE_NAME, ExchangeType.Topic, durable: true);

      // Declarar filas
      _channel.QueueDeclare(ALERTS_QUEUE, durable: true, exclusive: false, autoDelete: false);
      _channel.QueueDeclare(SENSOR_DATA_QUEUE, durable: true, exclusive: false, autoDelete: false);

      // Bind filas ao exchange
      _channel.QueueBind(ALERTS_QUEUE, EXCHANGE_NAME, "alerts.*");
      _channel.QueueBind(SENSOR_DATA_QUEUE, EXCHANGE_NAME, "sensor.data.*");
    }

    public void Dispose()
    {
      _channel?.Dispose();
      _connection?.Dispose();
    }
  }
}