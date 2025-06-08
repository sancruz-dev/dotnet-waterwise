using System.ComponentModel.DataAnnotations;

namespace WaterWise.Core.DTOs
{
  public class CreatePropriedadeDto
  {
    [Required]
    [StringLength(100)]
    public string? NomePropriedade { get; set; }

    [Required]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [Required]
    [Range(0.1, 10000)]
    public decimal AreaHectares { get; set; }

    [Required]
    public int IdProdutor { get; set; }

    [Required]
    public int IdNivelDegradacao { get; set; }
  }

  public class UpdatePropriedadeDto : CreatePropriedadeDto
  {
    public int Id { get; set; }
  }

  public class LeituraSensorInputDto
  {
    [Required]
    public int IdSensor { get; set; }

    [Range(0, 100)]
    public decimal? UmidadeSolo { get; set; }

    [Range(-50, 60)]
    public decimal? TemperaturaAr { get; set; }

    [Range(0, 500)]
    public decimal? PrecipitacaoMm { get; set; }
  }
}