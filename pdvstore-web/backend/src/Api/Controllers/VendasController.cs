using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Interfaces;

namespace PDVStore.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "OperadorOuAdministrador")]
    public class VendasController : ControllerBase
    {
        private readonly IVendaService _vendaService;

        public VendasController(IVendaService vendaService)
        {
            _vendaService = vendaService;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] VendaCreateDto dto)
        {
            try
            {
                var venda = await _vendaService.RegistrarVendaAsync(dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = venda.Id }, venda);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { tipo = "validacao-negocio", mensagem = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { tipo = "erro-interno", mensagem = "Erro inesperado ao registrar venda." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var venda = await _vendaService.ObterPorIdAsync(id);
            if (venda == null)
                return NotFound();
            return Ok(venda);
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] string? formaPagamento,
            [FromQuery] int? clienteId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var vendas = await _vendaService.ListarAsync(dataInicio, dataFim, formaPagamento, clienteId, page, pageSize);
            return Ok(vendas);
        }

        [HttpPost("{id}/cancelar")]
        [Authorize(Policy = "ApenasAdministrador")]
        public async Task<IActionResult> Cancelar(int id, [FromQuery] string motivo)
        {
            var ok = await _vendaService.CancelarVendaAsync(id, motivo);
            if (!ok)
                return NotFound(new { tipo = "venda-nao-encontrada", mensagem = "Venda não encontrada ou já cancelada." });
            return NoContent();
        }

        [HttpGet("{id}/recibo")]
        public async Task<IActionResult> Recibo(int id)
        {
            var pdfBytes = await _vendaService.GerarReciboPdfAsync(id);
            return File(pdfBytes, "application/pdf", $"recibo-venda-{id}.pdf");
        }
    }
}