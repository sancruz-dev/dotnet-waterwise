namespace WaterWise.Core.DTOs
{
  public class LeituraRecenteDto
  {
    public DateTime TimestampLeitura { get; set; }
    public decimal? UmidadeSolo { get; set; }
    public decimal? TemperaturaAr { get; set; }
    public decimal? PrecipitacaoMm { get; set; }
  }
}