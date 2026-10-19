using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;

namespace PDVStore.Domain.Interfaces
{
    public interface ICaixaService
    {
        Task<CaixaDto> AbrirCaixaAsync(decimal valorInicial, int usuarioCaixaId, CancellationToken ct = default);
        Task<CaixaDto?> ObterCaixaAbertoAsync(CancellationToken ct = default);
        Task<bool> FecharCaixaAsync(int caixaId, CancellationToken ct = default);
        Task<bool> RegistrarSangriaAsync(int caixaId, decimal valor, CancellationToken ct = default);
        Task<IEnumerable<CaixaDto>> ListarAsync(CancellationToken ct = default);
    }
}