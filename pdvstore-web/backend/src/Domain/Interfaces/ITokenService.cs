using PDVStore.Domain.Entities;

namespace PDVStore.Domain.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(UsuarioCaixa usuario);
        string GenerateRefreshToken();
        Task SalvarRefreshToken(int usuarioId, string refreshToken, CancellationToken ct = default);
        Task<bool> ValidarRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
        Task RevogarRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    }
}