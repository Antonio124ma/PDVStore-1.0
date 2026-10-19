using Microsoft.EntityFrameworkCore;
using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Enums;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;

namespace PDVStore.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly PDVContext _context;

        public UsuarioService(PDVContext context)
        {
            _context = context;
        }

        public async Task<UsuarioDto?> AutenticarAsync(string nome, string senha, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Nome == nome, ct);
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
                return null;
            if (!usuario.Ativo)
                return null;

            return new UsuarioDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Permissao = usuario.Permissao.ToString(),
                Ativo = usuario.Ativo,
                FotoUrl = usuario.FotoUrl
            };
        }

        public async Task<UsuarioDto> CriarAsync(UsuarioCreateDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new ArgumentException("Nome é obrigatório.");

            var usuario = new UsuarioCaixa
            {
                Nome = dto.Nome,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                Permissao = (TipoPermissao)dto.Permissao,
                CreatedAt = DateTime.UtcNow,
                Ativo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(ct);

            return new UsuarioDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Permissao = usuario.Permissao.ToString(),
                Ativo = usuario.Ativo
            };
        }

        public async Task<UsuarioDto?> ObterPorIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Usuarios.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UsuarioDto
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Permissao = u.Permissao.ToString(),
                    Ativo = u.Ativo,
                    FotoUrl = u.FotoUrl
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<UsuarioDto>> ListarAsync(bool apenasAtivos = true, CancellationToken ct = default)
        {
            var query = _context.Usuarios.AsNoTracking().AsQueryable();
            if (apenasAtivos)
                query = query.Where(u => u.Ativo);
            return await query.Select(u => new UsuarioDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Permissao = u.Permissao.ToString(),
                Ativo = u.Ativo,
                FotoUrl = u.FotoUrl
            }).ToListAsync(ct);
        }

        public async Task<bool> AtualizarAsync(int id, UsuarioUpdateDto dto, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FindAsync(new object[] { id }, ct);
            if (usuario == null)
                return false;

            usuario.Nome = dto.Nome;
            usuario.Permissao = (TipoPermissao)dto.Permissao;
            usuario.Ativo = dto.Ativo;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> AtivarDesativarAsync(int id, bool ativo, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FindAsync(new object[] { id }, ct);
            if (usuario == null)
                return false;
            usuario.Ativo = ativo;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.FindAsync(new object[] { id }, ct);
            if (usuario == null || !BCrypt.Net.BCrypt.Verify(senhaAtual, usuario.SenhaHash))
                return false;

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(novaSenha);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<UsuarioDto>> ListarAdministradoresAsync(CancellationToken ct = default)
        {
            return await _context.Usuarios.AsNoTracking()
                .Where(u => u.Permissao == TipoPermissao.Administrador)
                .Select(u => new UsuarioDto
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Permissao = u.Permissao.ToString(),
                    Ativo = u.Ativo
                })
                .ToListAsync(ct);
        }
    }
}