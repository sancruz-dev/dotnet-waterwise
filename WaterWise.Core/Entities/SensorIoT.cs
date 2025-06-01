using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_SENSOR_IOT")]
  public class SensorIoT : BaseEntity
  {
    [Column("ID_PROPRIEDADE")]
    public int IdPropriedade { get; set; }

    [Column("ID_TIPO_SENSOR")]
    public int IdTipoSensor { get; set; }

    [StringLength(50)]
    [Column("MODELO_DISPOSITIVO")]
    public string? ModeloDispositivo { get; set; }

    [Column("DATA_INSTALACAO")]
    public DateTime DataInstalacao { get; set; } = DateTime.Now;

    [Column("STATUS")]
    public string Status { get; set; } = "ATIVO";

    // Navigation Properties
    [ForeignKey("IdPropriedade")]
    public virtual PropriedadeRural Propriedade { get; set; }

    [ForeignKey("IdTipoSensor")]
    public virtual TipoSensor TipoSensor { get; set; }

    public virtual ICollection<LeituraSensor> Leituras { get; set; } = new List<LeituraSensor>();
  }
}