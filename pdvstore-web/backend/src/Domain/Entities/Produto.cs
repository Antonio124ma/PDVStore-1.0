using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class Produto : IHasId
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CodigoBarras { get; set; }

        [Required]
        public decimal Preco { get; set; }

        [Required]
        public decimal PrecoCusto { get; set; }

        [Required]
        public int Estoque { get; set; }

        public int EstoqueMinimo { get; set; } = 0;

        [MaxLength(100)]
        public string? Categoria { get; set; }

        [MaxLength(500)]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        public bool EstoqueAbaixoDoMinimo => Estoque <= EstoqueMinimo;
    }
}