using Microsoft.EntityFrameworkCore;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Enums;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;

namespace PDVStore.Application.Services
{
    public class CaixaService : ICaixaService
    {
        private readonly PDVContext _context;

        public CaixaService(PDVContext context)
        {
            _context = context;
        }

        public async Task<CaixaDto> AbrirCaixaAsync(decimal valorInicial, int usuarioCaixaId, CancellationToken ct = default)
        {
            var existente = await ObterCaixaAbertoAsync(ct);
            if (existente != null)
                throw new InvalidOperationException("Já existe um caixa aberto. Feche o caixa atual antes de abrir outro.");

            var caixa = new Caixa
            {
                UsuarioCaixaId = usuarioCaixaId,
                Abertura = DateTime.UtcNow,
                ValorInicial = valorInicial,
                Status = StatusCaixa.Aberto
            };

            _context.Caixas.Add(caixa);
            await _context.SaveChangesAsync(ct);

            return new CaixaDto
            {
                Id = caixa.Id,
                UsuarioCaixaId = caixa.UsuarioCaixaId,
                Abertura = caixa.Abertura,
                ValorInicial = caixa.ValorInicial,
                Status = caixa.Status.ToString()
            };
        }

        public async Task<CaixaDto?> ObterCaixaAbertoAsync(CancellationToken ct = default)
        {
            return await _context.Caixas.AsNoTracking()
                .Where(c => c.Status == StatusCaixa.Aberto)
                .Select(c => new CaixaDto
                {
                    Id = c.Id,
                    UsuarioCaixaId = c.UsuarioCaixaId,
                    Abertura = c.Abertura,
                    ValorInicial = c.ValorInicial,
                    Status = c.Status.ToString()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> FecharCaixaAsync(int caixaId, CancellationToken ct = default)
        {
            var caixa = await _context.Caixas.FindAsync(new object[] { caixaId }, ct);
            if (caixa == null || caixa.Status != StatusCaixa.Aberto)
                return false;

            decimal totalVendas = await _context.Vendas
                .Where(v => v.CaixaId == caixaId && v.Status == StatusVenda.Concluida)
                .SumAsync(v => v.ValorTotal, ct);
            decimal sangria = caixa.Sangria ?? 0;

            caixa.ValorFinal = caixa.ValorInicial + totalVendas - sangria;
            caixa.Fechamento = DateTime.UtcNow;
            caixa.Status = StatusCaixa.Fechado;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> RegistrarSangriaAsync(int caixaId, decimal valor, CancellationToken ct = default)
        {
            if (valor <= 0)
                throw new ArgumentException("Valor da sangria deve ser maior que zero.");

            var caixa = await _context.Caixas.FindAsync(new object[] { caixaId }, ct);
            if (caixa == null || caixa.Status != StatusCaixa.Aberto)
                return false;

            caixa.Sangria = (caixa.Sangria ?? 0) + valor;
            caixa.ValorFinal = caixa.ValorFinal ?? caixa.ValorInicial;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<CaixaDto>> ListarAsync(CancellationToken ct = default)
        {
            return await _context.Caixas
                .Include(c => c.UsuarioCaixa)
                .AsNoTracking()
                .OrderByDescending(c => c.Abertura)
                .Select(c => new CaixaDto
                {
                    Id = c.Id,
                    UsuarioCaixaId = c.UsuarioCaixaId,
                    UsuarioNome = c.UsuarioCaixa!.Nome,
                    Abertura = c.Abertura,
                    Fechamento = c.Fechamento,
                    ValorInicial = c.ValorInicial,
                    ValorFinal = c.ValorFinal,
                    Sangria = c.Sangria,
                    Status = c.Status.ToString()
                })
                .ToListAsync(ct);
        }
    }
}