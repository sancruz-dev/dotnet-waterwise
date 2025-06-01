using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_PRODUTOR_RURAL")]
  public class ProdutorRural : BaseEntity
  {
    [Required]
    [StringLength(100)]
    [Column("NOME_COMPLETO")]
    public string NomeCompleto { get; set; }

    [Required]
    [StringLength(18)]
    [Column("CPF_CNPJ")]
    public string CpfCnpj { get; set; }

    [Required]
    [StringLength(100)]
    [Column("EMAIL")]
    public string Email { get; set; }

    [StringLength(15)]
    [Column("TELEFONE")]
    public string? Telefone { get; set; }

    [Column("DATA_CADASTRO")]
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // Navigation Properties
    public virtual ICollection<PropriedadeRural> Propriedades { get; set; } = new List<PropriedadeRural>();
    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
  }
}