import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { LogOut, LayoutDashboard, ShoppingCart, Package, CreditCard, Users, ShieldAlert } from 'lucide-react';
import { useState } from 'react';

export default function Layout({ onLogout }: { onLogout: () => void }) {
  const { usuario } = useAuth();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const isAdmin = usuario?.permissao === 'Administrador';
  const isOperador = usuario?.permissao === 'Operador';
  const isEstoquista = usuario?.permissao === 'Estoquista';

  const menuItems = [
    { path: '/pdv', label: 'PDV / Vendas', icon: ShoppingCart, allowed: isOperador || isAdmin },
    { path: '/produtos', label: 'Produtos', icon: Package, allowed: isEstoquista || isAdmin },
    { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, allowed: isAdmin },
    ...(isAdmin ? [{ path: '/usuarios', label: 'Usuários', icon: Users, allowed: true }] : []),
    ...(isAdmin ? [{ path: '/caixa', label: 'Caixa', icon: CreditCard, allowed: true }] : []),
  ].filter((item) => item.allowed);

  return (
    <div className="flex h-screen bg-gray-50">
      <aside
        className={`fixed inset-y-0 left-0 z-50 w-64 bg-white border-r border-gray-200 transform transition-transform duration-300 ${
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="flex items-center justify-between h-16 px-4 border-b border-gray-200">
          <h1 className="text-xl font-bold text-gray-800">PDVStore</h1>
          <button onClick={() => setSidebarOpen(false)} className="text-gray-400 hover:text-gray-600">
            <LogOut size={20} />
          </button>
        </div>
        <nav className="py-4 space-y-1">
          {menuItems.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) =>
                `flex items-center px-4 py-3 text-sm font-medium rounded-lg transition ${
                  isActive
                    ? 'bg-blue-100 text-blue-700'
                    : 'text-gray-600 hover:bg-gray-100'
                }`
              }
            >
              <item.icon size={18} className="mr-3" />
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="absolute bottom-0 w-full p-4 border-t border-gray-200">
          <p className="text-xs text-gray-500 px-4 mb-2">{usuario?.nome}</p>
          <p className="text-xs text-gray-400 px-4">{usuario?.permissao}</p>
          <button
            onClick={() => {
              onLogout();
              navigate('/login');
            }}
            className="w-full flex items-center justify-center px-4 py-2 text-sm text-red-600 hover:bg-red-50 rounded-lg"
          >
            <LogOut size={16} className="mr-2" /> Sair
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden">
        <header className="h-16 border-b border-gray-200 bg-white flex items-center justify-between px-6">
          <button
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="md:hidden text-gray-600 hover:text-gray-800"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-600">{usuario?.nome}</span>
            <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">{usuario?.permissao}</span>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}