import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { toast } from 'sonner';
import LoginPage from '@/features/auth/pages/LoginPage';
import PDVPage from '@/features/pdv/pages/PDVPage';
import ProdutosPage from '@/features/produtos/pages/ProdutosPage';
import DashboardPage from '@/features/dashboard/pages/DashboardPage';
import Layout from '@/components/layout/Layout';

function AuthRequired({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

export default function App() {
  const { isAuthenticated, logout } = useAuth();

  if (!isAuthenticated) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  return (
    <>
      <Layout onLogout={logout}>
        <Routes>
          <Route path="/pdv" element={<PDVPage />} />
          <Route path="/produtos" element={<ProdutosPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/" element={<Navigate to="/pdv" replace />} />
        </Routes>
      </Layout>
    </>
  );
}