namespace WaterWise.Core.DTOs
{
  public class SensorDto
  {
    public int Id { get; set; }
    public string? TipoSensor { get; set; }
    public string? ModeloDispositivo { get; set; }
    public DateTime DataInstalacao { get; set; }
    public LeituraRecenteDto? UltimaLeitura { get; set; }
  }
}