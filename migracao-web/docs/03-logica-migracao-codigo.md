# PDVStore — Plano de Migração para Web
## Etapa 3: Lógica de Migração de Código

> **Objetivo:** Mostrar exemplos práticos de como converter uma função típica do C# WinForms (consulta ao banco + renderização na tela) para a nova stack (.NET 8 Web API + React + TypeScript).  
> **Abordagem:** Porta-se a lógica de negócio quase que 1:1 (mesmos nomes de serviços e regras), apenas mudando a camada de apresentação e a forma de acesso (HTTP em vez de chamada direta de método).

---

## 3.1 Exemplo 1: Consulta de Produto por Código de Barras (PDV)

### Antes — WinForms (`EstoqueService.GetByCodigoBarrasAsync` + `frmPDV`)

```csharp
// Camada de serviço (C# WinForms)
public async Task<Produto?> GetByCodigoBarrasAsync(string codigoBarras)
{
    return await _context.Produtos
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras && p.Ativo);
}

// No formulário PDV (WinForms), ao digitar no TextBox:
private async void txtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
{
    if (e.KeyCode == Keys.Enter)
    {
        var produto = await _estoqueService.GetByCodigoBarrasAsync(txtCodigoBarras.Text);
        if (produto != null)
        {
            _viewModel.Itens.Add(new ItemVenda
            {
                ProdutoId = produto.Id,
                Quantidade = 1,
                PrecoUnitario = produto.Preco
            });
        }
        else
        {
            MessageBox.Show("Produto não encontrado.");
        }
    }
}
```

### Depois — Backend (.NET 8 Web API Controller)

```csharp
// backend/src/Api/Controllers/ProdutosController.cs
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _produtoService;

    public ProdutosController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    [HttpGet("busca")]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> Busca(
        [FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Enumerable.Empty<ProdutoDto>());

        var produtos = await _produtoService.BuscarAsync(q, ct);
        var dtos = produtos.Select(p => new ProdutoDto
        {
            Id = p.Id,
            Nome = p.Nome,
            CodigoBarras = p.CodigoBarras,
            Preco = p.Preco,
            Estoque = p.Estoque,
            Ativo = p.Ativo
        });

        return Ok(dtos);
    }
}
```

### Depois — Frontend (React + TypeScript)

```tsx
// frontend/src/features/produtos/components/BuscaProduto.tsx
import { useState } from 'react';
import { api } from '@/lib/api';
import type { ProdutoDto } from '@/types/produto';

export function BuscaProduto({ onProdutoSelecionado }: { onProdutoSelecionado: (p: ProdutoDto) => void }) {
  const [termo, setTermo] = useState('');
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const handleBuscar = async (e: React.KeyboardEvent) => {
    if (e.key !== 'Enter') return;
    setCarregando(true);
    setErro(null);
    try {
      const resposta = await api.get<ProdutoDto[]>(`/produtos/busca?q=${encodeURIComponent(termo)}`);
      if (resposta.data.length === 0) {
        setErro('Produto não encontrado.');
        return;
      }
      onProdutoSelecionado(resposta.data[0]);
    } catch {
      setErro('Erro ao buscar produto.');
    } finally {
      setCarregando(false);
    }
  };

  return (
    <div className="flex gap-2">
      <input
        type="text"
        placeholder="Código de barras ou nome"
        value={termo}
        onChange={(e) => setTermo(e.target.value)}
        onKeyDown={handleBuscar}
        className="border p-2 rounded text-sm"
      />
      {carregando && <span className="text-xs text-gray-500">Buscando...</span>}
      {erro && <span className="text-xs text-red-500">{erro}</span>}
    </div>
  );
}
```

---

## 3.2 Exemplo 2: Registro de Venda Completa (PDV)

### Antes — WinForms (`VendaService.RegistrarVendaAsync` + `frmPDV.btnFinalizar_Click`)

O serviço C# já faz tudo em uma transação: valida caixa aberto, valida estoque, grava venda+itens, baixa estoque, processa pagamento (mock), commita. A tela mostra `MessageBox` e imprime via `PrintDocument` local.

### Depois — Backend (.NET 8 Web API + Service)

```csharp
// backend/src/Application/Services/VendaService.cs
public class VendaService : IVendaService
{
    private readonly PDVContext _context;
    private readonly IPagamentoIntegrator _pagamentoIntegrator;
    private readonly ICaixaService _caixaService;

    public VendaService(PDVContext context, IPagamentoIntegrator pagamentoIntegrator, ICaixaService caixaService)
    {
        _context = context;
        _pagamentoIntegrator = pagamentoIntegrator;
        _caixaService = caixaService;
    }

    public async Task<VendaDto> RegistrarVendaAsync(VendaCreateDto dto, CancellationToken ct = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            if (dto.Itens == null || !dto.Itens.Any())
                throw new InvalidOperationException("A venda deve conter pelo menos um item.");

            // 0. Exige caixa aberto
            var caixa = await _caixaService.ObterCaixaAbertoAsync(ct);
            if (caixa == null)
                throw new InvalidOperationException("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");

            // 0.1 Venda fiada exige cliente ativo e respeita limite de crédito
            if (dto.FormaPagamento.Equals("Fiado", StringComparison.OrdinalIgnoreCase))
            {
                if (!dto.ClienteId.HasValue)
                    throw new InvalidOperationException("Venda fiada exige um cliente selecionado.");

                var cliente = await _context.Clientes.FindAsync(new object[] { dto.ClienteId.Value }, ct);
                if (cliente == null || !cliente.Ativo)
                    throw new InvalidOperationException("Cliente informado não está ativo.");

                decimal totalItens = dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
                if (cliente.SaldoDevedor + totalItens - dto.Desconto > cliente.LimiteCredito)
                    throw new InvalidOperationException("Crédito insuficiente para venda fiada.");
            }

            // Calcula valor total
            decimal valorTotal = dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario) - dto.Desconto;
            if (valorTotal < 0) valorTotal = 0;

            // 1. Grava cabeçalho + itens
            var venda = new Venda
            {
                UsuarioCaixaId = dto.UsuarioCaixaId,
                CaixaId = caixa.Id,
                DataVenda = DateTime.UtcNow,
                ValorTotal = valorTotal,
                Desconto = dto.Desconto,
                FormaPagamento = dto.FormaPagamento,
                ClienteId = dto.ClienteId,
                Status = "Concluida",
                Itens = dto.Itens.Select(i => new ItemVenda
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario
                }).ToList()
            };

            _context.Vendas.Add(venda);

            // 2. Baixa estoque e registra movimentação
            foreach (var item in venda.Itens)
            {
                var produto = await _context.Produtos.FindAsync(new object[] { item.ProdutoId }, ct);
                if (produto == null || produto.Estoque < item.Quantidade)
                    throw new InvalidOperationException($"Estoque insuficiente para o produto {item.ProdutoId}.");

                produto.Estoque -= item.Quantidade;
                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = "Saída",
                    Quantidade = item.Quantidade,
                    DataMovimentacao = DateTime.UtcNow,
                    UsuarioId = dto.UsuarioCaixaId,
                    Motivo = $"Venda #{venda.Id}"
                });
            }

            // 3. Venda fiada aumenta saldo devedor
            if (venda.ClienteId.HasValue && venda.FormaPagamento.Equals("Fiado", StringComparison.OrdinalIgnoreCase))
            {
                var cliente = await _context.Clientes.FindAsync(new object[] { venda.ClienteId.Value }, ct);
                if (cliente != null)
                    cliente.SaldoDevedor += valorTotal;
            }

            // 4. Processa pagamento (mock PIX gera TxId)
            bool pagamentoOk = await _pagamentoIntegrator.ProcessarPagamentoAsync(venda, ct);
            if (!pagamentoOk)
                throw new InvalidOperationException("Falha ao processar pagamento.");

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return await ObterVendaDtoAsync(venda.Id, ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

### Depois — Controller

```csharp
// backend/src/Api/Controllers/VendasController.cs
[HttpPost]
public async Task<ActionResult<VendaDto>> Registrar(
    [FromBody] VendaCreateDto dto,
    CancellationToken ct)
{
    try
    {
        var venda = await _vendaService.RegistrarVendaAsync(dto, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = venda.Id }, venda);
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { tipo = "validacao-negocio", mensagem = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { tipo = "erro-interno", mensagem = "Erro inesperado ao registrar venda." });
    }
}
```

### Depois — Frontend (React)

```tsx
// frontend/src/features/pdv/components/PDVPage.tsx
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { api } from '@/lib/api';
import { toast } from '@/components/ui/sonner';
import { BuscaProduto } from './BuscaProduto';
import { Carrinho } from './Carrinho';

const vendaSchema = z.object({
  formaPagamento: z.enum(['Dinheiro', 'PIX', 'CartaoCredito', 'CartaoDebito', 'Fiado']),
  desconto: z.number().min(0),
  clienteId: z.number().optional(),
  valorRecebido: z.number().optional(),
});

type VendaForm = z.infer<typeof vendaSchema>;

export function PDVPage() {
  const [itens, setItens] = useState<Array<{
    produtoId: number;
    nome: string;
    quantidade: number;
    precoUnitario: number;
    subtotal: number;
  }>>([]);
  const [desconto, setDesconto] = useState(0);
  const [carregando, setCarregando] = useState(false);

  const total = itens.reduce((acc, i) => acc + i.subtotal, 0) - desconto;

  const { register, handleSubmit, reset } = useForm<VendaForm>({
    resolver: zodResolver(vendaSchema),
    defaultValues: { formaPagamento: 'Dinheiro', desconto: 0 },
  });

  const onProdutoSelecionado = (produto) => {
    setItens(prev => [...prev, {
      produtoId: produto.id,
      nome: produto.nome,
      quantidade: 1,
      precoUnitario: produto.preco,
      subtotal: produto.preco,
    }]);
  };

  const onSubmit = async (data: VendaForm) => {
    setCarregando(true);
    try {
      const resposta = await api.post('/vendas', {
        usuarioCaixaId: Number(localStorage.getItem('usuarioId')),
        formaPagamento: data.formaPagamento,
        desconto: data.desconto,
        valorTotal: total,
        clienteId: data.clienteId,
        itens: itens.map(i => ({
          produtoId: i.produtoId,
          quantidade: i.quantidade,
          precoUnitario: i.precoUnitario,
        })),
      });

      toast.success(`Venda #${resposta.data.id} registrada com sucesso!`);

      if (data.formaPagamento === 'Dinheiro' && data.valorRecebido != null) {
        const troco = data.valorRecebido - total;
        toast.info(`Troco: R$ ${troco.toFixed(2)}`);
      }

      // Gera recibo PDF
      const pdfBlob = await api.get(`/vendas/${resposta.data.id}/recibo`, {
        responseType: 'blob',
      });
      const url = URL.createObjectURL(pdfBlob);
      window.open(url, '_blank');

      reset();
      setItens([]);
      setDesconto(0);
    } catch (err: any) {
      toast.error(err.response?.data?.mensagem || 'Erro ao registrar venda.');
    } finally {
      setCarregando(false);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
      <div className="lg:col-span-8">
        <BuscaProduto onProdutoSelecionado={onProdutoSelecionado} />
        <Carrinho itens={itens} onRemover={(id) => setItens(itens.filter(i => i.produtoId !== id))} />
      </div>
      <div className="lg:col-span-4">
        <div className="p-4 border rounded bg-gray-50">
          <h3 className="text-lg font-semibold mb-4">Resumo da Venda</h3>
          <p className="text-sm text-gray-600">Itens: {itens.length}</p>
          <p className="text-sm">Subtotal: R$ {(itens.reduce((a, i) => a + i.subtotal, 0)).toFixed(2)}</p>
          <p className="text-sm">Desconto: R$ {desconto.toFixed(2)}</p>
          <p className="text-lg font-bold mt-2">Total: R$ {total.toFixed(2)}</p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-3">
            <select {...register('formaPagamento')} className="w-full border p-2 rounded">
              <option value="Dinheiro">Dinheiro</option>
              <option value="PIX">PIX</option>
              <option value="CartaoCredito">Cartão de Crédito</option>
              <option value="CartaoDebito">Cartão de Débito</option>
              <option value="Fiado">Fiado</option>
            </select>

            {data.formaPagamento === 'Dinheiro' && (
              <input {...register('valorRecebido')} type="number" placeholder="Valor recebido" className="w-full border p-2 rounded" />
            )}

            {data.formaPagamento === 'Fiado' && (
              <input {...register('clienteId')} type="number" placeholder="ID do Cliente" className="w-full border p-2 rounded" />
            )}

            <input {...register('desconto')} type="number" placeholder="Desconto (R$)" className="w-full border p-2 rounded" />

            <button
              type="submit"
              disabled={carregando || itens.length === 0}
              className="w-full bg-blue-600 text-white py-3 rounded disabled:opacity-50"
            >
              {carregando ? 'Processando...' : 'Finalizar Venda'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
```

---

## 3.3 Exemplo 3: Login e Autenticação com JWT

### Antes — WinForms (`frmLogin` + `Session.CurrentUser`)

```csharp
// WinForms: estado global estático, sessão em memória do processo local
Session.CurrentUser = usuario;
```

### Depois — Backend (JWT + Refresh Token)

```csharp
// backend/src/Api/Controllers/AuthController.cs
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
{
    var usuario = await _usuarioService.AutenticarAsync(req.Usuario, req.Senha, ct);
    if (usuario == null)
        return Unauthorized(new { tipo = "credenciais-invalidas", mensagem = "Credenciais inválidas." });

    if (!usuario.Ativo)
        return Unauthorized(new { tipo = "usuario-inativo", mensagem = "Usuário inativo." });

    var accessToken = _tokenService.GenerateAccessToken(usuario);
    var refreshToken = _tokenService.GenerateRefreshToken();
    await _tokenService.SalvarRefreshToken(usuario.Id, refreshToken, ct);

    return Ok(new LoginResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        Usuario = new UsuarioDto
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Permissao = usuario.Permissao.ToString(),
            Ativo = usuario.Ativo
        }
    });
}
```

### Depois — Frontend (React + Axios interceptor com refresh token)

```tsx
// frontend/src/lib/api.ts
import axios from 'axios';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (err) => {
    const originalConfig = err.config;
    if (err.response?.status === 401 && !originalConfig._retry) {
      originalConfig._retry = true;
      try {
        const refresh = localStorage.getItem('refreshToken');
        const { data } = await api.post('/auth/refresh', { refreshToken: refresh });
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        return api(originalConfig);
      } catch {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);
```

```tsx
// frontend/src/hooks/useAuth.ts
export function useAuth() {
  const [usuario, setUsuario] = useState<UsuarioDto | null>(null);

  const login = async (usuario: string, senha: string) => {
    const { data } = await api.post('/auth/login', { usuario, senha });
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('usuarioId', data.usuario.id.toString());
    setUsuario(data.usuario);
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuarioId');
    setUsuario(null);
    window.location.href = '/login';
  };

  return { usuario, login, logout, isAuthenticated: !!usuario };
}
```

---

Etapa 3 concluída. A seguir, a **Etapa 4: Desafios Técnicos** (arquivos locais, JWT, impressão térmica, etc.).