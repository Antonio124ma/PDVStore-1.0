# PDVStore — Plano de Migração para Web
## Etapa 1: Arquitetura e Mapeamento de Componentes

> **Contexto real do projeto:** O `PDVStore` é um sistema de Ponto de Venda (PDV/Frente de Caixa) legado desenvolvido em **C# .NET 8 WinForms**, banco **SQL Server LocalDB** com **Entity Framework Core**, autenticação via **BCrypt**, logging com **Serilog** e integração de pagamento via **mock** (`PagamentoIntegrator`).  
>  
> **Stack alvo definida:** Backend **.NET 8 com ASP.NET Core Web API + TypeScript**, Frontend **React com Tailwind CSS**, Banco **PostgreSQL na nuvem (AWS RDS / Azure Database for PostgreSQL)**, Hospedagem **AWS (ECS/EKS + RDS + S3)**.  
>  
> *Mantemos C# no backend para aproveitar o domínio existente (EF Core, Services) e minimizar riscos de reescrita de lógica de negócio.*

---

## 1.1 Visão Geral da Arquitetura Alvo

```
CLIENTE (Navegador / PWA)  ->  HTTPS/JSON (REST)  ->  GATEWAY/ALB  ->  BACKEND ASP.NET Core Web API (.NET 8)
                                                        ->  PostgreSQL (RDS) | AWS S3 | AWS Secrets Manager
```

### Princípios arquiteturais
1. **Camada de serviços intacta:** A lógica de negócio (`VendaService`, `EstoqueService`, `CaixaService`, etc.) é portada quase que 1:1 para serviços da Web API. Apenas a camada de apresentação (WinForms) é substituída por React.
2. **Banco de dados mantido no modelo relacional:** Migramos de SQL Server LocalDB para PostgreSQL na nuvem. O modelo de domínio (entidades `Venda`, `Produto`, `Caixa`, `UsuarioCaixa`, etc.) é preservado via EF Core + Npgsql.
3. **Stateless:** A Web API não mantém sessão em memória. Autenticação é feita com **JWT (access token) + Refresh Token**.
4. **Impressão térmica sem DLL local:** Substituída por geração de PDF no backend servido via S3 ou endpoint direto, impresso pelo navegador.

---

## 1.2 Mapeamento de Telas (Forms → React SPA)

| Tela WinForms | Componente(s) React | Rota sugerida | Observações |
|---------------|---------------------|---------------|-------------|
| `frmSplash` | `SplashScreen` / `AppLoader` | `/` | Verificação de saúde da API (`GET /health`) |
| `frmLogin` | `LoginPage` | `/login` | POST `/api/auth/login`, armazena JWT + perfil |
| `frmMenuPrincipal` | `MainLayout` + `Sidebar` | `/dashboard` | Layout com navegação por papel (Admin/Operador/Estoquista) |
| `frmPDV` | `PDVPage` + `Carrinho`, `BuscaProduto`, `Pagamento`, `Recibo` | `/pdv` | Carrinho em estado local; venda via POST `/api/vendas` |
| `frmGerenciarProdutos` | `ProdutosPage` + `ProdutoForm` | `/produtos` | CRUD de produtos, busca por código de barras |
| `frmEstoque` | `EstoquePage` + `MovimentacaoEstoque` | `/estoque` | Entrada/saída manual, histórico de movimentações |
| `frmClientes` | `ClientesPage` + `ClienteForm` | `/clientes` | CRUD clientes, limite de crédito, saldo devedor |
| `frmFornecedores` | `FornecedoresPage` + `FornecedorForm` | `/fornecedores` | CRUD fornecedores |
| `frmCompras` | `ComprasPage` + `CompraForm` | `/compras` | Registro de compras com entrada automática de estoque |
| `frmCaixa` | `CaixaPage` + `AberturaCaixa`, `FechamentoCaixa` | `/caixa` | Abrir/fechar caixa, sangria |
| `frmDashboard` | `DashboardPage` + gráficos | `/dashboard` | Relatórios: itens mais vendidos, pagamentos por forma, estoque mínimo |
| `frmGerenciarUsuarios` | `UsuariosPage` + `UsuarioForm` | `/usuarios` | CRUD usuários, papéis, ativo/inativo (somente Admin) |
| `frmSobre` | `SobrePage` | `/sobre` | Versão, licença |

### Mapeamento de controles WinForms → Web

| Controle WinForms | Equivalente Web (React + Tailwind) | Comportamento |
|-------------------|------------------------------------|---------------|
| `TextBox` | `<input type="text" className="border p-2 rounded">` | `react-hook-form` + `zod` |
| `MaskedTextBox` (CPF, CEP, telefone) | `react-imask` / `react-text-mask` | Máscara frontend, validação backend |
| `ComboBox` / `DropDownList` | `<select>` ou `react-select` | Options carregadas via API |
| `DataGridView` | `tanstack-table` + shadcn/ui DataTable | Paginação, ordenação, filtragem backend |
| `Button` | `<button className="bg-blue-600 text-white px-4 py-2 rounded">` | `onClick` → mutação via React Query |
| `Label` | `<span className="text-sm text-gray-600">` | Exibição de dados (total, usuário logado) |
| `PrintDocument` (recibo térmico) | Geração de PDF + `window.print()` | Veja seção 4.3 |
| `MessageBox.Show` | `Toast` (sonner) + `AlertDialog` | Notificações não bloqueantes |
| `Timer` / `BackgroundWorker` | `setInterval` / `useEffect` + polling ou WebSockets | Atualização de status do caixa, estoque |

### Fluxo de estado (Exemplo: PDV)

No WinForms, o estado do carrinho vive no `PDVViewModel` (memória do formulário). No React, vive em estado local (Zustand/React Query):

```tsx
// app/src/features/pdv/store.ts
interface CarrinhoItem {
  produtoId: number;
  nome: string;
  codigoBarras?: string;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

interface PDVState {
  itens: CarrinhoItem[];
  desconto: number;
  formaPagamento: 'Dinheiro' | 'PIX' | 'CartaoCredito' | 'CartaoDebito' | 'Fiado';
  valorRecebido?: number;
  clienteId?: number;
  total: number; // computado
}
```

---

## 1.3 Mapeamento de Camadas (Camada por camada)

| Camada WinForms | Camada Web Alvo | Tecnologia |
|-----------------|-----------------|------------|
| Apresentação (`Forms/`) | SPA React | React 18 + Tailwind + Vite |
| ViewModels (`ViewModels/`) | DTOs + React state / React Query | TypeScript interfaces |
| Serviços de negócio (`Services/`) | Controllers + Services Web API | ASP.NET Core 8 |
| Acesso a dados (`Data/PDVContext`) | EF Core + Npgsql | PostgreSQL (RDS) |
| Utilitários (`Helpers/`) | Middleware + Utilitários .NET | Serilog, Options pattern |
| Integrações (`Integrations/`) | Serviços de pagamento reais | SDK PagSeguro/Mercado Pago |
| Log (`logs/*.log`) | Serilog → AWS CloudWatch | Serilog.AspNetCore + CloudWatch sink |

### Exemplo de mapeamento de entidade (Venda)

WinForms (C#):
```csharp
public class Venda : IHasId {
    public int Id { get; set; }
    public int UsuarioCaixaId { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public decimal ValorTotal { get; set; }
    public string FormaPagamento { get; set; } = "Dinheiro";
    public int? ClienteId { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}
```

Web (TypeScript DTO):
```typescript
// app/src/types/venda.ts
export type FormaPagamento = 'Dinheiro' | 'PIX' | 'CartaoCredito' | 'CartaoDebito' | 'Fiado';

export interface ItemVendaDto {
  produtoId: number;
  quantidade: number;
  precoUnitario: number;
}

export interface VendaCreateDto {
  usuarioCaixaId: number;
  formaPagamento: FormaPagamento;
  desconto: number;
  valorTotal: number;
  clienteId?: number;
  itens: ItemVendaDto[];
}

export interface VendaDto extends VendaCreateDto {
  id: number;
  dataVenda: string; // ISO 8601
  status: 'Concluida' | 'Cancelada' | 'Pendente';
  pixTxId?: string;
  caixaId: number;
  itens: ItemVendaDto[] & { id: number; nomeProduto?: string };
}
```

---

## 1.4 Estrutura de Pastas Proposta (Monorepo)

```
/workspace/pdvstore-web/
├── backend/                 # ASP.NET Core Web API (.NET 8)
│   ├── src/
│   │   ├── Api/             # Program.cs, appsettings.json, Controllers
│   │   ├── Application/     # Services de negócio, DTOs, Interfaces
│   │   ├── Infrastructure/  # EF Core DbContext, Migrations, Repositories, S3
│   │   └── Domain/          # Entidades, Value Objects, Enums
│   ├── tests/
│   ├── pdvstore-web.sln
│   └── Dockerfile
├── frontend/                # React SPA
│   ├── src/
│   │   ├── api/             # Axios instances + generated clients
│   │   ├── components/      # Componentes compartilhados (ui/, layout/)
│   │   ├── features/        # pdv/, produtos/, estoque/, caixa/, usuarios/, auth/, dashboard/
│   │   ├── pages/           # Páginas de rota
│   │   ├── hooks/           # useAuth, useApi
│   │   ├── store/           # Zustand slices globais (auth, user)
│   │   ├── types/           # TypeScript DTOs
│   │   ├── lib/             # utils (date-fns, mask, validators)
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   └── Dockerfile
├── shared/                  # Contratos compartilhados (OpenAPI spec)
│   └── openapi.yaml
├── infra/                   # Terraform / AWS CDK / Docker Compose
├── docs/                    # Esta documentação
└── docker-compose.yml
```

---

## 1.5 Diagrama de Sequência: Registro de Venda (PDV)

```
Usuário      Navegador (React)      API (.NET 8)          PostgreSQL (RDS)
------       -----------------      ------------          -----------------
 |                    |                      |                         |
 | 1. Abre PDV        |                      |                         |
 |--------------------|--------------------->| GET /api/caixa/aberto   |
 |                    |                      |-------------------------->|
 |                    |                      |<--------------------------|
 | 2. Busca produto   |                      |                         |
 |    (código de barras)|------------------->| GET /api/produtos/busca?q=|
 |                    |                      |-------------------------->|
 |                    |                      |<--------------------------| (200 Produto)
 | 3. Adiciona item   |                      |                         |
 |    ao carrinho     | (estado local)        |                         |
 |                    |                      |                         |
 | 4. Finaliza venda  |                      |                         |
 |--------------------|--------------------->| POST /api/vendas        |
 |                    |                      |-------------------------->| INSERT (transação)
 |                    |                      |<--------------------------| COMMIT
 |                    |                      |<--------------------------| (201 Venda)
 | 5. Emite recibo    |                      |                         |
 |    (PDF)           |<---------------------| GET /api/vendas/{id}/recibo|
 |                    |                      |-------------------------->|
 |                    |                      |<--------------------------| (PDF bytes)
 | 6. Imprime         | window.print()        |                         |
```

---

Etapa 1 concluída. A seguir, a **Etapa 2: Modelagem da API** com endpoints RESTful detalhados.