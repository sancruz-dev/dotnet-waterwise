using WaterWise.ML.Services;
using WaterWise.Core.Entities;
using FluentAssertions;
using Xunit;

namespace WaterWise.Tests.Services
{
  public class MLPredictionServiceTests
  {
    [Fact]
    public async Task PredictFloodRiskAsync_WithHighRainAndLowMoisture_ShouldReturnHighRisk()
    {
      // Arrange
      var mlService = new MLPredictionService();
      var propriedade = new PropriedadeRural
      {
        AreaHectares = 100,
        IdNivelDegradacao = 4 // Alto nível de degradação
      };
      var leitura = new LeituraSensor
      {
        UmidadeSolo = 15, // Baixa umidade
        TemperaturaAr = 30,
        PrecipitacaoMm = 80 // Alta precipitação
      };

      // Act
      var risk = await mlService.PredictFloodRiskAsync(propriedade, leitura);

      // Assert
      risk.Should().BeGreaterThan(0.5f); // Alto risco
    }

    [Fact]
    public async Task PredictFloodRiskAsync_WithLowRainAndHighMoisture_ShouldReturnLowRisk()
    {
      // Arrange
      var mlService = new MLPredictionService();
      var propriedade = new PropriedadeRural
      {
        AreaHectares = 50,
        IdNivelDegradacao = 1 // Baixo nível de degradação
      };
      var leitura = new LeituraSensor
      {
        UmidadeSolo = 70, // Alta umidade
        TemperaturaAr = 25,
        PrecipitacaoMm = 5 // Baixa precipitação
      };

      // Act
      var risk = await mlService.PredictFloodRiskAsync(propriedade, leitura);

      // Assert
      risk.Should().BeLessThan(0.5f); // Baixo risco
    }

    [Fact]
    public async Task TrainModelAsync_ShouldCompleteSuccessfully()
    {
      // Arrange
      var mlService = new MLPredictionService();

      // Act & Assert
      await FluentActions.Invoking(() => mlService.TrainModelAsync())
          .Should().NotThrowAsync();
    }
  }
}