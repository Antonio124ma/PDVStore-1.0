import { useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { User, Lock, ArrowRight, Store } from 'lucide-react';

export default function LoginPage() {
  const { login, loading } = useAuth();
  const [usuario, setUsuario] = useState('');
  const [senha, setSenha] = useState('');

  return (
    <div className="flex h-screen items-center justify-center bg-gray-50">
      <div className="w-full max-w-md space-y-8 p-8 bg-white rounded-xl shadow-sm border border-gray-200">
        <div className="text-center">
          <div className="flex justify-center">
            <Store size={48} className="text-blue-600" />
          </div>
          <h2 className="mt-4 text-2xl font-bold text-gray-900">PDVStore</h2>
          <p className="text-sm text-gray-600">Sistema de Ponto de Venda</p>
        </div>

        <form onSubmit={(e) => { e.preventDefault(); login({ usuario, senha }); }}>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">Usuário</label>
              <div className="relative">
                <User size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  name="usuario"
                  value={usuario}
                  onChange={(e) => setUsuario(e.target.value)}
                  required
                  className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  placeholder="admin"
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">Senha</label>
              <div className="relative">
                <Lock size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  type="password"
                  name="senha"
                  value={senha}
                  onChange={(e) => setSenha(e.target.value)}
                  required
                  className="block w-full pl-10 pr-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                  placeholder="••••••••"
                />
              </div>
            </div>
            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
            >
              {loading ? (
                <ArrowRight size={18} className="animate-spin" />
              ) : (
                <>
                  <ArrowRight size={18} /> Acessar
                </>
              )}
            </button>
          </div>
        </form>

        <div className="text-center text-xs text-gray-500">
          Admin / admin123
        </div>
      </div>

      <div className="hidden md:block absolute top-0 left-0 w-full h-full bg-white/50 backdrop-blur-sm">
        <Monitor size={120} className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 text-gray-200" />
      </div>
    </div>
  );
}