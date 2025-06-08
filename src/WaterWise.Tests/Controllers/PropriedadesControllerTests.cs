using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.Entities;
using WaterWise.Core.DTOs;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using WaterWise.API;


namespace WaterWise.Tests.Controllers
{
  public class PropriedadesControllerTests : IClassFixture<WebApplicationFactory<Program>>
  {
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;


    public PropriedadesControllerTests(WebApplicationFactory<Program> factory)
    {
      _factory = factory.WithWebHostBuilder(builder =>
      {
        builder.ConfigureServices(services =>
          {
            // Remover o contexto real e usar InMemory
            var descriptor = services.SingleOrDefault(
                  d => d.ServiceType == typeof(DbContextOptions<WaterWiseContext>));
            if (descriptor != null)
              services.Remove(descriptor);

            services.AddDbContext<WaterWiseContext>(options =>
              {
                options.UseInMemoryDatabase("TestDb");
              });
          });
      });

      _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPropriedades_ShouldReturnOkResult()
    {
      // Arrange
      await SeedTestData();

      // Act
      var response = await _client.GetAsync("/api/v1/propriedades");

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
      var result = await response.Content.ReadFromJsonAsync<PagedResult<PropriedadeDto>>();
      result.Should().NotBeNull();
      result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPropriedade_WithValidId_ShouldReturnProperty()
    {
      // Arrange
      await SeedTestData();

      // Act
      var response = await _client.GetAsync("/api/v1/propriedades/1");

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
      var result = await response.Content.ReadFromJsonAsync<PropriedadeDto>();
      result.Should().NotBeNull();
      result.Id.Should().Be(1);
      result.Links.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPropriedade_WithInvalidId_ShouldReturnNotFound()
    {
      // Act
      var response = await _client.GetAsync("/api/v1/propriedades/999");

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePropriedade_WithValidData_ShouldCreateProperty()
    {
      // Arrange
      await SeedTestData();
      var createDto = new CreatePropriedadeDto
      {
        NomePropriedade = "Nova Propriedade Teste",
        Latitude = -23.5505m,
        Longitude = -46.6333m,
        AreaHectares = 100.5m,
        IdProdutor = 1,
        IdNivelDegradacao = 1
      };

      // Act
      var response = await _client.PostAsJsonAsync("/api/v1/propriedades", createDto);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
      var result = await response.Content.ReadFromJsonAsync<PropriedadeDto>();
      result.Should().NotBeNull();
      result.NomePropriedade.Should().Be(createDto.NomePropriedade);
      result.Links.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePropriedade_WithInvalidData_ShouldReturnBadRequest()
    {
      // Arrange
      var createDto = new CreatePropriedadeDto
      {
        // Nome faltando (obrigatório)
        Latitude = -23.5505m,
        Longitude = -46.6333m,
        AreaHectares = 100.5m,
        IdProdutor = 1,
        IdNivelDegradacao = 1
      };

      // Act
      var response = await _client.PostAsJsonAsync("/api/v1/propriedades", createDto);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePropriedade_WithValidData_ShouldUpdateProperty()
    {
      // Arrange
      await SeedTestData();
      var updateDto = new UpdatePropriedadeDto
      {
        Id = 1,
        NomePropriedade = "Propriedade Atualizada",
        Latitude = -23.5505m,
        Longitude = -46.6333m,
        AreaHectares = 150.0m,
        IdProdutor = 1,
        IdNivelDegradacao = 1
      };

      // Act
      var response = await _client.PutAsJsonAsync("/api/v1/propriedades/1", updateDto);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
      var result = await response.Content.ReadFromJsonAsync<PropriedadeDto>();
      result.Should().NotBeNull();
      result.NomePropriedade.Should().Be(updateDto.NomePropriedade);
    }

    [Fact]
    public async Task DeletePropriedade_WithValidId_ShouldDeleteProperty()
    {
      // Arrange
      await SeedTestData();

      // Act
      var response = await _client.DeleteAsync("/api/v1/propriedades/1");

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

      // Verificar se foi deletado
      var getResponse = await _client.GetAsync("/api/v1/propriedades/1");
      getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReceberDadosSensor_WithValidData_ShouldProcessData()
    {
      // Arrange
      await SeedTestData();
      var sensorData = new LeituraSensorInputDto
      {
        IdSensor = 1,
        UmidadeSolo = 45.5m,
        TemperaturaAr = 25.0m,
        PrecipitacaoMm = 10.0m
      };

      // Act
      var response = await _client.PostAsJsonAsync("/api/v1/propriedades/sensor-data", sensorData);

      // Assert
      response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
      var result = await response.Content.ReadFromJsonAsync<dynamic>();
      result.Should().NotBeNull();
    }

    private async Task SeedTestData()
    {
      using var scope = _factory.Services.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<WaterWiseContext>();

      // Garantir que o banco está limpo
      await context.Database.EnsureDeletedAsync();
      await context.Database.EnsureCreatedAsync();

      // Seed data
      var produtor = new ProdutorRural
      {
        Id = 1,
        NomeCompleto = "João Silva Teste",
        CpfCnpj = "123.456.789-00",
        Email = "joao@teste.com",
        Telefone = "(11) 99999-0001"
      };

      var nivelDegradacao = new NivelDegradacaoSolo
      {
        Id = 1,
        CodigoDegradacao = "BOM",
        DescricaoDegradacao = "Solo em bom estado",
        NivelNumerico = 2,
        AcoesCorretivas = "Manter práticas conservacionistas"
      };

      var tipoSensor = new TipoSensor
      {
        Id = 1,
        NomeTipo = "UMIDADE_SOLO",
        Descricao = "Sensor de umidade do solo",
        UnidadeMedida = "%",
        ValorMin = 0,
        ValorMax = 100
      };

      var propriedade = new PropriedadeRural
      {
        Id = 1,
        IdProdutor = 1,
        IdNivelDegradacao = 1,
        NomePropriedade = "Fazenda Teste",
        Latitude = -23.3234m,
        Longitude = -46.5678m,
        AreaHectares = 75.5m
      };

      var sensor = new SensorIoT
      {
        Id = 1,
        IdPropriedade = 1,
        IdTipoSensor = 1,
        ModeloDispositivo = "ESP32-Test"
      };

      context.ProdutoresRurais.Add(produtor);
      context.NiveisDegradacao.Add(nivelDegradacao);
      context.TiposSensores.Add(tipoSensor);
      context.PropriedadesRurais.Add(propriedade);
      context.SensoresIoT.Add(sensor);

      await context.SaveChangesAsync();
    }
  }
}