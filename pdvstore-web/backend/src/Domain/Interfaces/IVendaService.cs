using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;

namespace PDVStore.Domain.Interfaces
{
    public interface IVendaService
    {
        Task<VendaDto> RegistrarVendaAsync(VendaCreateDto dto, CancellationToken ct = default);
        Task<VendaDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<VendaDto>> ListarAsync(
            DateTime? dataInicio,
            DateTime? dataFim,
            string? formaPagamento,
            int? clienteId,
            int page,
            int pageSize,
            CancellationToken ct = default);
        Task<bool> CancelarVendaAsync(int vendaId, string motivo, CancellationToken ct = default);
        Task<byte[]> GerarReciboPdfAsync(int vendaId, CancellationToken ct = default);
    }
}