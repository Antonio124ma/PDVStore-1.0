using PDVStore.Application.DTOs;
using PDVStore.Domain.Entities;

namespace PDVStore.Domain.Interfaces
{
    public interface IUsuarioService
    {
        Task<UsuarioDto?> AutenticarAsync(string nome, string senha, CancellationToken ct = default);
        Task<UsuarioDto> CriarAsync(UsuarioCreateDto dto, CancellationToken ct = default);
        Task<UsuarioDto?> ObterPorIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<UsuarioDto>> ListarAsync(bool apenasAtivos = true, CancellationToken ct = default);
        Task<bool> AtualizarAsync(int id, UsuarioUpdateDto dto, CancellationToken ct = default);
        Task<bool> AtivarDesativarAsync(int id, bool ativo, CancellationToken ct = default);
        Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha, CancellationToken ct = default);
        Task<IEnumerable<UsuarioDto>> ListarAdministradoresAsync(CancellationToken ct = default);
    }
}