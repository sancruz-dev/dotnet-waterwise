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

            // ===========================================
            // CONFIGURAÇÕES SIMPLIFICADAS PARA ORACLE
            // ===========================================

            // As entidades já têm [Column] attributes, então só precisamos 
            // configurar relacionamentos e propriedades especiais

            modelBuilder.Entity<PropriedadeRural>(entity =>
            {
                entity.Property(p => p.Latitude)
                    .HasColumnType("NUMBER(10,7)");

                entity.Property(p => p.Longitude)
                    .HasColumnType("NUMBER(10,7)");

                entity.Property(p => p.AreaHectares)
                    .HasColumnType("NUMBER(10,2)");

                // Relacionamentos
                entity.HasOne(p => p.Produtor)
                    .WithMany(pr => pr.Propriedades)
                    .HasForeignKey(p => p.IdProdutor)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.NivelDegradacao)
                    .WithMany()
                    .HasForeignKey(p => p.IdNivelDegradacao)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LeituraSensor>(entity =>
            {
                entity.Property(l => l.UmidadeSolo)
                    .HasColumnType("NUMBER(5,2)");

                entity.Property(l => l.TemperaturaAr)
                    .HasColumnType("NUMBER(4,1)");

                entity.Property(l => l.PrecipitacaoMm)
                    .HasColumnType("NUMBER(6,2)");

                entity.HasOne(l => l.Sensor)
                    .WithMany(s => s.Leituras)
                    .HasForeignKey(l => l.IdSensor)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TipoSensor>(entity =>
            {
                entity.Property(t => t.ValorMin)
                    .HasColumnType("NUMBER(10,2)");

                entity.Property(t => t.ValorMax)
                    .HasColumnType("NUMBER(10,2)");

                entity.HasIndex(t => t.NomeTipo)
                    .IsUnique()
                    .HasDatabaseName("IX_TIPO_SENSOR_NOME");
            });

            modelBuilder.Entity<SensorIoT>(entity =>
            {
                entity.HasOne(s => s.Propriedade)
                    .WithMany(p => p.Sensores)
                    .HasForeignKey(s => s.IdPropriedade)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.TipoSensor)
                    .WithMany()
                    .HasForeignKey(s => s.IdTipoSensor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NivelDegradacaoSolo>(entity =>
            {
                entity.HasIndex(nd => nd.CodigoDegradacao)
                    .IsUnique()
                    .HasDatabaseName("IX_NIVEL_DEGRADACAO_CODIGO");
            });

            modelBuilder.Entity<NivelSeveridade>(entity =>
            {
                entity.HasIndex(ns => ns.CodigoSeveridade)
                    .IsUnique()
                    .HasDatabaseName("IX_NIVEL_SEVERIDADE_CODIGO");
            });

            modelBuilder.Entity<ProdutorRural>(entity =>
            {
                entity.HasIndex(pr => pr.CpfCnpj)
                    .IsUnique()
                    .HasDatabaseName("IX_PRODUTOR_CPF_CNPJ");

                entity.HasIndex(pr => pr.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_PRODUTOR_EMAIL");
            });

            modelBuilder.Entity<Alerta>(entity =>
            {
                entity.HasOne(a => a.Produtor)
                    .WithMany(p => p.Alertas)
                    .HasForeignKey(a => a.IdProdutor)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Leitura)
                    .WithMany(l => l.Alertas)
                    .HasForeignKey(a => a.IdLeitura)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.NivelSeveridade)
                    .WithMany()
                    .HasForeignKey(a => a.IdNivelSeveridade)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.EnableDetailedErrors();
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }
    }
}