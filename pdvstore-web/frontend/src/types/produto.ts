export interface ProdutoDto {
  id: number;
  nome: string;
  codigoBarras?: string;
  preco: number;
  precoCusto: number;
  estoque: number;
  estoqueMinimo: number;
  categoria?: string;
  descricao?: string;
  ativo: boolean;
  estoqueAbaixoDoMinimo: boolean;
}

export interface ProdutoCreateDto {
  nome: string;
  codigoBarras?: string;
  preco: number;
  precoCusto: number;
  estoque: number;
  estoqueMinimo?: number;
  categoria?: string;
  descricao?: string;
}

export interface ProdutoUpdateDto {
  nome: string;
  codigoBarras?: string;
  preco: number;
  precoCusto: number;
  estoqueMinimo: number;
  categoria?: string;
  descricao?: string;
  ativo: boolean;
}