using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WaterWise.Core.Entities
{
  [Table("GS_WW_PROPRIEDADE_RURAL")]
  public class PropriedadeRural
  {
    [Key]
    [Column("ID_PROPRIEDADE")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("ID_PRODUTOR")]
    [Required]
    public int IdProdutor { get; set; }

    [Column("ID_NIVEL_DEGRADACAO")]
    [Required]
    public int IdNivelDegradacao { get; set; }

    [Required]
    [StringLength(100)]
    [Column("NOME_PROPRIEDADE")]
    public string NomePropriedade { get; set; }

    [Column("LATITUDE", TypeName = "NUMBER(10,7)")]
    [Required]
    public decimal Latitude { get; set; }

    [Column("LONGITUDE", TypeName = "NUMBER(10,7)")]
    [Required]
    public decimal Longitude { get; set; }

    [Column("AREA_HECTARES", TypeName = "NUMBER(10,2)")]
    [Required]
    public decimal AreaHectares { get; set; }

    [Column("DATA_CADASTRO")]
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // Navigation Properties
    [ForeignKey("IdProdutor")]
    public virtual ProdutorRural Produtor { get; set; }

    [ForeignKey("IdNivelDegradacao")]
    public virtual NivelDegradacaoSolo NivelDegradacao { get; set; }

    public virtual ICollection<SensorIoT> Sensores { get; set; } = new List<SensorIoT>();
  }
}