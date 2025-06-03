using Microsoft.EntityFrameworkCore;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.Entities;

namespace WaterWise.API.Extensions
{
  public static class DatabaseSeeder
  {
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
      using var scope = serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<WaterWiseContext>();

      try
      {
        Console.WriteLine("🔍 Verificando se as tabelas existem...");

        // Tentar uma operação simples para verificar se as tabelas existem
        bool tablesExist = await CheckIfTablesExistAsync(context);

        if (!tablesExist)
        {
          Console.WriteLine("📋 Tabelas não encontradas. Criando estrutura do banco...");

          // Tentar criar as tabelas
          await context.Database.EnsureCreatedAsync();
          Console.WriteLine("✅ Estrutura do banco criada");
        }

        // Verificar se já tem dados usando uma abordagem mais segura
        bool hasData = await CheckIfHasDataAsync(context);

        if (hasData)
        {
          Console.WriteLine("📊 Base de dados já possui dados. Seed ignorado.");
          return;
        }

        Console.WriteLine("🌱 Iniciando seed da base de dados...");

        // Seed dados em ordem de dependência
        await SeedNiveisDegradacaoAsync(context);
        await SeedTiposSensoresAsync(context);
        await SeedNiveisSeveridadeAsync(context);
        await SeedProdutoresAsync(context);
        await SeedPropriedadesAsync(context);
        await SeedSensoresAsync(context);
        await SeedLeiturasAsync(context);

        Console.WriteLine("✅ Seed da base de dados concluído com sucesso!");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"❌ Erro durante o seed: {ex.Message}");
        Console.WriteLine($"🔍 Detalhes: {ex.InnerException?.Message}");

        // Não relançar a exceção para não quebrar a inicialização da API
        Console.WriteLine("⚠️ Continuando sem seed de dados...");
      }
    }

    private static async Task<bool> CheckIfTablesExistAsync(WaterWiseContext context)
    {
      try
      {
        // Tentar fazer uma consulta simples em uma tabela
        var count = await context.Database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = 'GS_WW_PRODUTOR_RURAL'"
        );
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static async Task<bool> CheckIfHasDataAsync(WaterWiseContext context)
    {
      try
      {
        // Usar uma abordagem mais direta para verificar dados
        var produtorCount = await context.Database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM GS_WW_PRODUTOR_RURAL WHERE ROWNUM = 1"
        );
        return produtorCount > 0;
      }
      catch
      {
        // Se der erro, assumir que não tem dados
        return false;
      }
    }

    private static async Task SeedNiveisDegradacaoAsync(WaterWiseContext context)
    {
      try
      {
        var niveisDegradacao = new List<NivelDegradacaoSolo>
                {
                    new() {
                        CodigoDegradacao = "EXCELENTE",
                        DescricaoDegradacao = "Solo em excelente estado",
                        NivelNumerico = 1,
                        AcoesCorretivas = "Manter práticas atuais"
                    },
                    new() {
                        CodigoDegradacao = "BOM",
                        DescricaoDegradacao = "Solo em bom estado",
                        NivelNumerico = 2,
                        AcoesCorretivas = "Monitoramento regular"
                    },
                    new() {
                        CodigoDegradacao = "REGULAR",
                        DescricaoDegradacao = "Solo necessita atenção",
                        NivelNumerico = 3,
                        AcoesCorretivas = "Implementar práticas conservacionistas"
                    },
                    new() {
                        CodigoDegradacao = "RUIM",
                        DescricaoDegradacao = "Solo degradado",
                        NivelNumerico = 4,
                        AcoesCorretivas = "Recuperação urgente necessária"
                    },
                    new() {
                        CodigoDegradacao = "CRITICO",
                        DescricaoDegradacao = "Solo criticamente degradado",
                        NivelNumerico = 5,
                        AcoesCorretivas = "Intervenção imediata obrigatória"
                    }
                };

        await context.NiveisDegradacao.AddRangeAsync(niveisDegradacao);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {niveisDegradacao.Count} níveis de degradação inseridos");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir níveis de degradação: {ex.Message}");
      }
    }

    private static async Task SeedTiposSensoresAsync(WaterWiseContext context)
    {
      try
      {
        var tiposSensores = new List<TipoSensor>
                {
                    new() {
                        NomeTipo = "UMIDADE_SOLO",
                        Descricao = "Sensor de umidade do solo",
                        UnidadeMedida = "%",
                        ValorMin = 0,
                        ValorMax = 100
                    },
                    new() {
                        NomeTipo = "TEMPERATURA",
                        Descricao = "Sensor de temperatura do ar",
                        UnidadeMedida = "°C",
                        ValorMin = -10,
                        ValorMax = 50
                    },
                    new() {
                        NomeTipo = "PRECIPITACAO",
                        Descricao = "Pluviômetro",
                        UnidadeMedida = "mm/h",
                        ValorMin = 0,
                        ValorMax = 500
                    },
                    new() {
                        NomeTipo = "PH_SOLO",
                        Descricao = "Sensor de pH do solo",
                        UnidadeMedida = "pH",
                        ValorMin = 0,
                        ValorMax = 14
                    }
                };

        await context.TiposSensores.AddRangeAsync(tiposSensores);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {tiposSensores.Count} tipos de sensores inseridos");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir tipos de sensores: {ex.Message}");
      }
    }

    private static async Task SeedNiveisSeveridadeAsync(WaterWiseContext context)
    {
      try
      {
        var niveisSeveridade = new List<NivelSeveridade>
                {
                    new() {
                        CodigoSeveridade = "BAIXO",
                        DescricaoSeveridade = "Risco baixo",
                        AcoesRecomendadas = "Monitoramento contínuo"
                    },
                    new() {
                        CodigoSeveridade = "MEDIO",
                        DescricaoSeveridade = "Risco médio",
                        AcoesRecomendadas = "Alertar produtor e monitorar"
                    },
                    new() {
                        CodigoSeveridade = "ALTO",
                        DescricaoSeveridade = "Risco alto",
                        AcoesRecomendadas = "Ação preventiva imediata"
                    },
                    new() {
                        CodigoSeveridade = "CRITICO",
                        DescricaoSeveridade = "Risco crítico",
                        AcoesRecomendadas = "Evacuação e medidas emergenciais"
                    }
                };

        await context.NiveisSeveridade.AddRangeAsync(niveisSeveridade);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {niveisSeveridade.Count} níveis de severidade inseridos");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir níveis de severidade: {ex.Message}");
      }
    }

    private static async Task SeedProdutoresAsync(WaterWiseContext context)
    {
      try
      {
        var produtores = new List<ProdutorRural>
                {
                    new() {
                        NomeCompleto = "João Silva Santos",
                        CpfCnpj = "123.456.789-00",
                        Email = "joao.silva@email.com",
                        Telefone = "(11) 99999-0001"
                    },
                    new() {
                        NomeCompleto = "Maria Oliveira Costa",
                        CpfCnpj = "987.654.321-00",
                        Email = "maria.oliveira@email.com",
                        Telefone = "(11) 99999-0002"
                    },
                    new() {
                        NomeCompleto = "Carlos Eduardo Lima",
                        CpfCnpj = "456.789.123-00",
                        Email = "carlos.lima@email.com",
                        Telefone = "(11) 99999-0003"
                    }
                };

        await context.ProdutoresRurais.AddRangeAsync(produtores);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {produtores.Count} produtores inseridos");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir produtores: {ex.Message}");
      }
    }

    private static async Task SeedPropriedadesAsync(WaterWiseContext context)
    {
      try
      {
        var propriedades = new List<PropriedadeRural>
                {
                    new() {
                        IdProdutor = 1,
                        IdNivelDegradacao = 2,
                        NomePropriedade = "Fazenda São João",
                        Latitude = -23.5505m,
                        Longitude = -46.6333m,
                        AreaHectares = 150.5m
                    },
                    new() {
                        IdProdutor = 2,
                        IdNivelDegradacao = 1,
                        NomePropriedade = "Sítio Boa Vista",
                        Latitude = -23.5489m,
                        Longitude = -46.6388m,
                        AreaHectares = 75.2m
                    },
                    new() {
                        IdProdutor = 3,
                        IdNivelDegradacao = 3,
                        NomePropriedade = "Rancho Verde",
                        Latitude = -23.5601m,
                        Longitude = -46.6528m,
                        AreaHectares = 200.0m
                    }
                };

        await context.PropriedadesRurais.AddRangeAsync(propriedades);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {propriedades.Count} propriedades inseridas");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir propriedades: {ex.Message}");
      }
    }

    private static async Task SeedSensoresAsync(WaterWiseContext context)
    {
      try
      {
        var sensores = new List<SensorIoT>
                {
                    new() {
                        IdPropriedade = 1,
                        IdTipoSensor = 1,
                        ModeloDispositivo = "ESP32-SOIL-001",
                        Status = "ATIVO"
                    },
                    new() {
                        IdPropriedade = 1,
                        IdTipoSensor = 2,
                        ModeloDispositivo = "ESP32-TEMP-001",
                        Status = "ATIVO"
                    },
                    new() {
                        IdPropriedade = 1,
                        IdTipoSensor = 3,
                        ModeloDispositivo = "ESP32-RAIN-001",
                        Status = "ATIVO"
                    },
                    new() {
                        IdPropriedade = 2,
                        IdTipoSensor = 1,
                        ModeloDispositivo = "ESP32-SOIL-002",
                        Status = "ATIVO"
                    },
                    new() {
                        IdPropriedade = 3,
                        IdTipoSensor = 2,
                        ModeloDispositivo = "ESP32-TEMP-003",
                        Status = "MANUTENCAO"
                    }
                };

        await context.SensoresIoT.AddRangeAsync(sensores);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {sensores.Count} sensores inseridos");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir sensores: {ex.Message}");
      }
    }

    private static async Task SeedLeiturasAsync(WaterWiseContext context)
    {
      try
      {
        var leituras = new List<LeituraSensor>
                {
                    new() {
                        IdSensor = 1,
                        UmidadeSolo = 45.5m,
                        TimestampLeitura = DateTime.Now.AddHours(-1)
                    },
                    new() {
                        IdSensor = 2,
                        TemperaturaAr = 25.0m,
                        TimestampLeitura = DateTime.Now.AddHours(-1)
                    },
                    new() {
                        IdSensor = 3,
                        PrecipitacaoMm = 5.0m,
                        TimestampLeitura = DateTime.Now.AddHours(-1)
                    },
                    new() {
                        IdSensor = 4,
                        UmidadeSolo = 38.2m,
                        TimestampLeitura = DateTime.Now.AddMinutes(-30)
                    }
                };

        await context.LeiturasSensores.AddRangeAsync(leituras);
        await context.SaveChangesAsync();
        Console.WriteLine($"   ✅ {leituras.Count} leituras de exemplo inseridas");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"   ⚠️ Erro ao inserir leituras: {ex.Message}");
      }
    }
  }
}