namespace PDVStore.Application.DTOs
{
    public class CaixaDto
    {
        public int Id { get; set; }
        public int UsuarioCaixaId { get; set; }
        public string? UsuarioNome { get; set; }
        public DateTime Abertura { get; set; }
        public DateTime? Fechamento { get; set; }
        public decimal ValorInicial { get; set; }
        public decimal? ValorFinal { get; set; }
        public decimal? Sangria { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AbrirCaixaDto
    {
        public decimal ValorInicial { get; set; }
        public int UsuarioCaixaId { get; set; }
    }

    public class SangriaDto
    {
        public decimal Valor { get; set; }
    }
}