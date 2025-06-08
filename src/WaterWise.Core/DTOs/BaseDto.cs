namespace WaterWise.Core.DTOs
{
  public abstract class BaseDto
  {
    public List<LinkDto> Links { get; set; } = new();
  }

  public class LinkDto
  {
    public string? Href { get; set; }
    public string? Rel { get; set; }
    public string? Method { get; set; }
  }
}