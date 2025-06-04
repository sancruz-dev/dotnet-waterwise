using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.DTOs;
using WaterWise.Core.Entities;
using WaterWise.Core.Services;

namespace WaterWise.API.Controllers
{
  [ApiController]
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiVersion("1.0")]
  public class SensorController : ControllerBase
  {
    private readonly WaterWiseContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<PropriedadesController> _logger;

    public SensorController(
        WaterWiseContext context,
        IRabbitMQService rabbitMQService,
        ILogger<PropriedadesController> logger)
    {
      _context = context;
      _rabbitMQService = rabbitMQService;
      _logger = logger;
    }

    /// <summary>
    /// Receives sensor data from IoT devices
    /// </summary>
    /// <param name="inputDto">Sensor reading data</param>
    /// <returns>Processing confirmation</returns>
    [HttpPost("sensor-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceberDadosSensor([FromBody] LeituraSensorInputDto inputDto)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var sensor = await _context.SensoresIoT
          .Include(s => s.Propriedade)
              .ThenInclude(p => p.Produtor)
          .FirstOrDefaultAsync(s => s.Id == inputDto.IdSensor);

      if (sensor == null)
        return BadRequest(new { error = "Sensor não encontrado" });

      // ✅ SOLUÇÃO: Verificar se já existe leitura recente
      var ultimaLeitura = await _context.LeiturasSensores
          .Where(l => l.IdSensor == inputDto.IdSensor)
          .OrderByDescending(l => l.TimestampLeitura)
          .FirstOrDefaultAsync();

      var agora = DateTime.Now;

      // Evitar leituras duplicadas no mesmo segundo
      if (ultimaLeitura != null &&
          Math.Abs((agora - ultimaLeitura.TimestampLeitura).TotalSeconds) < 1)
      {
        // Adicionar alguns milissegundos para evitar duplicação
        agora = ultimaLeitura.TimestampLeitura.AddMilliseconds(
            new Random().Next(100, 999));
      }

      var leitura = new LeituraSensor
      {
        IdSensor = inputDto.IdSensor,
        TimestampLeitura = agora, // ✅ Usar timestamp calculado
        UmidadeSolo = inputDto.UmidadeSolo,
        TemperaturaAr = inputDto.TemperaturaAr,
        PrecipitacaoMm = inputDto.PrecipitacaoMm
      };

      try
      {
        _context.LeiturasSensores.Add(leitura);
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("ORA-00001") == true)
      {
        // ✅ Tratamento específico para constraint violation
        return Conflict(new
        {
          error = "Leitura duplicada detectada. Tente novamente em alguns segundos.",
          details = "Uma leitura similar já foi registrada para este sensor."
        });
      }

      // Resto do código permanece igual...
      await _rabbitMQService.PublishSensorDataAsync(new
      {
        SensorId = leitura.IdSensor,
        PropertyName = sensor.Propriedade.NomePropriedade,
        ProducerName = sensor.Propriedade.Produtor.NomeCompleto,
        Reading = leitura,
        Timestamp = DateTime.UtcNow
      });

      await VerificarAlertas(leitura, sensor);

      return Ok(new
      {
        success = true,
        idLeitura = leitura.Id,
        timestamp = leitura.TimestampLeitura,
        message = "Dados do sensor processados com sucesso"
      });
    }

    private async Task VerificarAlertas(LeituraSensor leitura, SensorIoT sensor)
    {
      var alertas = new List<object>();

      // Alerta para umidade baixa
      if (leitura.UmidadeSolo.HasValue && leitura.UmidadeSolo < 20)
      {
        var alerta = new
        {
          TipoAlerta = "UMIDADE_CRITICA",
          Severidade = "ALTO",
          Propriedade = sensor.Propriedade.NomePropriedade,
          Produtor = sensor.Propriedade.Produtor.NomeCompleto,
          Valor = leitura.UmidadeSolo,
          Timestamp = DateTime.UtcNow,
          Mensagem = $"Umidade crítica detectada: {leitura.UmidadeSolo}%"
        };

        alertas.Add(alerta);
        await _rabbitMQService.PublishAlertAsync(alerta, "umidade.critica");
      }

      // Alerta para chuva intensa
      if (leitura.PrecipitacaoMm.HasValue && leitura.PrecipitacaoMm > 50)
      {
        var alerta = new
        {
          TipoAlerta = "PRECIPITACAO_INTENSA",
          Severidade = "CRITICO",
          Propriedade = sensor.Propriedade.NomePropriedade,
          Produtor = sensor.Propriedade.Produtor.NomeCompleto,
          Valor = leitura.PrecipitacaoMm,
          Timestamp = DateTime.UtcNow,
          Mensagem = $"Precipitação intensa: {leitura.PrecipitacaoMm}mm/h - Risco de enchente"
        };

        alertas.Add(alerta);
        await _rabbitMQService.PublishAlertAsync(alerta, "precipitacao.intensa");
      }

      if (alertas.Any())
      {
        _logger.LogWarning("Alertas gerados para sensor {SensorId}: {AlertCount} alertas",
            leitura.IdSensor, alertas.Count);
      }
    }


  }
}