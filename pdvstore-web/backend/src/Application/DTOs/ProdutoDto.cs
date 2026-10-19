namespace PDVStore.Application.DTOs
{
    public class ProdutoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoCusto { get; set; }
        public int Estoque { get; set; }
        public int EstoqueMinimo { get; set; }
        public string? Categoria { get; set; }
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
        public bool EstoqueAbaixoDoMinimo => Estoque <= EstoqueMinimo;
    }

    public class ProdutoCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoCusto { get; set; }
        public int Estoque { get; set; }
        public int EstoqueMinimo { get; set; }
        public string? Categoria { get; set; }
        public string? Descricao { get; set; }
    }

    public class ProdutoUpdateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoCusto { get; set; }
        public int EstoqueMinimo { get; set; }
        public string? Categoria { get; set; }
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
    }
}