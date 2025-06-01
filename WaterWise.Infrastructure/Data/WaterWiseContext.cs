using Microsoft.EntityFrameworkCore;
using WaterWise.Core.Entities;

namespace WaterWise.Infrastructure.Data
{
    public class WaterWiseContext : DbContext
    {
        public WaterWiseContext(DbContextOptions<WaterWiseContext> options)
            : base(options)
        {
        }

        // DbSets para as entidades
        public DbSet<PropriedadeRural> PropriedadesRurais { get; set; }
        public DbSet<Alerta> Alertas { get; set; }
        public DbSet<NivelSeveridade> NiveisSeveridade { get; set; }
        public DbSet<TipoSensor> TiposSensores { get; set; }
        public DbSet<NivelDegradacaoSolo> NiveisDegradacaoSolo { get; set; }
        public DbSet<SensorIoT> SensoresIoT { get; set; }
        public DbSet<LeituraSensor> LeiturasSensores { get; set; }
        public DbSet<ProdutorRural> ProdutoresRurais { get; set; }
        public DbSet<NivelDegradacaoSolo> NiveisDegradacao { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurações adicionais para o Oracle, se necessário
            modelBuilder.Entity<PropriedadeRural>()
                .Property(p => p.Latitude)
                .HasColumnType("NUMBER(10,7)");

            modelBuilder.Entity<PropriedadeRural>()
                .Property(p => p.Longitude)
                .HasColumnType("NUMBER(10,7)");

            modelBuilder.Entity<PropriedadeRural>()
                .Property(p => p.AreaHectares)
                .HasColumnType("NUMBER(10,2)");

            modelBuilder.Entity<TipoSensor>()
                .Property(t => t.ValorMin)
                .HasColumnType("NUMBER(10,2)");

            modelBuilder.Entity<TipoSensor>()
                .Property(t => t.ValorMax)
                .HasColumnType("NUMBER(10,2)");

            // Configurar chaves únicas
            modelBuilder.Entity<NivelSeveridade>()
                .HasIndex(ns => ns.CodigoSeveridade)
                .IsUnique();

            modelBuilder.Entity<TipoSensor>()
                .HasIndex(ts => ts.NomeTipo)
                .IsUnique();

            modelBuilder.Entity<NivelDegradacaoSolo>()
                .HasIndex(nd => nd.CodigoDegradacao)
                .IsUnique();
        }
    }
}