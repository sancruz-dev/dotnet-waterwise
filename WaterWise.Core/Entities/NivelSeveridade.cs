using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_NIVEL_SEVERIDADE")]
  public class NivelSeveridade : BaseEntity
  {
    [Column("ID_NIVEL_SEVERIDADE")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdNivelSeveridade { get; set; }

    [Column("CODIGO_SEVERIDADE")]
    [Required]
    [StringLength(20)]
    public string CodigoSeveridade { get; set; }

    [Column("DESCRICAO_SEVERIDADE")]
    [Required]
    [StringLength(100)]
    public string DescricaoSeveridade { get; set; }

    [Column("ACOES_RECOMENDADAS")]
    [StringLength(500)]
    public string AcoesRecomendadas { get; set; }
  }
}