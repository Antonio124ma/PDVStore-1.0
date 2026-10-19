using PDVStore.Domain.Entities;
using PDVStore.Domain.Interfaces;

namespace PDVStore.Infrastructure.Storage
{
    public class PagamentoIntegrator : IPagamentoIntegrator
    {
        public Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken ct = default)
        {
            if (venda == null || venda.ValorTotal < 0)
                return Task.FromResult(false);

            // Simula latência de chamada bancária
            // Em produção, substituir por SDK real (PagSeguro, Mercado Pago, etc.)
            if (venda.FormaPagamento == FormaPagamento.PIX)
            {
                venda.PixTxId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            }

            return Task.FromResult(true); // Sempre aprova em mock
        }
    }
}