using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;

namespace PDVStore.Domain.Interfaces
{
    public interface IProdutoService
    {
        Task<IEnumerable<ProdutoDto>> ListarAsync(bool apenasAtivos = true, CancellationToken ct = default);
        Task<ProdutoDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<ProdutoDto>> BuscarAsync(string filtro, CancellationToken ct = default);
        Task<ProdutoDto> CriarAsync(ProdutoCreateDto dto, CancellationToken ct = default);
        Task<ProdutoDto?> AtualizarAsync(int id, ProdutoUpdateDto dto, CancellationToken ct = default);
        Task<bool> DesativarAsync(int id, CancellationToken ct = default);
        Task<bool> AjustarEstoqueAsync(int produtoId, int quantidade, TipoMovimentacaoEstoque tipo, string? motivo, CancellationToken ct = default);
        Task<IEnumerable<ProdutoDto>> ObterAbaixoEstoqueMinimoAsync(CancellationToken ct = default);
    }
}