using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Interfaces;

namespace PDVStore.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var clientes = await _clienteService.ListarAsync();
            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var cliente = await _clienteService.ObterPorIdAsync(id);
            if (cliente == null)
                return NotFound();
            return Ok(cliente);
        }

        [HttpPost]
        [Authorize(Policy = "ApenasAdministrador")]
        public async Task<IActionResult> Criar([FromBody] ClienteCreateDto dto)
        {
            var cliente = await _clienteService.CriarAsync(dto);
            return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, cliente);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ApenasAdministrador")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] ClienteUpdateDto dto)
        {
            var cliente = await _clienteService.AtualizarAsync(id, dto);
            if (cliente == null)
                return NotFound();
            return Ok(cliente);
        }

        [HttpPatch("{id}/ativo")]
        [Authorize(Policy = "ApenasAdministrador")]
        public async Task<IActionResult> AtivarDesativar(int id, [FromQuery] bool ativo)
        {
            var ok = await _clienteService.AtivarDesativarAsync(id, ativo);
            if (!ok)
                return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/receber-debito")]
        [Authorize(Policy = "OperadorOuAdministrador")]
        public async Task<IActionResult> ReceberDebito(int id, [FromBody] ReceberDebitoDto dto)
        {
            try
            {
                var ok = await _clienteService.ReceberDebitoAsync(id, dto.Valor);
                if (!ok)
                    return NotFound(new { tipo = "cliente-nao-encontrado", mensagem = "Cliente não encontrado." });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { tipo = "validacao-negocio", mensagem = ex.Message });
            }
        }
    }
}