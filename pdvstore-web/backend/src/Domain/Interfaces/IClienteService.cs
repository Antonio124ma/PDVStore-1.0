using PDVStore.Application.DTOs;

namespace PDVStore.Domain.Interfaces
{
    public interface IClienteService
    {
        Task<IEnumerable<ClienteDto>> ListarAsync(CancellationToken ct = default);
        Task<ClienteDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<ClienteDto> CriarAsync(ClienteCreateDto dto, CancellationToken ct = default);
        Task<ClienteDto?> AtualizarAsync(int id, ClienteUpdateDto dto, CancellationToken ct = default);
        Task<bool> AtivarDesativarAsync(int id, bool ativo, CancellationToken ct = default);
        Task<bool> ReceberDebitoAsync(int id, decimal valor, CancellationToken ct = default);
    }
}