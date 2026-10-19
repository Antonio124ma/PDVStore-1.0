using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Interfaces;

namespace PDVStore.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "ApenasAdministrador")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] bool apenasAtivos = true)
        {
            var usuarios = await _usuarioService.ListarAsync(apenasAtivos);
            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var usuario = await _usuarioService.ObterPorIdAsync(id);
            if (usuario == null)
                return NotFound();
            return Ok(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] UsuarioCreateDto dto)
        {
            try
            {
                var usuario = await _usuarioService.CriarAsync(dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = usuario.Id }, usuario);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { tipo = "validacao-negocio", mensagem = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] UsuarioUpdateDto dto)
        {
            var ok = await _usuarioService.AtualizarAsync(id, dto);
            if (!ok)
                return NotFound();
            return NoContent();
        }

        [HttpPatch("{id}/ativo")]
        public async Task<IActionResult> AtivarDesativar(int id, [FromQuery] bool ativo)
        {
            var ok = await _usuarioService.AtivarDesativarAsync(id, ativo);
            if (!ok)
                return NotFound();
            return NoContent();
        }

        [HttpGet("administradores")]
        public async Task<IActionResult> ListarAdministradores()
        {
            var admins = await _usuarioService.ListarAdministradoresAsync();
            return Ok(admins);
        }
    }
}