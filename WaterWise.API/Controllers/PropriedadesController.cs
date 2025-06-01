using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.DTOs;
using WaterWise.Core.Entities;
using WaterWise.Core.Services;
using WaterWise.ML.Services;

namespace WaterWise.API.Controllers
{
  [ApiController]
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiVersion("1.0")]
  public class PropriedadesController : ControllerBase
  {
    private readonly WaterWiseContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IMLPredictionService _mlService;
    private readonly ILogger<PropriedadesController> _logger;

    public PropriedadesController(
        WaterWiseContext context,
        IRabbitMQService rabbitMQService,
        IMLPredictionService mlService,
        ILogger<PropriedadesController> logger)
    {
      _context = context;
      _rabbitMQService = rabbitMQService;
      _mlService = mlService;
      _logger = logger;
    }

    /// <summary>
    /// Retrieves all rural properties with pagination support
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <returns>Paginated list of properties with HATEOAS links</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PropriedadeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PropriedadeDto>>> GetPropriedades(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
      var totalItems = await _context.PropriedadesRurais.CountAsync();
      var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

      var propriedades = await _context.PropriedadesRurais
          .Include(p => p.Produtor)
          .Include(p => p.NivelDegradacao)
          .Include(p => p.Sensores)
              .ThenInclude(s => s.TipoSensor)
          .Include(p => p.Sensores)
              .ThenInclude(s => s.Leituras.OrderByDescending(l => l.TimestampLeitura).Take(1))
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync();

      var propriedadesDto = new List<PropriedadeDto>();

      foreach (var propriedade in propriedades)
      {
        var dto = MapToDto(propriedade);

        // Adicionar predição ML.NET
        var ultimaLeitura = propriedade.Sensores
            .SelectMany(s => s.Leituras)
            .OrderByDescending(l => l.TimestampLeitura)
            .FirstOrDefault();

        if (ultimaLeitura != null)
        {
          // Converter float para decimal? explicitamente
          var riscoEnchente = await _mlService.PredictFloodRiskAsync(propriedade, ultimaLeitura);
          dto.RiscoEnchente = (decimal?)riscoEnchente;
        }

        // Adicionar HATEOAS links
        AddHateoasLinks(dto);
        propriedadesDto.Add(dto);
      }

      var result = new PagedResult<PropriedadeDto>
      {
        Items = propriedadesDto,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems,
        TotalPages = totalPages
      };

      // HATEOAS para paginação
      AddPaginationLinks(result, page, totalPages);

      return Ok(result);
    }


    /// <summary>
    /// Retrieves a specific rural property by ID
    /// </summary>
    /// <param name="id">Property ID</param>
    /// <returns>Property details with HATEOAS links</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PropriedadeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropriedadeDto>> GetPropriedade(int id)
    {
      var propriedade = await _context.PropriedadesRurais
          .Include(p => p.Produtor)
          .Include(p => p.NivelDegradacao)
          .Include(p => p.Sensores)
              .ThenInclude(s => s.TipoSensor)
          .Include(p => p.Sensores)
              .ThenInclude(s => s.Leituras.OrderByDescending(l => l.TimestampLeitura).Take(10))
          .FirstOrDefaultAsync(p => p.Id == id);

      if (propriedade == null)
        return NotFound(new { error = $"Propriedade com ID {id} não encontrada" });

      var dto = MapToDto(propriedade);

      // ML.NET Prediction
      var ultimaLeitura = propriedade.Sensores
          .SelectMany(s => s.Leituras)
          .OrderByDescending(l => l.TimestampLeitura)
          .FirstOrDefault();

      if (ultimaLeitura != null)
      {
        // Converter float para decimal? explicitamente
        var riscoEnchente = await _mlService.PredictFloodRiskAsync(propriedade, ultimaLeitura);
        dto.RiscoEnchente = (decimal?)riscoEnchente;
      }

      AddHateoasLinks(dto);

      return Ok(dto);
    }
    /// <summary>
    /// Creates a new rural property
    /// </summary>
    /// <param name="createDto">Property creation data</param>
    /// <returns>Created property with HATEOAS links</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PropriedadeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PropriedadeDto>> CreatePropriedade([FromBody] CreatePropriedadeDto createDto)
    {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

      var propriedade = new PropriedadeRural
      {
        IdProdutor = createDto.IdProdutor,
        IdNivelDegradacao = createDto.IdNivelDegradacao,
        NomePropriedade = createDto.NomePropriedade,
        Latitude = createDto.Latitude,
        Longitude = createDto.Longitude,
        AreaHectares = createDto.AreaHectares,
        DataCadastro = DateTime.Now
      };

      _context.PropriedadesRurais.Add(propriedade);
      await _context.SaveChangesAsync();

      // Recarregar com dados relacionados
      propriedade = await _context.PropriedadesRurais
          .Include(p => p.Produtor)
          .Include(p => p.NivelDegradacao)
          .FirstAsync(p => p.Id == propriedade.Id);

      var dto = MapToDto(propriedade);
      AddHateoasLinks(dto);

      _logger.LogInformation("Nova propriedade criada: {PropertyId}", propriedade.Id);

      return CreatedAtAction(
          nameof(GetPropriedade),
          new { id = propriedade.Id },
          dto);
    }

    /// <summary>
    /// Updates an existing rural property
    /// </summary>
    /// <param name="id">Property ID</param>
    /// <param name="updateDto">Updated property data</param>
    /// <returns>Updated property with HATEOAS links</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PropriedadeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropriedadeDto>> UpdatePropriedade(int id, [FromBody] UpdatePropriedadeDto updateDto)
    {
      if (id != updateDto.Id)
        return BadRequest(new { error = "ID da URL não confere com ID do objeto" });

      var propriedade = await _context.PropriedadesRurais.FindAsync(id);
      if (propriedade == null)
        return NotFound(new { error = $"Propriedade com ID {id} não encontrada" });

      propriedade.NomePropriedade = updateDto.NomePropriedade;
      propriedade.Latitude = updateDto.Latitude;
      propriedade.Longitude = updateDto.Longitude;
      propriedade.AreaHectares = updateDto.AreaHectares;
      propriedade.IdNivelDegradacao = updateDto.IdNivelDegradacao;
      propriedade.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();

      // Recarregar com dados relacionados
      propriedade = await _context.PropriedadesRurais
          .Include(p => p.Produtor)
          .Include(p => p.NivelDegradacao)
          .FirstAsync(p => p.Id == id);

      var dto = MapToDto(propriedade);
      AddHateoasLinks(dto);

      return Ok(dto);
    }

    /// <summary>
    /// Deletes a rural property
    /// </summary>
    /// <param name="id">Property ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePropriedade(int id)
    {
      var propriedade = await _context.PropriedadesRurais.FindAsync(id);
      if (propriedade == null)
        return NotFound(new { error = $"Propriedade com ID {id} não encontrada" });

      _context.PropriedadesRurais.Remove(propriedade);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Propriedade deletada: {PropertyId}", id);

      return NoContent();
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

      var leitura = new LeituraSensor
      {
        IdSensor = inputDto.IdSensor,
        TimestampLeitura = DateTime.Now,
        UmidadeSolo = inputDto.UmidadeSolo,
        TemperaturaAr = inputDto.TemperaturaAr,
        PrecipitacaoMm = inputDto.PrecipitacaoMm
      };

      _context.LeiturasSensores.Add(leitura);
      await _context.SaveChangesAsync();

      // Publicar dados no RabbitMQ
      await _rabbitMQService.PublishSensorDataAsync(new
      {
        SensorId = leitura.IdSensor,
        PropertyName = sensor.Propriedade.NomePropriedade,
        ProducerName = sensor.Propriedade.Produtor.NomeCompleto,
        Reading = leitura,
        Timestamp = DateTime.UtcNow
      });

      // Verificar condições de alerta
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

    private PropriedadeDto MapToDto(PropriedadeRural propriedade)
    {
      return new PropriedadeDto
      {
        Id = propriedade.Id,
        NomePropriedade = propriedade.NomePropriedade,
        Latitude = propriedade.Latitude,
        Longitude = propriedade.Longitude,
        AreaHectares = propriedade.AreaHectares,
        NomeProdutor = propriedade.Produtor?.NomeCompleto ?? "",
        EmailProdutor = propriedade.Produtor?.Email ?? "",
        NivelDegradacao = propriedade.NivelDegradacao?.DescricaoDegradacao ?? "",
        NivelNumerico = propriedade.NivelDegradacao?.NivelNumerico ?? 0,
        Sensores = propriedade.Sensores?.Select(s => new SensorDto
        {
          Id = s.Id,
          TipoSensor = s.TipoSensor?.NomeTipo ?? "",
          ModeloDispositivo = s.ModeloDispositivo ?? "",
          DataInstalacao = s.DataInstalacao,
          UltimaLeitura = s.Leituras?.FirstOrDefault() != null ? new LeituraRecenteDto
          {
            TimestampLeitura = s.Leituras.First().TimestampLeitura,
            UmidadeSolo = s.Leituras.First().UmidadeSolo,
            TemperaturaAr = s.Leituras.First().TemperaturaAr,
            PrecipitacaoMm = s.Leituras.First().PrecipitacaoMm
          } : null
        }).ToList() ?? new List<SensorDto>()
      };
    }

    private void AddHateoasLinks(PropriedadeDto dto)
    {
      dto.Links.Add(new LinkDto
      {
        Href = Url.Action(nameof(GetPropriedade), new { id = dto.Id }),
        Rel = "self",
        Method = "GET"
      });

      dto.Links.Add(new LinkDto
      {
        Href = Url.Action(nameof(UpdatePropriedade), new { id = dto.Id }),
        Rel = "update",
        Method = "PUT"
      });

      dto.Links.Add(new LinkDto
      {
        Href = Url.Action(nameof(DeletePropriedade), new { id = dto.Id }),
        Rel = "delete",
        Method = "DELETE"
      });

      dto.Links.Add(new LinkDto
      {
        Href = Url.Action(nameof(GetPropriedades)),
        Rel = "collection",
        Method = "GET"
      });
    }

    private void AddPaginationLinks(PagedResult<PropriedadeDto> result, int currentPage, int totalPages)
    {
      result.Links.Add(new LinkDto
      {
        Href = Url.Action(nameof(GetPropriedades), new { page = 1, pageSize = result.PageSize }),
        Rel = "first",
        Method = "GET"
      });

      if (currentPage > 1)
      {
        result.Links.Add(new LinkDto
        {
          Href = Url.Action(nameof(GetPropriedades), new { page = currentPage - 1, pageSize = result.PageSize }),
          Rel = "previous",
          Method = "GET"
        });
      }

      if (currentPage < totalPages)
      {
        result.Links.Add(new LinkDto
        {
          Href = Url.Action(nameof(GetPropriedades), new { page = currentPage + 1, pageSize = result.PageSize }),
          Rel = "next",
          Method = "GET"
        });
      }

      result.Links.Add(new LinkDto
      {
        Href = Url.Action(nameof(GetPropriedades), new { page = totalPages, pageSize = result.PageSize }),
        Rel = "last",
        Method = "GET"
      });
    }
  }

  public class PagedResult<T> : BaseDto
  {
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
  }
}