namespace WaterWise.Core.DTOs
{
  public class PropriedadeDto : BaseDto
  {
    public int Id { get; set; }
    public string NomePropriedade { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal AreaHectares { get; set; }
    public string NomeProdutor { get; set; }
    public string EmailProdutor { get; set; }
    public string NivelDegradacao { get; set; }
    public int NivelNumerico { get; set; }
    public List<SensorDto> Sensores { get; set; } = new();
    public decimal? RiscoEnchente { get; set; } // ML.NET Prediction
  }

  public abstract class BaseDto
  {
    public List<LinkDto> Links { get; set; } = new();
  }

  public class LinkDto
  {
    public string Href { get; set; }
    public string Rel { get; set; }
    public string Method { get; set; }
  }

  public class SensorDto
  {
    public int Id { get; set; }
    public string TipoSensor { get; set; }
    public string ModeloDispositivo { get; set; }
    public DateTime DataInstalacao { get; set; }
    public LeituraRecenteDto? UltimaLeitura { get; set; }
  }

  public class LeituraRecenteDto
  {
    public DateTime TimestampLeitura { get; set; }
    public decimal? UmidadeSolo { get; set; }
    public decimal? TemperaturaAr { get; set; }
    public decimal? PrecipitacaoMm { get; set; }
  }


}