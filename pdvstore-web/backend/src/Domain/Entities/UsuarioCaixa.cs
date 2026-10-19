using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class UsuarioCaixa : IHasId
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? FotoUrl { get; set; }

        public bool Ativo { get; set; } = true;

        public TipoPermissao Permissao { get; set; } = TipoPermissao.Operador;

        public bool EhAdmin() => Permissao == TipoPermissao.Administrador;

        public bool PodeGerenciarUsuarios() => Permissao == TipoPermissao.Administrador;
    }
}