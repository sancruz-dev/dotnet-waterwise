using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_SENSOR_IOT")]
  public class SensorIoT
  {
    [Key]
    [Column("ID_SENSOR")]  // mapeia para chave primária real
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("ID_PROPRIEDADE")]
    [Required]  // ← ADICIONADO: NOT NULL no Oracle
    public int IdPropriedade { get; set; }

    [Column("ID_TIPO_SENSOR")]
    [Required]  // ← ADICIONADO: NOT NULL no Oracle
    public int IdTipoSensor { get; set; }

    [StringLength(50)]
    [Column("MODELO_DISPOSITIVO")]
    public string? ModeloDispositivo { get; set; }  // pode ser null

    [Column("DATA_INSTALACAO")]
    public DateTime DataInstalacao { get; set; } = DateTime.Now;

    // // Navigation Properties
    [ForeignKey("IdPropriedade")]
    public virtual PropriedadeRural Propriedade { get; set; }

    [ForeignKey("IdTipoSensor")]
    public virtual TipoSensor TipoSensor { get; set; }

    public virtual ICollection<LeituraSensor> Leituras { get; set; } = new List<LeituraSensor>();
  }
}