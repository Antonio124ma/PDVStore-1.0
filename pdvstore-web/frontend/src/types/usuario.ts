export type TipoPermissao = 'Administrador' | 'Operador' | 'Estoquista';

export interface UsuarioDto {
  id: number;
  nome: string;
  permissao: TipoPermissao;
  ativo: boolean;
  fotoUrl?: string;
}

export interface LoginRequest {
  usuario: string;
  senha: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  usuario: UsuarioDto;
}