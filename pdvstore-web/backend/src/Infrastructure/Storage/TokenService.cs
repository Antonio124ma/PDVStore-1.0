using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PDVStore.Infrastructure.Storage
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(UsuarioCaixa usuario)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
            var issuer = jwtSettings["Issuer"] ?? "PDVStore";
            var audience = jwtSettings["Audience"] ?? "PDVStore";
            var expireMinutes = int.Parse(jwtSettings["ExpireMinutes"] ?? "15");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Permissao.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        public Task SalvarRefreshToken(int usuarioId, string refreshToken, CancellationToken ct = default)
        {
            // Em produção, salvar no banco ou Redis com data de expiração
            return Task.CompletedTask;
        }

        public Task<bool> ValidarRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            // Em produção, validar contra banco/Redis
            return Task.FromResult(!string.IsNullOrEmpty(refreshToken));
        }

        public Task RevogarRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            // Em produção, revogar no banco/Redis
            return Task.CompletedTask;
        }
    }
}