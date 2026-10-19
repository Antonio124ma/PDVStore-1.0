namespace PDVStore.Application.DTOs
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Permissao { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public string? FotoUrl { get; set; }
    }

    public class UsuarioCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public TipoPermissaoDto Permissao { get; set; } = TipoPermissaoDto.Operador;
    }

    public class UsuarioUpdateDto
    {
        public string Nome { get; set; } = string.Empty;
        public TipoPermissaoDto Permissao { get; set; } = TipoPermissaoDto.Operador;
        public bool Ativo { get; set; }
    }

    public enum TipoPermissaoDto
    {
        Operador = 1,
        Administrador = 2,
        Estoquista = 3
    }

    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UsuarioDto Usuario { get; set; } = new();
    }
}