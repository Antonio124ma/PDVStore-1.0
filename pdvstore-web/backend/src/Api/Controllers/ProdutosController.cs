using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Interfaces;

namespace PDVStore.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ProdutosController : ControllerBase
    {
        private readonly IProdutoService _produtoService;

        public ProdutosController(IProdutoService produtoService)
        {
            _produtoService = produtoService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] bool apenasAtivos = true)
        {
            var produtos = await _produtoService.ListarAsync(apenasAtivos);
            return Ok(produtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var produto = await _produtoService.ObterPorIdAsync(id);
            if (produto == null)
                return NotFound();
            return Ok(produto);
        }

        [HttpGet("busca")]
        public async Task<IActionResult> Busca([FromQuery] string q)
        {
            var produtos = await _produtoService.BuscarAsync(q);
            return Ok(produtos);
        }

        [HttpPost]
        [Authorize(Policy = "EstoquistaOuAdministrador")]
        public async Task<IActionResult> Criar([FromBody] ProdutoCreateDto dto)
        {
            var produto = await _produtoService.CriarAsync(dto);
            return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "EstoquistaOuAdministrador")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] ProdutoUpdateDto dto)
        {
            var produto = await _produtoService.AtualizarAsync(id, dto);
            if (produto == null)
                return NotFound();
            return Ok(produto);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ApenasAdministrador")]
        public async Task<IActionResult> Desativar(int id)
        {
            var ok = await _produtoService.DesativarAsync(id);
            if (!ok)
                return NotFound();
            return NoContent();
        }

        [HttpPatch("{id}/estoque")]
        [Authorize(Policy = "EstoquistaOuAdministrador")]
        public async Task<IActionResult> AjustarEstoque(int id, [FromQuery] int quantidade, [FromQuery] string tipo, [FromQuery] string? motivo = null)
        {
            var tipoEnum = Enum.Parse<PDVStore.Domain.Enums.TipoMovimentacaoEstoque>(tipo);
            var ok = await _produtoService.AjustarEstoqueAsync(id, quantidade, tipoEnum, motivo);
            if (!ok)
                return NotFound();
            return NoContent();
        }

        [HttpGet("estoque-minimo")]
        [Authorize(Policy = "EstoquistaOuAdministrador")]
        public async Task<IActionResult> EstoqueMinimo()
        {
            var produtos = await _produtoService.ObterAbaixoEstoqueMinimoAsync();
            return Ok(produtos);
        }
    }
}