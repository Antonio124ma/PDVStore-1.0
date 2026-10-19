import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import { toast } from 'sonner';
import { Search, Plus, Edit2, Trash2 } from 'lucide-react';
import type { ProdutoDto, ProdutoCreateDto } from '@/types';

export default function ProdutosPage() {
  const [produtos, setProdutos] = useState<ProdutoDto[]>([]);
  const [termo, setTermo] = useState('');
  const [carregando, setCarregando] = useState(false);
  const [abertoModal, setAbertoModal] = useState(false);
  const [editando, setEditando] = useState<ProdutoDto | null>(null);
  const [novoNome, setNovoNome] = useState('');
  const [novoPreco, setNovoPreco] = useState('');

  useEffect(() => {
    carregarProdutos();
  }, []);

  const carregarProdutos = async () => {
    setCarregando(true);
    try {
      const resp = await api.get<ProdutoDto[]>('/produtos');
      setProdutos(resp.data);
    } catch {
      toast.error('Erro ao carregar produtos.');
    } finally {
      setCarregando(false);
    }
  };

  const filtrados = produtos.filter((p) =>
    p.nome.toLowerCase().includes(termo.toLowerCase()) ||
    p.codigoBarras?.includes(termo)
  );

  const handleSalvar = async () => {
    const preco = parseFloat(novoPreco);
    if (!novoNome || isNaN(preco)) {
      toast.error('Preencha nome e preço corretamente.');
      return;
    }

    const dto: ProdutoCreateDto = {
      nome: novoNome,
      preco: preco,
      precoCusto: 0,
      estoque: 0,
    };

    try {
      if (editando) {
        await api.put(`/produtos/${editando.id}`, { ...dto, id: editando.id });
        toast.success('Produto atualizado.');
      } else {
        await api.post('/produtos', dto);
        toast.success('Produto criado.');
      }
      setAbertoModal(false);
      setEditando(null);
      setNovoNome('');
      setNovoPreco('');
      carregarProdutos();
    } catch {
      toast.error('Erro ao salvar produto.');
    }
  };

  const handleExcluir = async (id: number) => {
    if (!confirm('Deseja realmente desativar este produto?')) return;
    try {
      await api.delete(`/produtos/${id}`);
      toast.success('Produto desativado.');
      carregarProdutos();
    } catch {
      toast.error('Erro ao desativar produto.');
  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Produtos</h1>
          <p className="text-gray-600 text-sm">Gerencie o catálogo de produtos e estoque.</p>
        </div>
        <button
          onClick={() => { setEditando(null); setNovoNome(''); setNovoPreco(''); setAbertoModal(true); }}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          <Plus size={18} /> Novo Produto
        </button>
      </div>

      <div className="flex gap-2">
        <input
          type="text"
          placeholder="Buscar por nome ou código de barras"
          value={termo}
          onChange={(e) => setTermo(e.target.value)}
          className="flex-1 border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
        />
        <button
          onClick={carregarProdutos}
          className="px-4 py-2.5 border border-gray-300 rounded-lg hover:bg-gray-100"
        >
          <Search size={18} />
        </button>
      </div>

      <div className="border rounded-lg overflow-hidden">
        <table className="w-full text-left">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Produto</th>
              <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase">Código Barras</th>
              <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase text-right">Preço</th>
              <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase text-right">Estoque</th>
              <th className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase text-center">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {carregando ? (
              <tr>
                <td colSpan={5} className="p-8 text-center text-gray-500">Carregando...</td>
              </tr>
            ) : filtrados.length === 0 ? (
              <tr>
                <td colSpan={5} className="p-8 text-center text-gray-500">Nenhum produto encontrado.</td>
              </tr>
            ) : (
              filtrados.map((p) => (
                <tr key={p.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <p className="text-sm font-medium">{p.nome}</p>
                    {p.estoqueAbaixoDoMinimo && (
                      <span className="inline-block mt-1 px-2 py-0.5 text-xs bg-orange-100 text-orange-700 rounded">
                        Estoque baixo
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-600">{p.codigoBarras || '-'}</td>
                  <td className="px-4 py-3 text-sm font-medium text-right">R$ {p.preco.toFixed(2)}</td>
                  <td className="px-4 py-3 text-sm text-right">
                    {p.estoqueAbaixoDoMinimo ? (
                      <span className="text-orange-600 font-semibold">{p.estoque}</span>
                    ) : (
                      p.estoque
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <button
                      onClick={() => { setEditando(p); setNovoNome(p.nome); setNovoPreco(p.preco.toString()); setAbertoModal(true); }}
                      className="text-blue-600 hover:text-blue-700 text-sm"
                    >
                      <Edit2 size={16} /> Editar
                    </button>
                    <button
                      onClick={() => handleExcluir(p.id)}
                      className="ml-2 text-red-600 hover:text-red-700 text-sm"
                    >
                      <Trash2 size={16} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {abertoModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" onClick={() => setAbertoModal(false)}>
          <div className="bg-white p-6 rounded-lg w-full max-w-md shadow-lg" onClick={(e) => e.stopPropagation()}>
            <h2 className="text-lg font-bold mb-4">{editando ? 'Editar Produto' : 'Novo Produto'}</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Nome</label>
                <input
                  value={novoNome}
                  onChange={(e) => setNovoNome(e.target.value)}
                  className="w-full border p-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                  placeholder="Nome do produto"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Preço (R$)</label>
                <input
                  type="number"
                  step="0.01"
                  value={novoPreco}
                  onChange={(e) => setNovoPreco(e.target.value)}
                  className="w-full border p-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                  placeholder="0,00"
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 mt-6">
              <button
                onClick={() => setAbertoModal(false)}
                className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg"
              >
                Cancelar
              </button>
              <button
                onClick={handleSalvar}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
              >
                Salvar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  };