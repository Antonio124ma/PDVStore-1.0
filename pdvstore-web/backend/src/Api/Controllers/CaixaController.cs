using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Interfaces;

namespace PDVStore.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "OperadorOuAdministrador")]
    public class CaixaController : ControllerBase
    {
        private readonly ICaixaService _caixaService;

        public CaixaController(ICaixaService caixaService)
        {
            _caixaService = caixaService;
        }

        [HttpGet("aberto")]
        public async Task<IActionResult> ObterAberto()
        {
            var caixa = await _caixaService.ObterCaixaAbertoAsync();
            if (caixa == null)
                return NotFound(new { tipo = "caixa-nao-aberto", mensagem = "Nenhum caixa aberto." });
            return Ok(caixa);
        }

        [HttpPost]
        public async Task<IActionResult> Abrir([FromBody] AbrirCaixaDto dto)
        {
            try
            {
                var caixa = await _caixaService.AbrirCaixaAsync(dto.ValorInicial, dto.UsuarioCaixaId);
                return CreatedAtAction(nameof(ObterAberto), new { id = caixa.Id }, caixa);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { tipo = "validacao-negocio", mensagem = ex.Message });
            }
        }

        [HttpPost("{id}/fechar")]
        public async Task<IActionResult> Fechar(int id)
        {
            var ok = await _caixaService.FecharCaixaAsync(id);
            if (!ok)
                return NotFound(new { tipo = "caixa-nao-encontrado", mensagem = "Caixa não encontrado ou já fechado." });
            return NoContent();
        }

        [HttpPost("{id}/sangria")]
        public async Task<IActionResult> Sangria(int id, [FromBody] SangriaDto dto)
        {
            try
            {
                var ok = await _caixaService.RegistrarSangriaAsync(id, dto.Valor);
                if (!ok)
                    return NotFound(new { tipo = "caixa-nao-encontrado", mensagem = "Caixa não encontrado ou não está aberto." });
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { tipo = "validacao-negocio", mensagem = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "ApenasAdministrador")]
        public async Task<IActionResult> Listar()
        {
            var caixas = await _caixaService.ListarAsync();
            return Ok(caixas);
        }
    }
}