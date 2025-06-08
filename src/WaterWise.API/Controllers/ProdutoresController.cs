using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.DTOs;
using WaterWise.Core.Entities;

namespace WaterWise.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class ProdutoresController : ControllerBase
    {
        private readonly WaterWiseContext _context;
        private readonly ILogger<ProdutoresController> _logger;

        public ProdutoresController(
            WaterWiseContext context,
            ILogger<ProdutoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all rural producers with pagination support
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10)</param>
        /// <returns>Paginated list of producers with HATEOAS links</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProdutorRuralDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProdutorRuralDto>>> GetProdutores(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var totalItems = await _context.ProdutoresRurais.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var produtores = await _context.ProdutoresRurais
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var produtoresDto = produtores.Select(produtor => MapToDto(produtor)).ToList();

            foreach (var dto in produtoresDto)
            {
                AddHateoasLinks(dto);
            }

            var result = new PagedResult<ProdutorRuralDto>
            {
                Items = produtoresDto,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            AddPaginationLinks(result, page, totalPages);

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific rural producer by ID
        /// </summary>
        /// <param name="id">Producer ID</param>
        /// <returns>Producer details with HATEOAS links</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProdutorRuralDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProdutorRuralDto>> GetProdutor(int id)
        {
            var produtor = await _context.ProdutoresRurais
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produtor == null)
                return NotFound(new { error = $"Produtor com ID {id} não encontrado" });

            var dto = MapToDto(produtor);
            AddHateoasLinks(dto);

            return Ok(dto);
        }

        /// <summary>
        /// Creates a new rural producer
        /// </summary>
        /// <param name="createDto">Producer creation data</param>
        /// <returns>Created producer with HATEOAS links</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProdutorRuralDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProdutorRuralDto>> CreateProdutor([FromBody] CreateProdutorRuralDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var produtor = new ProdutorRural
            {
                NomeCompleto = createDto.NomeCompleto,
                Email = createDto.Email,
                Telefone = createDto.Telefone,
                CpfCnpj = createDto.CpfCnpj,
                Senha = createDto.Senha
            };

            _context.ProdutoresRurais.Add(produtor);
            await _context.SaveChangesAsync();

            var dto = MapToDto(produtor);
            AddHateoasLinks(dto);

            _logger.LogInformation("Novo produtor criado: {ProducerId}", produtor.Id);

            return CreatedAtAction(
                nameof(GetProdutor),
                new { id = produtor.Id },
                dto);
        }

        /// <summary>
        /// Updates an existing rural producer
        /// </summary>
        /// <param name="id">Producer ID</param>
        /// <param name="updateDto">Updated producer data</param>
        /// <returns>Updated producer with HATEOAS links</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ProdutorRuralDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProdutorRuralDto>> UpdateProdutor(int id, [FromBody] UpdateProdutorRuralDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest(new { error = "ID da URL não confere com ID do objeto" });

            var produtor = await _context.ProdutoresRurais.FindAsync(id);
            if (produtor == null)
                return NotFound(new { error = $"Produtor com ID {id} não encontrado" });

            produtor.NomeCompleto = updateDto.NomeCompleto;
            produtor.Email = updateDto.Email;
            produtor.Telefone = updateDto.Telefone;
            produtor.CpfCnpj = updateDto.CpfCnpj;
            produtor.Senha = updateDto.Senha; // Atualizar hash da senha

            await _context.SaveChangesAsync();

            var dto = MapToDto(produtor);
            AddHateoasLinks(dto);

            return Ok(dto);
        }

        /// <summary>
        /// Deletes a rural producer
        /// </summary>
        /// <param name="id">Producer ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProdutor(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Iniciando exclusão do produtor ID: {ProducerId}", id);

                var produtor = await _context.ProdutoresRurais
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (produtor == null)
                {
                    _logger.LogWarning("Produtor não encontrado com ID: {ProducerId}", id);
                    return NotFound(new { error = $"Produtor com ID {id} não encontrado" });
                }

                var propriedadesCount = await _context.PropriedadesRurais
                    .CountAsync(p => p.IdProdutor == id);

                if (propriedadesCount > 0)
                {
                    _logger.LogWarning("Produtor ID: {ProducerId} possui {PropertyCount} propriedades associadas. Exclusão não permitida.", id, propriedadesCount);
                    return BadRequest(new { error = $"Produtor com ID {id} possui {propriedadesCount} propriedades associadas. Exclua as propriedades primeiro." });
                }

                _logger.LogInformation("Excluindo produtor ID: {ProducerId}", id);
                _context.ProdutoresRurais.Remove(produtor);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("✅ Produtor excluído com sucesso! ID: {ProducerId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "❌ Erro ao excluir produtor ID: {ProducerId}. Transação revertida. Erro: {ErrorMessage}",
                    id, ex.Message);

                return StatusCode(500, new
                {
                    error = "Erro interno do servidor ao excluir produtor",
                    message = ex.Message,
                    details = "A operação foi revertida. Nenhum dado foi alterado."
                });
            }
        }

        private ProdutorRuralDto MapToDto(ProdutorRural produtor)
        {
            return new ProdutorRuralDto
            {
                Id = produtor.Id,
                NomeCompleto = produtor.NomeCompleto,
                Email = produtor.Email,
                Telefone = produtor.Telefone,
                CpfCnpj = produtor.CpfCnpj
            };
        }

        private void AddHateoasLinks(ProdutorRuralDto dto)
        {
            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetProdutor), new { id = dto.Id }),
                Rel = "self",
                Method = "GET"
            });

            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(UpdateProdutor), new { id = dto.Id }),
                Rel = "update",
                Method = "PUT"
            });

            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(DeleteProdutor), new { id = dto.Id }),
                Rel = "delete",
                Method = "DELETE"
            });

            dto.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetProdutores)),
                Rel = "collection",
                Method = "GET"
            });
        }

        private void AddPaginationLinks(PagedResult<ProdutorRuralDto> result, int currentPage, int totalPages)
        {
            result.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetProdutores), new { page = 1, pageSize = result.PageSize }),
                Rel = "first",
                Method = "GET"
            });

            if (currentPage > 1)
            {
                result.Links.Add(new LinkDto
                {
                    Href = Url.Action(nameof(GetProdutores), new { page = currentPage - 1, pageSize = result.PageSize }),
                    Rel = "previous",
                    Method = "GET"
                });
            }

            if (currentPage < totalPages)
            {
                result.Links.Add(new LinkDto
                {
                    Href = Url.Action(nameof(GetProdutores), new { page = currentPage + 1, pageSize = result.PageSize }),
                    Rel = "next",
                    Method = "GET"
                });
            }

            result.Links.Add(new LinkDto
            {
                Href = Url.Action(nameof(GetProdutores), new { page = totalPages, pageSize = result.PageSize }),
                Rel = "last",
                Method = "GET"
            });
        }
    }
}