using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_NIVEL_DEGRADACAO_SOLO")]
  public class NivelDegradacaoSolo : BaseEntity
  {
    [Column("ID_NIVEL_DEGRADACAO")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdNivelDegradacao { get; set; }

    [Column("CODIGO_DEGRADACAO")]
    [Required]
    [StringLength(20)]
    public string CodigoDegradacao { get; set; }

    [Column("DESCRICAO_DEGRADACAO")]
    [Required]
    [StringLength(150)]
    public string DescricaoDegradacao { get; set; }

    [Column("NIVEL_NUMERICO")]
    [Required]
    [Range(1, 5)]
    public int NivelNumerico { get; set; }

    [Column("ACOES_CORRETIVAS")]
    [StringLength(500)]
    public string AcoesCorretivas { get; set; }
  }
}