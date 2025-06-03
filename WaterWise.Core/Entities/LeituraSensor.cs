using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_LEITURA_SENSOR")]
  public class LeituraSensor
  {
    [Key]
    [Column("ID_LEITURA")]  // ← CORREÇÃO: Mapear para a coluna real do Oracle
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("ID_SENSOR")]
    [Required]  // ← Adicionar Required pois é NOT NULL no Oracle
    public int IdSensor { get; set; }

    [Column("TIMESTAMP_LEITURA")]
    [Required]
    public DateTime TimestampLeitura { get; set; } = DateTime.Now;

    [Column("UMIDADE_SOLO", TypeName = "NUMBER(5,2)")]
    public decimal? UmidadeSolo { get; set; }

    [Column("TEMPERATURA_AR", TypeName = "NUMBER(4,1)")]
    public decimal? TemperaturaAr { get; set; }

    [Column("PRECIPITACAO_MM", TypeName = "NUMBER(6,2)")]
    public decimal? PrecipitacaoMm { get; set; }

    // Navigation Properties
    [ForeignKey("IdSensor")]
    public virtual SensorIoT Sensor { get; set; }

    public virtual ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
  }
}