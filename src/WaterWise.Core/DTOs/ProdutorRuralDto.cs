using System.ComponentModel.DataAnnotations;

namespace WaterWise.Core.DTOs
{
  public class ProdutorRuralDto : BaseDto
  {
    public int Id { get; set; }
    public string NomeCompleto { get; set; }
    public string Email { get; set; }
    public string? Telefone { get; set; }
    public string CpfCnpj { get; set; }
    public string Senha { get; set; }
  }

  public class CreateProdutorRuralDto
  {
    [Required]
    [StringLength(100)]
    public string NomeCompleto { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; }

    [StringLength(15)]
    public string? Telefone { get; set; }

    [StringLength(18)]
    public string CpfCnpj { get; set; } // Opcional

    [Required]
    [StringLength(100)]
    public string Senha { get; set; }
  }

  public class UpdateProdutorRuralDto : CreateProdutorRuralDto
  {
    public int Id { get; set; }
  }

  public class LoginProdutorDto
  {
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; }

    [Required]
    [StringLength(100)]
    public string Senha { get; set; }
  }
}