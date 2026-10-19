using System.ComponentModel.DataAnnotations;

namespace PDVStore.Domain.Entities
{
    public class Cliente : IHasId
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? CpfCnpj { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Endereco { get; set; }

        public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;

        [Required]
        public decimal LimiteCredito { get; set; } = 0;

        [Required]
        public decimal SaldoDevedor { get; set; } = 0;

        public bool TemCreditoDisponivel(decimal valor) =>
            SaldoDevedor + valor <= LimiteCredito;
    }
}