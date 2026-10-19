import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { api } from '@/lib/api';
import { toast } from 'sonner';
import { Barcode, DollarSign, CreditCard, RefreshCw, Print } from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import type { VendaCreateDto, ProdutoDto } from '@/types';

const vendaSchema = z.object({
  formaPagamento: z.enum(['Dinheiro', 'PIX', 'CartaoCredito', 'CartaoDebito', 'Fiado']),
  desconto: z.coerce.number().min(0).default(0),
  clienteId: z.coerce.number().optional(),
  valorRecebido: z.coerce.number().optional(),
});

type VendaForm = z.infer<typeof vendaSchema>;

interface CarrinhoItem {
  produtoId: number;
  nome: string;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

export default function PDVPage() {
  const { usuario } = useAuth();
  const [itens, setItens] = useState<CarrinhoItem[]>([]);
  const [termoBusca, setTermoBusca] = useState('');
  const [buscando, setBuscando] = useState(false);
  const [erroBusca, setErroBusca] = useState<string | null>(null);
  const [carregandoVenda, setCarregandoVenda] = useState(false);

  const { register, handleSubmit, reset, formState: { values: formValues } } = useForm<VendaForm>({
    resolver: zodResolver(vendaSchema),
    defaultValues: { formaPagamento: 'Dinheiro', desconto: 0 },
  });

  const total = itens.reduce((acc, i) => acc + i.subtotal, 0) - (formValues?.desconto ?? 0);

  const onProdutoSelecionado = (produto: ProdutoDto) => {
    if (produto.estoque <= 0) {
      toast.error('Produto sem estoque disponível.');
      return;
    }
    setItens((prev) => {
      const existente = prev.find((i) => i.produtoId === produto.id);
      if (existente) {
        if (existente.quantidade >= produto.estoque) {
          toast.error('Estoque insuficiente.');
          return prev;
        }
        return prev.map((i) =>
          i.produtoId === produto.id
            ? { ...i, quantidade: i.quantidade + 1, subtotal: (i.quantidade + 1) * i.precoUnitario }
            : i
        );
      }
      return [...prev, { produtoId: produto.id, nome: produto.nome, quantidade: 1, precoUnitario: produto.preco, subtotal: produto.preco }];
    });
    setTermoBusca('');
  };

  const handleBusca = async (e: React.KeyboardEvent) => {
    if (e.key !== 'Enter') return;
    if (!termoBusca.trim()) return;
    setBuscando(true);
    setErroBusca(null);
    try {
      const resp = await api.get<ProdutoDto[]>(`/produtos/busca?q=${encodeURIComponent(termoBusca)}`);
      if (resp.data.length === 0) {
        setErroBusca('Produto não encontrado.');
        return;
      }
      onProdutoSelecionado(resp.data[0]);
    } catch {
      setErroBusca('Erro ao buscar produto.');
    } finally {
      setBuscando(false);
    }
  };

  const removerItem = (index: number) => {
    setItens(itens.filter((_, i) => i !== index));
  const onSubmit = async (data: VendaForm) => {
    setCarregandoVenda(true);
    try {
      const dto: VendaCreateDto = {
        usuarioCaixaId: Number(localStorage.getItem('usuarioId') || 0),
        formaPagamento: data.formaPagamento,
        desconto: data.desconto,
        valorTotal: total,
        clienteId: data.clienteId,
        itens: itens.map((i) => ({
          produtoId: i.produtoId,
          quantidade: i.quantidade,
          precoUnitario: i.precoUnitario,
        })),
      };

      const resp = await api.post('/vendas', dto);
      toast.success(`Venda #${resp.data.id} registrada com sucesso!`);

      if (data.formaPagamento === 'Dinheiro' && data.valorRecebido != null) {
        const troco = data.valorRecebido - total;
        toast.info(`Troco: R$ ${troco.toFixed(2)}`);
      }

      try {
        const pdfResp = await api.get(`/vendas/${resp.data.id}/recibo`, { responseType: 'blob' });
        const url = URL.createObjectURL(pdfResp.data as Blob);
        window.open(url, '_blank');
      } catch {
        toast.warning('Não foi possível gerar o recibo.');
      }

      reset();
      setItens([]);
    } catch (err: any) {
      toast.error(err.response?.data?.mensagem || 'Erro ao registrar venda.');
    } finally {
      setCarregandoVenda(false);
    }
  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 h-full">
      <div className="lg:col-span-8">
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Código de barras ou nome (Enter)"
            value={termoBusca}
            onChange={(e) => setTermoBusca(e.target.value)}
            onKeyDown={handleBusca}
            className="flex-1 border border-gray-300 p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm"
          />
          {buscando && <span className="text-xs text-gray-500">Buscando...</span>}
          {erroBusca && <span className="text-xs text-red-500">{erroBusca}</span>}
        </div>

        <div className="mt-4 border rounded-lg p-4 bg-white">
          <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
            <Barcode size={20} /> Carrinho ({itens.length} itens)
          </h3>
          {itens.length === 0 ? (
            <p className="text-gray-500 text-sm">Carrinho vazio. Busque um produto para começar.</p>
          ) : (
            <div className="space-y-2 max-h-96 overflow-y-auto pr-2">
              {itens.map((item, i) => (
                <div key={i} className="flex items-center justify-between p-2 border-b border-gray-100">
                  <div className="flex-1">
                    <p className="text-sm font-medium">{item.nome}</p>
                    <p className="text-xs text-gray-500">R$ {item.precoUnitario.toFixed(2)} x {item.quantidade}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-semibold">R$ {item.subtotal.toFixed(2)}</p>
                    <button
                      onClick={() => removerItem(i)}
                      className="text-red-500 hover:text-red-700 text-xs"
                    >
                      Remover
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="lg:col-span-4">
        <div className="p-6 border rounded-lg bg-gray-50">
          <h3 className="text-lg font-semibold mb-6 flex items-center gap-2">
            <DollarSign size={20} /> Resumo da Venda
          </h3>

          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">Subtotal</span>
              <span>R$ {itens.reduce((a, i) => a + i.subtotal, 0).toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Desconto</span>
              <span>R$ {(formValues?.desconto ?? 0).toFixed(2)}</span>
            </div>
            <div className="flex justify-between text-lg font-bold border-t pt-2">
              <span>Total</span>
              <span>R$ {total.toFixed(2)}</span>
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-3">
            <select
              {...register('formaPagamento')}
              className="w-full border p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
            >
              <option value="Dinheiro">Dinheiro</option>
              <option value="PIX">PIX</option>
              <option value="CartaoCredito">Cartão de Crédito</option>
              <option value="CartaoDebito">Cartão de Débito</option>
              <option value="Fiado">Fiado</option>
            </select>

            {formValues?.formaPagamento === 'Dinheiro' && (
              <input
                {...register('valorRecebido')}
                type="number"
                placeholder="Valor recebido"
                className="w-full border p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              />
            )}

            {formValues?.formaPagamento === 'Fiado' && (
              <input
                {...register('clienteId')}
                type="number"
                placeholder="ID do Cliente"
                className="w-full border p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
              />
            )}

            <input
              {...register('desconto')}
              type="number"
              placeholder="Desconto (R$)"
              className="w-full border p-2.5 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
            />

            <button
              type="submit"
              disabled={carregandoVenda || itens.length === 0}
              className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
            >
              {carregandoVenda ? (
                <>
                  <RefreshCw size={18} className="animate-spin" /> Processando...
                </>
              ) : (
                <>
                  <DollarSign size={18} /> Finalizar Venda
                </>
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  };
  };