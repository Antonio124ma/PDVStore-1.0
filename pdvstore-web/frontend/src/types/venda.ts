export type FormaPagamento = 'Dinheiro' | 'PIX' | 'CartaoCredito' | 'CartaoDebito' | 'Fiado';

export interface ItemVendaDto {
  produtoId: number;
  nomeProduto: string;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

export interface VendaCreateDto {
  usuarioCaixaId: number;
  formaPagamento: FormaPagamento;
  desconto: number;
  valorTotal: number;
  clienteId?: number;
  itens: ItemVendaCreateDto[];
}

export interface ItemVendaCreateDto {
  produtoId: number;
  quantidade: number;
  precoUnitario: number;
}

export interface VendaDto {
  id: number;
  usuarioCaixaId: number;
  usuarioNome: string;
  dataVenda: string;
  valorTotal: number;
  desconto: number;
  formaPagamento: FormaPagamento;
  pixTxId?: string;
  status: string;
  caixaId: number;
  clienteId?: number;
  clienteNome?: string;
  itens: ItemVendaDto[];
}