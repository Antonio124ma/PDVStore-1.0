using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class MovimentacaoEstoque : IHasId
    {
        public int Id { get; set; }

        [Required]
        public int ProdutoId { get; set; }

        [Required]
        public TipoMovimentacaoEstoque Tipo { get; set; }

        [Required]
        public int Quantidade { get; set; }

        public decimal? PrecoUnitario { get; set; }

        public DateTime DataMovimentacao { get; set; } = DateTime.UtcNow;

        public int? UsuarioId { get; set; }

        [MaxLength(200)]
        public string? Motivo { get; set; }

        public int? ReferenciaVendaId { get; set; }

        public Produto? Produto { get; set; }
        public UsuarioCaixa? Usuario { get; set; }
    }
}