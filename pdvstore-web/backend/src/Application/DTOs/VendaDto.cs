namespace PDVStore.Application.DTOs
{
    public class VendaDto
    {
        public int Id { get; set; }
        public int UsuarioCaixaId { get; set; }
        public string? UsuarioNome { get; set; }
        public DateTime DataVenda { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal Desconto { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string? PixTxId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CaixaId { get; set; }
        public int? ClienteId { get; set; }
        public string? ClienteNome { get; set; }
        public ICollection<ItemVendaDto> Itens { get; set; } = new List<ItemVendaDto>();
    }

    public class ItemVendaDto
    {
        public int Id { get; set; }
        public int VendaId { get; set; }
        public int ProdutoId { get; set; }
        public string? NomeProduto { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal => Quantidade * PrecoUnitario;
    }

    public class VendaCreateDto
    {
        public int UsuarioCaixaId { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public decimal Desconto { get; set; } = 0;
        public decimal ValorTotal { get; set; }
        public int? ClienteId { get; set; }
        public ICollection<ItemVendaCreateDto> Itens { get; set; } = new List<ItemVendaCreateDto>();
    }

    public class ItemVendaCreateDto
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
    }
}