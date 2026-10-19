using Microsoft.EntityFrameworkCore;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;

namespace PDVStore.Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly PDVContext _context;

        public ClienteService(PDVContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClienteDto>> ListarAsync(CancellationToken ct = default)
        {
            return await _context.Clientes.AsNoTracking()
                .OrderBy(c => c.Nome)
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    CpfCnpj = c.CpfCnpj,
                    Telefone = c.Telefone,
                    Email = c.Email,
                    Endereco = c.Endereco,
                    CadastradoEm = c.CadastradoEm,
                    Ativo = c.Ativo,
                    LimiteCredito = c.LimiteCredito,
                    SaldoDevedor = c.SaldoDevedor
                })
                .ToListAsync(ct);
        }

        public async Task<ClienteDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Clientes.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    CpfCnpj = c.CpfCnpj,
                    Telefone = c.Telefone,
                    Email = c.Email,
                    Endereco = c.Endereco,
                    CadastradoEm = c.CadastradoEm,
                    Ativo = c.Ativo,
                    LimiteCredito = c.LimiteCredito,
                    SaldoDevedor = c.SaldoDevedor
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<ClienteDto> CriarAsync(ClienteCreateDto dto, CancellationToken ct = default)
        {
            var cliente = new Cliente
            {
                Nome = dto.Nome,
                CpfCnpj = dto.CpfCnpj,
                Telefone = dto.Telefone,
                Email = dto.Email,
                Endereco = dto.Endereco,
                LimiteCredito = dto.LimiteCredito,
                SaldoDevedor = 0,
                Ativo = true
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync(ct);

            return new ClienteDto
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                CpfCnpj = cliente.CpfCnpj,
                Telefone = cliente.Telefone,
                Email = cliente.Email,
                Endereco = cliente.Endereco,
                CadastradoEm = cliente.CadastradoEm,
                Ativo = cliente.Ativo,
                LimiteCredito = cliente.LimiteCredito,
                SaldoDevedor = cliente.SaldoDevedor
            };
        }

        public async Task<ClienteDto?> AtualizarAsync(int id, ClienteUpdateDto dto, CancellationToken ct = default)
        {
            var cliente = await _context.Clientes.FindAsync(new object[] { id }, ct);
            if (cliente == null)
                return null;

            cliente.Nome = dto.Nome;
            cliente.CpfCnpj = dto.CpfCnpj;
            cliente.Telefone = dto.Telefone;
            cliente.Email = dto.Email;
            cliente.Endereco = dto.Endereco;
            cliente.LimiteCredito = dto.LimiteCredito;
            cliente.Ativo = dto.Ativo;

            await _context.SaveChangesAsync(ct);

            return new ClienteDto
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                CpfCnpj = cliente.CpfCnpj,
                Telefone = cliente.Telefone,
                Email = cliente.Email,
                Endereco = cliente.Endereco,
                CadastradoEm = cliente.CadastradoEm,
                Ativo = cliente.Ativo,
                LimiteCredito = cliente.LimiteCredito,
                SaldoDevedor = cliente.SaldoDevedor
            };
        }

        public async Task<bool> AtivarDesativarAsync(int id, bool ativo, CancellationToken ct = default)
        {
            var cliente = await _context.Clientes.FindAsync(new object[] { id }, ct);
            if (cliente == null)
                return false;
            cliente.Ativo = ativo;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ReceberDebitoAsync(int id, decimal valor, CancellationToken ct = default)
        {
            if (valor <= 0)
                throw new ArgumentException("Valor deve ser maior que zero.");

            var cliente = await _context.Clientes.FindAsync(new object[] { id }, ct);
            if (cliente == null)
                return false;

            cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - valor);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}