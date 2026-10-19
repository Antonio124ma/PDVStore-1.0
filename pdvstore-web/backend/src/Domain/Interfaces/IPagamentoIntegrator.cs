using PDVStore.Domain.Entities;

namespace PDVStore.Domain.Interfaces
{
    public interface IPagamentoIntegrator
    {
        Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken ct = default);
    }
}