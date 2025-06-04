using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.DTOs;
using WaterWise.Core.Entities;
using Oracle.ManagedDataAccess.Client;

namespace WaterWise.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class NiveisDegradacaoController : ControllerBase
    {
        private readonly WaterWiseContext _context;
        private readonly ILogger<NiveisDegradacaoController> _logger;

        public NiveisDegradacaoController(
            WaterWiseContext context,
            ILogger<NiveisDegradacaoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all soil degradation levels with pagination support
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <returns>Paginated list of degradation levels with HATEOAS links</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<NivelDegradacaoSoloDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<NivelDegradacaoSoloDto>>> GetNiveisDegradacao(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var totalItems = await _context.NiveisDegradacaoSolo.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var niveis = await _context.NiveisDegradacaoSolo
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var niveisDto = niveis.Select(nivel => MapToDto(nivel)).ToList();

            foreach (var dto in niveisDto)
            {
                AddHateoasLinks(dto);
            }

            var result = new PagedResult<NivelDegradacaoSoloDto>
            {
                Items = niveisDto,
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
        /// Retrieves a specific soil degradation level by ID
        /// </summary>
        /// <param name="id">Degradation level ID</param>
        /// <returns>Degradation level details with HATEOAS links</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(NivelDegradacaoSoloDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NivelDegradacaoSoloDto>> GetNivelDegradacao(int id)
        {
            var nivel = await _context.NiveisDegradacaoSolo
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nivel == null)
                return NotFound(new { error = $"Nível de degradação com ID {id} não encontrado" });

            var dto = MapToDto(nivel);
            AddHateoasLinks(dto);

            return Ok(dto);
        }


        /// <summary>
        /// Creates a new soil degradation level
        /// </summary>
        /// <param name="createDto">Degradation level creation data</param>
        /// <returns>Created degradation level with HATEOAS links</returns>
        [HttpPost]
        [ProducesResponseType(typeof(NivelDegradacaoSoloDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<NivelDegradacaoSoloDto>> CreateNivelDegradacao([FromBody] CreateNivelDegradacaoSoloDto createDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { error = "Dados de entrada inválidos", details = errors });
            }

            var nivel = new NivelDegradacaoSolo
            {
                CodigoDegradacao = createDto.CodigoDegradacao,
                DescricaoDegradacao = createDto.DescricaoDegradacao,
                NivelNumerico = createDto.NivelNumerico,
                AcoesCorretivas = createDto.AcoesCorretivas
            };

            try
            {
                _context.NiveisDegradacaoSolo.Add(nivel);
                await _context.SaveChangesAsync();

                var dto = MapToDto(nivel);
                AddHateoasLinks(dto);

                _logger.LogInformation("Novo nível de degradação criado: {DegradationId}", nivel.Id);

                return CreatedAtAction(
                    nameof(GetNivelDegradacao),
                    new { id = nivel.Id },
                    dto);
            }
            catch (DbUpdateException ex) when (ex.InnerException is OracleException oracleEx && oracleEx.Number == 2290)
            {
                _logger.LogWarning("Tentativa de criar nível de degradação com NivelNumerico inválido: {NivelNumerico}. Erro: {ErrorMessage}", createDto.NivelNumerico, ex.Message);
                return BadRequest(new
                {
                    error = "Valor inválido para NivelNumerico",
                    details = "O valor de NivelNumerico deve estar entre 1 e 5. Valor fornecido: " + createDto.NivelNumerico
                });
            }
        }

        /// <summary>
        /// Updates an existing soil degradation level
        /// </summary>
        /// <param name="id">Degradation level ID</param>
        /// <param name="updateDto">Updated degradation level data</param>
        /// <returns>Updated degradation level with HATEOAS links</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(NivelDegradacaoSoloDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NivelDegradacaoSoloDto>> UpdateNivelDegradacao(int id, [FromBody] UpdateNivelDegradacaoSoloDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest(new { error = "ID da URL não confere com ID do objeto" });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { error = "Dados de entrada inválidos", details = errors });
            }

            var nivel = await _context.NiveisDegradacaoSolo.FindAsync(id);
            if (nivel == null)
                return NotFound(new { error = $"Nível de degradação com ID {id} não encontrado" });

            nivel.CodigoDegradacao = updateDto.CodigoDegradacao;
            nivel.DescricaoDegradacao = updateDto.DescricaoDegradacao;
            nivel.NivelNumerico = updateDto.NivelNumerico;
            nivel.AcoesCorretivas = updateDto.AcoesCorretivas;

            try
            {
                await _context.SaveChangesAsync();

                var dto = MapToDto(nivel);
                AddHateoasLinks(dto);

                return Ok(dto);
            }
            catch (DbUpdateException ex) when (ex.InnerException is OracleException oracleEx && oracleEx.Number == 2290)
            {
                _logger.LogWarning("Tentativa de atualizar nível de degradação com NivelNumerico inválido: {NivelNumerico}. Erro: {ErrorMessage}", updateDto.NivelNumerico, ex.Message);
                return BadRequest(new
                {
                    error = "Valor inválido para NivelNumerico",
                    details = "O valor de NivelNumerico deve estar entre 1 e 5. Valor fornecido: " + updateDto.NivelNumerico
                });
            }
        }

        /// <summary>
        /// Deletes a soil degradation level
        /// </summary>
        /// <param name="id">Degradation level ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNivelDegradacao(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Iniciando exclusão do nível de degradação ID: {DegradationId}", id);

                var nivel = await _context.NiveisDegradacaoSolo
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (nivel == null)
                {
                    _logger.LogWarning("Nível de degradação não encontrado com ID: {DegradationId}", id);
                    return NotFound(new { error = $"Nível de degradação com ID {id} não encontrado" });
                }

                // Verificar se o nível de degradação está associado a propriedades
                var propriedadesCount = await _context.PropriedadesRurais
                    .CountAsync(p => p.IdNivelDegradacao == id);

                if (propriedadesCount > 0)
                {
                    _logger.LogWarning("Nível de degradação ID: {DegradationId} possui {PropertyCount} propriedades associadas. Exclusão não permitida.", id, propriedadesCount);
                    return BadRequest(new { error = $"Nível de degradação com ID {id} possui {propriedadesCount} propriedades associadas. Exclua as propriedades primeiro." });
                }

                _logger.LogInformation("Excluindo nível de degradação ID: {DegradationId}", id);
                _context.NiveisDegradacaoSolo.Remove(nivel);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("✅ Nível de degradação excluído com sucesso! ID: {DegradationId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "❌ Erro ao excluir nível de degradação ID: {DegradationId}. Transação revertida. Erro: {ErrorMessage}",
                    id, ex.Message);

                return StatusCode(500, new
                {
                    error = "Erro interno do servidor ao excluir nível de degradação",
                    message = ex.Message,
                    details = "A operação foi revertida. Nenhum dado foi alterado."
                });
            }
        }

        private NivelDegradacaoSoloDto MapToDto(NivelDegradacaoSolo nivel)
        {
            return new NivelDegradacaoSoloDto
            {
                Id = nivel.Id,
                CodigoDegradacao = nivel.CodigoDegradacao,
                DescricaoDegradacao = nivel.DescricaoDegradacao,
                NivelNumerico = nivel.NivelNumerico,
                AcoesCorretivas = nivel.AcoesCorretivas
            };
        }

        private void AddHateoasLinks(NivelDegradacaoSoloDto dto)
        {
            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetNivelDegradacao), new { id = dto.Id }),
                Rel = "self",
                Method = "GET"
            });

            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(UpdateNivelDegradacao), new { id = dto.Id }),
                Rel = "update",
                Method = "PUT"
            });

            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(DeleteNivelDegradacao), new { id = dto.Id }),
                Rel = "delete",
                Method = "DELETE"
            });

            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetNiveisDegradacao)),
                Rel = "collection",
                Method = "GET"
            });
        }

        private void AddPaginationLinks(PagedResult<NivelDegradacaoSoloDto> result, int currentPage, int totalPages)
        {
            result.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetNiveisDegradacao), new { page = 1, pageSize = result.PageSize }),
                Rel = "first",
                Method = "GET"
            });

            if (currentPage > 1)
            {
                result.Links.Add(new LinkDto
                {
                    Href = Url.Action(nameof(GetNiveisDegradacao), new { page = currentPage - 1, pageSize = result.PageSize }),
                    Rel = "previous",
                    Method = "GET"
                });
            }

            if (currentPage < totalPages)
            {
                result.Links.Add(new LinkDto
                {
                    Href = Url.Action(nameof(GetNiveisDegradacao), new { page = currentPage + 1, pageSize = result.PageSize }),
                    Rel = "next",
                    Method = "GET"
                });
            }

            result.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetNiveisDegradacao), new { page = totalPages, pageSize = result.PageSize }),
                Rel = "last",
                Method = "GET"
            });
        }
    }
}