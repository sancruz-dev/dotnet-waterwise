using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_TIPO_SENSOR")]
  public class TipoSensor : BaseEntity
  {
    [Column("ID_TIPO_SENSOR")]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTipoSensor { get; set; }

    [Column("NOME_TIPO")]
    [Required]
    [StringLength(50)]
    public string NomeTipo { get; set; }

    [Column("DESCRICAO")]
    [StringLength(200)]
    public string Descricao { get; set; }

    [Column("UNIDADE_MEDIDA")]
    [StringLength(20)]
    public string UnidadeMedida { get; set; }

    [Column("VALOR_MIN", TypeName = "NUMBER(10,2)")]
    public decimal? ValorMin { get; set; }

    [Column("VALOR_MAX", TypeName = "NUMBER(10,2)")]
    public decimal? ValorMax { get; set; }
  }
}