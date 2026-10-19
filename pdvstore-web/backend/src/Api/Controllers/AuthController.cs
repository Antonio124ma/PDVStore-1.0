using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;

namespace PDVStore.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ITokenService _tokenService;
        private readonly PDVContext _context;

        public AuthController(IUsuarioService usuarioService, ITokenService tokenService, PDVContext context)
        {
            _usuarioService = usuarioService;
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _usuarioService.AutenticarAsync(request.Usuario, request.Senha);
            if (usuario == null)
                return Unauthorized(new { tipo = "credenciais-invalidas", mensagem = "Credenciais inválidas." });

            var usuarioEntity = await _context.Usuarios.FindAsync(new object[] { usuario.Id });
            if (usuarioEntity == null)
                return Unauthorized(new { tipo = "usuario-nao-encontrado", mensagem = "Usuário não encontrado." });

            var accessToken = _tokenService.GenerateAccessToken(usuarioEntity);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _tokenService.SalvarRefreshToken(usuarioEntity.Id, refreshToken);

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Usuario = usuario
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var valido = await _tokenService.ValidarRefreshTokenAsync(request.RefreshToken);
            if (!valido)
                return Unauthorized(new { tipo = "refresh-token-invalido", mensagem = "Refresh token inválido ou expirado." });

            // Em produção, recuperar o usuário do refresh token salvo
            return BadRequest(new { tipo = "nao-implementado", mensagem = "Refresh token rota requer implementação de armazenamento." });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            await _tokenService.RevogarRefreshTokenAsync(request.RefreshToken);
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var usuario = await _usuarioService.ObterPorIdAsync(usuarioId);
            if (usuario == null)
                return NotFound();
            return Ok(usuario);
        }

        [HttpPost("alterar-senha")]
        [Authorize]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ok = await _usuarioService.AlterarSenhaAsync(usuarioId, request.SenhaAtual, request.NovaSenha);
            if (!ok)
                return BadRequest(new { tipo = "senha-atual-invalida", mensagem = "Senha atual inválida." });
            return NoContent();
        }
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AlterarSenhaRequest
    {
        public string SenhaAtual { get; set; } = string.Empty;
        public string NovaSenha { get; set; } = string.Empty;
    }
}