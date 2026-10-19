using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class ItemVenda : IHasId
    {
        public int Id { get; set; }

        [Required]
        public int VendaId { get; set; }

        [Required]
        public int ProdutoId { get; set; }

        [Required]
        public int Quantidade { get; set; }

        [Required]
        public decimal PrecoUnitario { get; set; }

        public decimal Subtotal => Quantidade * PrecoUnitario;

        public Produto? Produto { get; set; }
        public Venda? Venda { get; set; }
    }
}