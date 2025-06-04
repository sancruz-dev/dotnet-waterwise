namespace WaterWise.Core.DTOs
{
  public class PropriedadeDto : BaseDto
  {
    public int Id { get; set; }
    public string? NomePropriedade { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AreaHectares { get; set; }
    public string? NomeProdutor { get; set; }
    public string? EmailProdutor { get; set; }
    public string? NivelDegradacao { get; set; }
    public int NivelNumerico { get; set; }
    public List<SensorDto> Sensores { get; set; } = new();
    public decimal? RiscoEnchente { get; set; } // ML.NET Prediction
  }
}