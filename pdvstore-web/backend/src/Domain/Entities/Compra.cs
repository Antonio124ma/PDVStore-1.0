using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class Compra : IHasId
    {
        public int Id { get; set; }

        [Required]
        public int FornecedorId { get; set; }

        [Required]
        public int UsuarioCaixaId { get; set; }

        public DateTime DataCompra { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? NumeroNota { get; set; }

        [Required]
        public decimal ValorTotal { get; set; }

        [Required]
        public string Status { get; set; } = "Concluida";

        public Fornecedor? Fornecedor { get; set; }
        public UsuarioCaixa? UsuarioCaixa { get; set; }

        public ICollection<ItemCompra> Itens { get; set; } = new List<ItemCompra>();
    }
}