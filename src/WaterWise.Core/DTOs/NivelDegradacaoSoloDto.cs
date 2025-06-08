namespace WaterWise.Core.DTOs
{
  public class NivelDegradacaoSoloDto : BaseDto
  {
    public int Id { get; set; }
    public string? CodigoDegradacao { get; set; }
    public string? DescricaoDegradacao { get; set; }
    public int NivelNumerico { get; set; }
    public string? AcoesCorretivas { get; set; }
  }

  public class CreateNivelDegradacaoSoloDto
  {
    public string? CodigoDegradacao { get; set; }

    public string? DescricaoDegradacao { get; set; }

    public int NivelNumerico { get; set; }

    public string? AcoesCorretivas { get; set; }
  }

  public class UpdateNivelDegradacaoSoloDto : CreateNivelDegradacaoSoloDto
  {
    public int Id { get; set; }
  }
}