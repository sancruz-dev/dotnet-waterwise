namespace WaterWise.Core.Services
{
  public interface IRabbitMQService
  {
    Task PublishAlertAsync(object message, string routingKey);
    Task PublishSensorDataAsync(object sensorData);
  }
}