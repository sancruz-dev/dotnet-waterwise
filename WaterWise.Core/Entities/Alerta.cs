using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_ALERTA")]
  public class Alerta : BaseEntity
  {
    [Column("ID_ALERTA")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAlerta { get; set; }

    [Column("ID_PRODUTOR")]
    [Required]
    public int IdProdutor { get; set; }

    [Column("ID_LEITURA")]
    [Required]
    public int IdLeitura { get; set; }

    [Column("ID_NIVEL_SEVERIDADE")]
    [Required]
    public int IdNivelSeveridade { get; set; }

    [Column("TIMESTAMP_ALERTA")]
    public DateTime TimestampAlerta { get; set; } = DateTime.Now;

    [Column("DESCRICAO_ALERTA")]
    [StringLength(500)]
    public string DescricaoAlerta { get; set; }

    // Navigation Properties
    [ForeignKey("IdProdutor")]
    public virtual ProdutorRural Produtor { get; set; }

    [ForeignKey("IdLeitura")]
    public virtual LeituraSensor Leitura { get; set; }

    [ForeignKey("IdNivelSeveridade")]
    public virtual NivelSeveridade NivelSeveridade { get; set; }
  }
}