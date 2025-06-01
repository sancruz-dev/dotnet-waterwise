using Microsoft.ML;
using WaterWise.ML.Models;
using WaterWise.Core.Entities;

namespace WaterWise.ML.Services
{
  public interface IMLPredictionService
  {
    Task<float> PredictFloodRiskAsync(PropriedadeRural propriedade, LeituraSensor ultimaLeitura);
    Task TrainModelAsync();
  }

  public class MLPredictionService : IMLPredictionService
  {
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly string _modelPath;

    public MLPredictionService()
    {
      _mlContext = new MLContext(seed: 0);

      // Ajustar o caminho do modelo para ser relativo ao diretório de trabalho atual
      var baseDirectory = AppContext.BaseDirectory;
      var assetsDirectory = Path.Combine(baseDirectory, "Assets");

      // Criar diretório Assets se não existir
      if (!Directory.Exists(assetsDirectory))
      {
        Directory.CreateDirectory(assetsDirectory);
      }

      _modelPath = Path.Combine(assetsDirectory, "flood_risk_model.zip");
      LoadModel();
    }

    public async Task<float> PredictFloodRiskAsync(PropriedadeRural propriedade, LeituraSensor ultimaLeitura)
    {
      if (_model == null)
      {
        await TrainModelAsync();
      }

      var input = new EnchentePredictionInput
      {
        UmidadeSolo = (float)(ultimaLeitura.UmidadeSolo ?? 30),
        TemperaturaAr = (float)(ultimaLeitura.TemperaturaAr ?? 25),
        PrecipitacaoMm = (float)(ultimaLeitura.PrecipitacaoMm ?? 0),
        AreaHectares = (float)propriedade.AreaHectares,
        NivelDegradacao = propriedade.IdNivelDegradacao
      };

      var predictionEngine = _mlContext.Model.CreatePredictionEngine<EnchentePredictionInput, EnchentePredictionOutput>(_model);
      var prediction = predictionEngine.Predict(input);

      return prediction.Probability;
    }

    public async Task TrainModelAsync()
    {
      try
      {
        // Dados sintéticos para treinamento
        var trainingData = GenerateTrainingData();
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Pipeline de treinamento
        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(EnchentePredictionInput.UmidadeSolo),
                nameof(EnchentePredictionInput.TemperaturaAr),
                nameof(EnchentePredictionInput.PrecipitacaoMm),
                nameof(EnchentePredictionInput.AreaHectares),
                nameof(EnchentePredictionInput.NivelDegradacao))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(EnchentePredictionInput.RiscoEnchente),
                featureColumnName: "Features"));

        // Treinar modelo
        _model = pipeline.Fit(dataView);

        // Garantir que o diretório existe antes de salvar
        var directory = Path.GetDirectoryName(_modelPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        // Salvar modelo
        _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

        Console.WriteLine($"Modelo ML treinado e salvo em: {_modelPath}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Erro ao treinar modelo ML: {ex.Message}");
        throw;
      }
    }

    private void LoadModel()
    {
      if (File.Exists(_modelPath))
      {
        try
        {
          _model = _mlContext.Model.Load(_modelPath, out _);
          Console.WriteLine($"Modelo ML carregado de: {_modelPath}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Erro ao carregar modelo ML: {ex.Message}");
          // Se falhar ao carregar, _model fica null e será treinado novamente
        }
      }
    }

    private List<EnchentePredictionInput> GenerateTrainingData()
    {
      // Gerar dados sintéticos para treinamento
      var data = new List<EnchentePredictionInput>();
      var random = new Random(42);

      for (int i = 0; i < 1000; i++)
      {
        var umidade = random.Next(0, 100);
        var temperatura = random.Next(15, 40);
        var precipitacao = random.Next(0, 200);
        var area = random.Next(1, 500);
        var degradacao = random.Next(1, 5);

        // Lógica simples: risco alto se precipitação alta, umidade baixa e degradação alta
        var riscoEnchente = precipitacao > 50 && umidade < 30 && degradacao >= 4;

        data.Add(new EnchentePredictionInput
        {
          UmidadeSolo = umidade,
          TemperaturaAr = temperatura,
          PrecipitacaoMm = precipitacao,
          AreaHectares = area,
          NivelDegradacao = degradacao,
          RiscoEnchente = riscoEnchente
        });
      }

      return data;
    }
  }
}