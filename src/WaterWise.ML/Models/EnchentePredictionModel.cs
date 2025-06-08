using Microsoft.ML.Data;

namespace WaterWise.ML.Models
{
  public class EnchentePredictionInput
  {
    [LoadColumn(0)]
    public float UmidadeSolo { get; set; }

    [LoadColumn(1)]
    public float TemperaturaAr { get; set; }

    [LoadColumn(2)]
    public float PrecipitacaoMm { get; set; }

    [LoadColumn(3)]
    public float AreaHectares { get; set; }

    [LoadColumn(4)]
    public float NivelDegradacao { get; set; }

    [LoadColumn(5)]
    public bool RiscoEnchente { get; set; }
  }

  public class EnchentePredictionOutput
  {
    [ColumnName("PredictedLabel")]
    public bool PredictedRiscoEnchente { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
  }
}