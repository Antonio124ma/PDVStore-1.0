using Microsoft.EntityFrameworkCore;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Enums;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;

namespace PDVStore.Application.Services
{
    public class VendaService : IVendaService
    {
        private readonly PDVContext _context;
        private readonly IPagamentoIntegrator _pagamentoIntegrator;
        private readonly ICaixaService _caixaService;

        public VendaService(
            PDVContext context,
            IPagamentoIntegrator pagamentoIntegrator,
            ICaixaService caixaService)
        {
            _context = context;
            _pagamentoIntegrator = pagamentoIntegrator;
            _caixaService = caixaService;
        }

        public async Task<VendaDto> RegistrarVendaAsync(
            VendaCreateDto dto,
            CancellationToken ct = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                if (dto.Itens == null || !dto.Itens.Any())
                    throw new InvalidOperationException("A venda deve conter pelo menos um item.");

                var caixa = await _caixaService.ObterCaixaAbertoAsync(ct);
                if (caixa == null)
                    throw new InvalidOperationException("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");

                var formaPagamento = Enum.Parse<FormaPagamento>(dto.FormaPagamento);
                if (formaPagamento == FormaPagamento.Fiado)
                {
                    if (!dto.ClienteId.HasValue)
                        throw new InvalidOperationException("Venda fiada exige um cliente selecionado.");

                    var cliente = await _context.Clientes.FindAsync(new object[] { dto.ClienteId.Value }, ct);
                    if (cliente == null || !cliente.Ativo)
                        throw new InvalidOperationException("Cliente informado não está ativo.");

                    decimal totalItens = dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
                    if (!cliente.TemCreditoDisponivel(totalItens - dto.Desconto))
                        throw new InvalidOperationException("Crédito insuficiente para venda fiada.");
                }
            }
                decimal valorTotal = dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario) - dto.Desconto;
                if (valorTotal < 0) valorTotal = 0;

                var venda = new Venda
                {
                    UsuarioCaixaId = dto.UsuarioCaixaId,
                    CaixaId = caixa.Id,
                    DataVenda = DateTime.UtcNow,
                    ValorTotal = valorTotal,
                    Desconto = dto.Desconto,
                    FormaPagamento = formaPagamento,
                    ClienteId = dto.ClienteId,
                    Status = StatusVenda.Concluida,
                    Itens = dto.Itens.Select(i => new ItemVenda
                    {
                        ProdutoId = i.ProdutoId,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList()
                };

                _context.Vendas.Add(venda);

                foreach (var item in venda.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(new object[] { item.ProdutoId }, ct);
                    if (produto == null || produto.Estoque < item.Quantidade)
                        throw new InvalidOperationException($"Estoque insuficiente para o produto ID {item.ProdutoId}.");

                    produto.Estoque -= item.Quantidade;
                    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = TipoMovimentacaoEstoque.Saida,
                        Quantidade = item.Quantidade,
                        DataMovimentacao = DateTime.UtcNow,
                        UsuarioId = dto.UsuarioCaixaId,
                        Motivo = $"Venda #{venda.Id}"
                    });
                }

                if (venda.ClienteId.HasValue && formaPagamento == FormaPagamento.Fiado)
                {
                    var cliente = await _context.Clientes.FindAsync(new object[] { venda.ClienteId.Value }, ct);
                    if (cliente != null)
                        cliente.SaldoDevedor += valorTotal;
                }

                bool pagamentoOk = await _pagamentoIntegrator.ProcessarPagamentoAsync(new Domain.Entities.Venda
                {
                    ValorTotal = valorTotal,
                    FormaPagamento = formaPagamento
                }, ct);
                if (!pagamentoOk)
                    throw new InvalidOperationException("Falha ao processar pagamento.");

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return await ObterPorIdAsync(venda.Id, ct)
                    ?? throw new InvalidOperationException("Venda não encontrada após registro.");
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        public async Task<VendaDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Vendas
                .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
                .Include(v => v.Cliente)
                .Include(v => v.UsuarioCaixa)
                .AsNoTracking()
                .Select(v => new VendaDto
                {
                    Id = v.Id,
                    UsuarioCaixaId = v.UsuarioCaixaId,
                    UsuarioNome = v.UsuarioCaixa!.Nome,
                    DataVenda = v.DataVenda,
                    ValorTotal = v.ValorTotal,
                    Desconto = v.Desconto,
                    FormaPagamento = v.FormaPagamento.ToString(),
                    PixTxId = v.PixTxId,
                    Status = v.Status.ToString(),
                    CaixaId = v.CaixaId,
                    ClienteId = v.ClienteId,
                    ClienteNome = v.Cliente!.Nome,
                    Itens = v.Itens.Select(i => new ItemVendaDto
                    {
                        Id = i.Id,
                        VendaId = i.VendaId,
                        ProdutoId = i.ProdutoId,
                        NomeProduto = i.Produto!.Nome,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList()
                })
                .FirstOrDefaultAsync(v => v.Id == id, ct);
        public async Task<IEnumerable<VendaDto>> ListarAsync(
            DateTime? dataInicio,
            DateTime? dataFim,
            string? formaPagamento,
            int? clienteId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = _context.Vendas
                .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
                .Include(v => v.Cliente)
                .Include(v => v.UsuarioCaixa)
                .AsNoTracking()
                .AsQueryable();

            if (dataInicio.HasValue)
                query = query.Where(v => v.DataVenda >= dataInicio.Value);
            if (dataFim.HasValue)
                query = query.Where(v => v.DataVenda <= dataFim.Value);
            if (!string.IsNullOrWhiteSpace(formaPagamento))
                query = query.Where(v => v.FormaPagamento.ToString() == formaPagamento);
            if (clienteId.HasValue)
                query = query.Where(v => v.ClienteId == clienteId.Value);

            return await query
                .OrderByDescending(v => v.DataVenda)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VendaDto
                {
                    Id = v.Id,
                    UsuarioCaixaId = v.UsuarioCaixaId,
                    UsuarioNome = v.UsuarioCaixa!.Nome,
                    DataVenda = v.DataVenda,
                    ValorTotal = v.ValorTotal,
                    Desconto = v.Desconto,
                    FormaPagamento = v.FormaPagamento.ToString(),
                    Status = v.Status.ToString(),
                    CaixaId = v.CaixaId,
                    ClienteId = v.ClienteId,
                    ClienteNome = v.Cliente!.Nome,
                    Itens = v.Itens.Select(i => new ItemVendaDto
                    {
                        Id = i.Id,
                        VendaId = i.VendaId,
                        ProdutoId = i.ProdutoId,
                        NomeProduto = i.Produto!.Nome,
                        Quantidade = i.Quantidade,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList()
                })
                .ToListAsync(ct);
        public async Task<bool> CancelarVendaAsync(int vendaId, string motivo, CancellationToken ct = default)
        {
            var venda = await _context.Vendas
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, ct);

            if (venda == null || venda.Status == StatusVenda.Cancelada)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                foreach (var item in venda.Itens)
                {
                    var produto = await _context.Produtos.FindAsync(new object[] { item.ProdutoId }, ct);
                    if (produto != null)
                        produto.Estoque += item.Quantidade;

                    _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = TipoMovimentacaoEstoque.Entrada,
                        Quantidade = item.Quantidade,
                        DataMovimentacao = DateTime.UtcNow,
                        UsuarioId = null,
                        Motivo = $"Estorno - Venda #{venda.Id}",
                        ReferenciaVendaId = venda.Id
                    });
                }

                if (venda.ClienteId.HasValue && venda.FormaPagamento == FormaPagamento.Fiado)
                {
                    var cliente = await _context.Clientes.FindAsync(new object[] { venda.ClienteId.Value }, ct);
                    if (cliente != null)
                        cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - venda.ValorTotal);
                }

                venda.Status = StatusVenda.Cancelada;
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                return false;
            }
        }

        public async Task<byte[]> GerarReciboPdfAsync(int vendaId, CancellationToken ct = default)
        {
            var venda = await _context.Vendas
                .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == vendaId, ct);

            if (venda == null)
                throw new InvalidOperationException("Venda não encontrada.");

            var linhas = new List<string>
            {
                "=== RECIBO DE VENDA ===",
                $"Venda #: {venda.Id}",
                $"Data: {venda.DataVenda:dd/MM/yyyy HH:mm}",
                $"Operador: {venda.UsuarioCaixa?.Nome ?? "-"}",
                $"Forma Pagamento: {venda.FormaPagamento}",
                $"Cliente: {venda.Cliente?.Nome ?? "Consumidor Final"}",
                "",
                "ITENS:",
            };

            foreach (var item in venda.Itens)
            {
                linhas.Add($"{item.Quantidade}x {item.NomeProduto} - R$ {item.PrecoUnitario:F2} = R$ {item.Subtotal:F2}");
            }

            linhas.Add("");
            linhas.Add($"Desconto: R$ {venda.Desconto:F2}");
            linhas.Add($"TOTAL: R$ {venda.ValorTotal:F2}");
            linhas.Add("");
            if (!string.IsNullOrEmpty(venda.PixTxId))
                linhas.Add($"PIX TxId: {venda.PixTxId}");
            linhas.Add("Obrigado pela preferência!");

            var texto = string.Join("\n", linhas);
            return System.Text.Encoding.UTF8.GetBytes(texto);
        }
    }
}
        }
        }