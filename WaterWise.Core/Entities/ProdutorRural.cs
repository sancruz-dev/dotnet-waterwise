using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_PRODUTOR_RURAL")]
  public class ProdutorRural
  {
    [Key]
    [Column("ID_PRODUTOR")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]  //  (NOT NULL no Oracle)
    [StringLength(100)]
    [Column("NOME_COMPLETO")]
    public string NomeCompleto { get; set; }

    [StringLength(18)]
    [Column("CPF_CNPJ")]
    public string? CpfCnpj { get; set; }  // ← Agora nullable

    [Required]  //  (NOT NULL no Oracle)
    [StringLength(100)]
    [Column("EMAIL")]
    public string Email { get; set; }

    [StringLength(15)]
    [Column("TELEFONE")]
    public string? Telefone { get; set; }

    [StringLength(100)]
    [Column("SENHA")]
    public string? Senha { get; set; }

    [Column("DATA_CADASTRO")]
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // Navigation Properties
    public virtual ICollection<PropriedadeRural> Propriedades { get; set; } = new List<PropriedadeRural>();
    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
  }
}