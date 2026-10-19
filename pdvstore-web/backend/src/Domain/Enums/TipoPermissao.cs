namespace PDVStore.Domain.Enums
{
    public enum TipoPermissao
    {
        Operador = 1,      // Acesso somente ao PDV (vendas)
        Administrador = 2, // Acesso total
        Estoquista = 3     // Acesso somente ao cadastro de produtos
    }
}