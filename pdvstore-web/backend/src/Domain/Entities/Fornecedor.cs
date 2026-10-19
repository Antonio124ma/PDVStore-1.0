using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class Fornecedor : IHasId
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Cnpj { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Endereco { get; set; }

        public bool Ativo { get; set; } = true;

        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    }
}