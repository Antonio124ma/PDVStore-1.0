using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class Venda : IHasId
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioCaixaId { get; set; }

        public DateTime DataVenda { get; set; } = DateTime.UtcNow;

        [Required]
        public decimal ValorTotal { get; set; }

        public decimal Desconto { get; set; } = 0;

        [Required]
        public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Dinheiro;

        public string? PixTxId { get; set; }

        [Required]
        public StatusVenda Status { get; set; } = StatusVenda.Concluida;

        [Required]
        public int CaixaId { get; set; } = 1;

        public int? ClienteId { get; set; }

        public UsuarioCaixa? UsuarioCaixa { get; set; }
        public Caixa? Caixa { get; set; }
        public Cliente? Cliente { get; set; }

        public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();

        public decimal SubtotalItens => Itens.Sum(i => i.Subtotal);
    }
}