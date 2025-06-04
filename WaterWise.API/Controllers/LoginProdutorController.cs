using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.DTOs;

namespace WaterWise.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class LoginProdutorController : ControllerBase
    {
        private readonly WaterWiseContext _context;
        private readonly ILogger<LoginProdutorController> _logger;

        public LoginProdutorController(
            WaterWiseContext context,
            ILogger<LoginProdutorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a rural producer using email and password
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <returns>Producer details if authentication is successful</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProdutorRuralDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ProdutorRuralDto>> Login([FromBody] LoginProdutorDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Tentativa de login para o email: {Email}", loginDto.Email);

            // Buscar o produtor pelo email (case-insensitive)
            var produtor = await _context.ProdutoresRurais
                .FirstOrDefaultAsync(p => p.Email.ToLower() == loginDto.Email.ToLower());

            if (produtor == null)
            {
                _logger.LogWarning("Produtor não encontrado para o email: {Email}", loginDto.Email);
                return Unauthorized(new { error = "Email ou senha inválidos" });
            }

            // Verificar a senha
            // bool senhaValida = BCrypt.Net.BCrypt.Verify(loginDto.Senha, produtor.Senha);
            bool senhaValida = loginDto.Senha == produtor.Senha;

            if (!senhaValida)
            {
                _logger.LogWarning("Senha inválida para o email: {Email}", loginDto.Email);
                return Unauthorized(new { error = "Email ou senha inválidos" });
            }

            _logger.LogInformation("Login bem-sucedido para o produtor ID: {ProducerId}", produtor.Id);

            var dto = new ProdutorRuralDto
            {
                Id = produtor.Id,
                NomeCompleto = produtor.NomeCompleto,
                Email = produtor.Email,
                Telefone = produtor.Telefone
            };

            return Ok(dto);
        }
    }
}