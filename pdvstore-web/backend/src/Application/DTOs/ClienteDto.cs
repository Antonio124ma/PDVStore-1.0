namespace PDVStore.Application.DTOs
{
    public class ClienteDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? CpfCnpj { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Endereco { get; set; }
        public DateTime CadastradoEm { get; set; }
        public bool Ativo { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal SaldoDevedor { get; set; }
    }

    public class ClienteCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? CpfCnpj { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Endereco { get; set; }
        public decimal LimiteCredito { get; set; } = 0;
    }

    public class ClienteUpdateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? CpfCnpj { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Endereco { get; set; }
        public decimal LimiteCredito { get; set; }
        public bool Ativo { get; set; }
    }

    public class ReceberDebitoDto
    {
        public decimal Valor { get; set; }
    }
}