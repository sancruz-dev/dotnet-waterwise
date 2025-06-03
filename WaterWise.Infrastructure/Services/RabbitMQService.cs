using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using WaterWise.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WaterWise.Infrastructure.Services
{
  public class RabbitMQService : IRabbitMQService, IDisposable
  {
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly bool _isEnabled;
    private const string EXCHANGE_NAME = "waterwise.exchange";
    private const string ALERTS_QUEUE = "waterwise.alerts";
    private const string SENSOR_DATA_QUEUE = "waterwise.sensor.data";

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
      _logger = logger;
      _isEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", true);

      if (!_isEnabled)
      {
        _logger.LogWarning("RabbitMQ está desabilitado na configuração");
        return;
      }

      try
      {
        var factory = new ConnectionFactory()
        {
          HostName = configuration.GetValue<string>("RabbitMQ:Host", "localhost"),
          Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),
          UserName = configuration.GetValue<string>("RabbitMQ:Username", "guest"),
          Password = configuration.GetValue<string>("RabbitMQ:Password", "guest"),
          VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost", "/"),
          RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
          SocketReadTimeout = TimeSpan.FromSeconds(10),
          SocketWriteTimeout = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        SetupExchangeAndQueues();

        _logger.LogInformation("✅ RabbitMQ conectado com sucesso em {Host}:{Port}",
            factory.HostName, factory.Port);
      }
      catch (Exception ex)
      {
        _logger.LogWarning("⚠️ Erro ao conectar RabbitMQ: {Error}. Mensageria desabilitada.", ex.Message);
        _isEnabled = false;

        // Limpar recursos se a conexão falhou
        _channel?.Dispose();
        _connection?.Dispose();
      }
    }

    public async Task PublishAlertAsync(object message, string routingKey)
    {
      if (!_isEnabled || _channel == null)
      {
        _logger.LogDebug("RabbitMQ não disponível. Alerta não publicado: {RoutingKey}", routingKey);
        return;
      }

      try
      {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(
            exchange: EXCHANGE_NAME,
            routingKey: $"alerts.{routingKey}",
            basicProperties: null,
            body: body);

        _logger.LogDebug("📨 Alerta publicado: {RoutingKey}", routingKey);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "❌ Erro ao publicar alerta: {RoutingKey}", routingKey);
      }

      await Task.CompletedTask;
    }

    public async Task PublishSensorDataAsync(object sensorData)
    {
      if (!_isEnabled || _channel == null)
      {
        _logger.LogDebug("RabbitMQ não disponível. Dados do sensor não publicados.");
        return;
      }

      try
      {
        var json = JsonSerializer.Serialize(sensorData, new JsonSerializerOptions
        {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(
            exchange: EXCHANGE_NAME,
            routingKey: "sensor.data.received",
            basicProperties: null,
            body: body);

        _logger.LogDebug("📊 Dados do sensor publicados");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "❌ Erro ao publicar dados do sensor");
      }

      await Task.CompletedTask;
    }

    private void SetupExchangeAndQueues()
    {
      if (_channel == null) return;

      try
      {
        // Declarar exchange
        _channel.ExchangeDeclare(EXCHANGE_NAME, ExchangeType.Topic, durable: true);

        // Declarar filas
        _channel.QueueDeclare(ALERTS_QUEUE, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(SENSOR_DATA_QUEUE, durable: true, exclusive: false, autoDelete: false);

        // Bind filas ao exchange
        _channel.QueueBind(ALERTS_QUEUE, EXCHANGE_NAME, "alerts.*");
        _channel.QueueBind(SENSOR_DATA_QUEUE, EXCHANGE_NAME, "sensor.data.*");

        _logger.LogDebug("📋 Filas e exchanges configurados");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "❌ Erro ao configurar filas RabbitMQ");
        throw;
      }
    }

    public void Dispose()
    {
      try
      {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        if (_isEnabled)
        {
          _logger.LogInformation("🔌 Conexão RabbitMQ fechada");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "❌ Erro ao fechar conexão RabbitMQ");
      }
    }
  }
}