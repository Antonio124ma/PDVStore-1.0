using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class Caixa : IHasId
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioCaixaId { get; set; }

        public DateTime Abertura { get; set; } = DateTime.UtcNow;

        public DateTime? Fechamento { get; set; }

        [Required]
        public decimal ValorInicial { get; set; } = 0;

        public decimal? ValorFinal { get; set; }

        public decimal? Sangria { get; set; }

        [Required]
        public StatusCaixa Status { get; set; } = StatusCaixa.Aberto;

        public UsuarioCaixa? UsuarioCaixa { get; set; }

        public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
    }
}