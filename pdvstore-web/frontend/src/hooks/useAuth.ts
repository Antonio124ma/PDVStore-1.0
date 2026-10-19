import { useState } from 'react';
import { api } from '@/lib/api';
import type { UsuarioDto, LoginRequest, LoginResponse } from '@/types/usuario';

export function useAuth() {
  const [usuario, setUsuario] = useState<UsuarioDto | null>(null);
  const [loading, setLoading] = useState(false);

  const login = async (data: LoginRequest) => {
    setLoading(true);
    try {
      const { data: resp } = await api.post<LoginResponse>('/auth/login', data);
      localStorage.setItem('accessToken', resp.accessToken);
      localStorage.setItem('refreshToken', resp.refreshToken);
      localStorage.setItem('usuario', JSON.stringify(resp.usuario));
      setUsuario(resp.usuario);
      return resp;
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuario');
    setUsuario(null);
  };

  const init = () => {
    const u = localStorage.getItem('usuario');
    if (u) setUsuario(JSON.parse(u));
  };

  return { usuario, login, logout, loading, isAuthenticated: !!usuario, init };
}