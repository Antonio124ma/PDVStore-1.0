using Microsoft.EntityFrameworkCore;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Enums;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;

namespace PDVStore.Application.Services
{
    public class ProdutoService : IProdutoService
    {
        private readonly PDVContext _context;

        public ProdutoService(PDVContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProdutoDto>> ListarAsync(bool apenasAtivos = true, CancellationToken ct = default)
        {
            var query = _context.Produtos.AsNoTracking().AsQueryable();
            if (apenasAtivos)
                query = query.Where(p => p.Ativo);
            return await query.OrderBy(p => p.Nome)
                .Select(p => new ProdutoDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    CodigoBarras = p.CodigoBarras,
                    Preco = p.Preco,
                    PrecoCusto = p.PrecoCusto,
                    Estoque = p.Estoque,
                    EstoqueMinimo = p.EstoqueMinimo,
                    Categoria = p.Categoria,
                    Descricao = p.Descricao,
                    Ativo = p.Ativo
                })
                .ToListAsync(ct);
        }

        public async Task<ProdutoDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Produtos.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProdutoDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    CodigoBarras = p.CodigoBarras,
                    Preco = p.Preco,
                    PrecoCusto = p.PrecoCusto,
                    Estoque = p.Estoque,
                    EstoqueMinimo = p.EstoqueMinimo,
                    Categoria = p.Categoria,
                    Descricao = p.Descricao,
                    Ativo = p.Ativo
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<ProdutoDto>> BuscarAsync(string filtro, CancellationToken ct = default)
        {
            var termo = filtro.Trim();
            if (string.IsNullOrEmpty(termo))
                return await ListarAsync(true, ct);

            return await _context.Produtos.AsNoTracking()
                .Where(p => p.Ativo && (p.Nome.Contains(termo) || (p.CodigoBarras != null && p.CodigoBarras.Contains(termo))))
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    CodigoBarras = p.CodigoBarras,
                    Preco = p.Preco,
                    PrecoCusto = p.PrecoCusto,
                    Estoque = p.Estoque,
                    EstoqueMinimo = p.EstoqueMinimo,
                    Categoria = p.Categoria,
                    Descricao = p.Descricao,
                    Ativo = p.Ativo
                })
                .ToListAsync(ct);
        public async Task<ProdutoDto> CriarAsync(ProdutoCreateDto dto, CancellationToken ct = default)
        {
            var produto = new Produto
            {
                Nome = dto.Nome,
                CodigoBarras = dto.CodigoBarras,
                Preco = dto.Preco,
                PrecoCusto = dto.PrecoCusto,
                Estoque = dto.Estoque,
                EstoqueMinimo = dto.EstoqueMinimo,
                Categoria = dto.Categoria,
                Descricao = dto.Descricao,
                Ativo = true
            };
            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync(ct);
            return new ProdutoDto
            {
                Id = produto.Id,
                Nome = produto.Nome,
                CodigoBarras = produto.CodigoBarras,
                Preco = produto.Preco,
                PrecoCusto = produto.PrecoCusto,
                Estoque = produto.Estoque,
                EstoqueMinimo = produto.EstoqueMinimo,
                Categoria = produto.Categoria,
                Descricao = produto.Descricao,
                Ativo = produto.Ativo
            };
        }

        public async Task<ProdutoDto?> AtualizarAsync(int id, ProdutoUpdateDto dto, CancellationToken ct = default)
        {
            var produto = await _context.Produtos.FindAsync(new object[] { id }, ct);
            if (produto == null)
                return null;

            produto.Nome = dto.Nome;
            produto.CodigoBarras = dto.CodigoBarras;
            produto.Preco = dto.Preco;
            produto.PrecoCusto = dto.PrecoCusto;
            produto.EstoqueMinimo = dto.EstoqueMinimo;
            produto.Categoria = dto.Categoria;
            produto.Descricao = dto.Descricao;
            produto.Ativo = dto.Ativo;

            await _context.SaveChangesAsync(ct);

            return new ProdutoDto
            {
                Id = produto.Id,
                Nome = produto.Nome,
                CodigoBarras = produto.CodigoBarras,
                Preco = produto.Preco,
                PrecoCusto = produto.PrecoCusto,
                Estoque = produto.Estoque,
                EstoqueMinimo = produto.EstoqueMinimo,
                Categoria = produto.Categoria,
                Descricao = produto.Descricao,
                Ativo = produto.Ativo
            };
        }

        public async Task<bool> DesativarAsync(int id, CancellationToken ct = default)
        {
            var produto = await _context.Produtos.FindAsync(new object[] { id }, ct);
            if (produto == null)
                return false;
            produto.Ativo = false;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> AjustarEstoqueAsync(int produtoId, int quantidade, TipoMovimentacaoEstoque tipo, string? motivo, CancellationToken ct = default)
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.");

            var produto = await _context.Produtos.FindAsync(new object[] { produtoId }, ct);
            if (produto == null)
                return false;

            if (tipo == TipoMovimentacaoEstoque.Saida && produto.Estoque < quantidade)
                throw new InvalidOperationException("Estoque insuficiente para saída.");

            if (tipo == TipoMovimentacaoEstoque.Entrada)
                produto.Estoque += quantidade;
            else
                produto.Estoque -= quantidade;

            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ProdutoId = produtoId,
                Tipo = tipo,
                Quantidade = quantidade,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = null,
                Motivo = motivo
            });

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<ProdutoDto>> ObterAbaixoEstoqueMinimoAsync(CancellationToken ct = default)
        {
            return await _context.Produtos.AsNoTracking()
                .Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo)
                .OrderBy(p => p.Nome)
                .Select(p => new ProdutoDto
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    CodigoBarras = p.CodigoBarras,
                    Preco = p.Preco,
                    PrecoCusto = p.PrecoCusto,
                    Estoque = p.Estoque,
                    EstoqueMinimo = p.EstoqueMinimo,
                    Categoria = p.Categoria,
                    Descricao = p.Descricao,
                    Ativo = p.Ativo
                })
                .ToListAsync(ct);
        }
    }
}
        }
    }
}