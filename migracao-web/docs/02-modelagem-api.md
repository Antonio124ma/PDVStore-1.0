# PDVStore — Plano de Migração para Web
## Etapa 2: Modelagem da API RESTful

> **Princípio:** Todos os endpoints seguem REST, retornam JSON, usam códigos HTTP padrão e exigem autenticação JWT (exceto `POST /api/auth/login` e `GET /api/health`).  
> **Versionamento:** `/api/v1/`  
> **Serialização:** camelCase no JSON (System.Text.Json padrão do .NET 8).  
> **Paginação:** Query params `page` (default 1) e `pageSize` (default 20, max 100). Resposta envolve `{ items, total, page, pageSize }`.

---

## 2.1 Autenticação e Segurança

| Endpoint | Método | Descrição | Body de request | Resposta de sucesso |
|----------|--------|-----------|-----------------|---------------------|
| `/api/v1/auth/login` | POST | Autentica usuário e retorna access + refresh token | `{ usuario, senha }` | `{ accessToken, refreshToken, usuario }` |
| `/api/v1/auth/refresh` | POST | Renova o access token usando refresh token | `{ refreshToken }` | `{ accessToken, refreshToken }` |
| `/api/v1/auth/logout` | POST | Invalida o refresh token (revoga) | `{ refreshToken }` | `204 No Content` |
| `/api/v1/auth/me` | GET | Retorna dados do usuário autenticado | — | `UsuarioDto` |
| `/api/v1/auth/alterar-senha` | POST | Altera senha do usuário logado | `{ senhaAtual, novaSenha }` | `204 No Content` |

**Middleware de autorização:** `[Authorize]` com políticas por papel (`Administrador`, `Operador`, `Estoquista`).

### Exemplo: payload de login

Request:
```json
POST /api/v1/auth/login
{
  "usuario": "admin",
  "senha": "admin123"
}
```

Response (200):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "d8f3a2b1-...",
  "usuario": {
    "id": 1,
    "nome": "Admin",
    "permissao": "Administrador",
    "ativo": true
  }
}
```

---

## 2.2 Produtos (estoque/catálogo)

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/produtos` | GET | Lista produtos (paginado, filtro `q`, `ativo`) | Todos autenticados |
| `/api/v1/produtos/{id}` | GET | Obtém produto por ID | Todos autenticados |
| `/api/v1/produtos/busca` | GET | Busca por nome ou código de barras (`?q=`) | Todos autenticados |
| `/api/v1/produtos` | POST | Cadastra novo produto | Administrador, Estoquista |
| `/api/v1/produtos/{id}` | PUT | Atualiza produto existente | Administrador, Estoquista |
| `/api/v1/produtos/{id}` | DELETE | Desativa (soft delete) produto | Administrador |
| `/api/v1/produtos/{id}/estoque` | PATCH | Ajuste manual de estoque (entrada/saída) | Administrador, Estoquista |

### Exemplo: resposta listagem paginada

```json
GET /api/v1/produtos?page=1&pageSize=10&q=mouse
{
  "items": [
    {
      "id": 42,
      "nome": "Mouse Sem Fio",
      "codigoBarras": "7891234567890",
      "preco": 89.90,
      "precoCusto": 45.00,
      "estoque": 15,
      "estoqueMinimo": 5,
      "categoria": "Informática",
      "descricao": "Mouse ergonômico sem fio",
      "ativo": true
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 10
}
```

---

## 2.3 Vendas (PDV)

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/vendas` | GET | Lista vendas (paginado, filtro `dataInicio`, `dataFim`, `formaPagamento`, `clienteId`) | Administrador, Operador |
| `/api/v1/vendas/{id}` | GET | Obtém venda por ID com itens | Administrador, Operador |
| `/api/v1/vendas` | POST | Registra uma nova venda (transação completa) | Operador, Administrador |
| `/api/v1/vendas/{id}/cancelar` | POST | Cancela uma venda (estorno de estoque) | Administrador |
| `/api/v1/vendas/{id}/recibo` | GET | Gera/retorna recibo da venda em PDF | Operador, Administrador |
| `/api/v1/vendas/relatorio-forma-pagamento` | GET | Relatório de vendas por forma de pagamento (período) | Administrador |

### Exemplo: registrar venda (POST)

Request:
```json
POST /api/v1/vendas
{
  "usuarioCaixaId": 3,
  "formaPagamento": "Dinheiro",
  "desconto": 5.00,
  "valorTotal": 95.00,
  "clienteId": null,
  "itens": [
    { "produtoId": 10, "quantidade": 2, "precoUnitario": 50.00 }
  ]
}
```

Response (201):
```json
{
  "id": 128,
  "usuarioCaixaId": 3,
  "dataVenda": "2026-10-19T14:30:00Z",
  "valorTotal": 95.00,
  "desconto": 5.00,
  "formaPagamento": "Dinheiro",
  "status": "Concluida",
  "caixaId": 7,
  "clienteId": null,
  "pixTxId": null,
  "itens": [
    { "id": 255, "vendaId": 128, "produtoId": 10, "quantidade": 2, "precoUnitario": 50.00, "nomeProduto": "Mouse Sem Fio" }
  ]
}
```

---

## 2.4 Caixa

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/caixa/aberto` | GET | Verifica se existe caixa aberto no momento | Operador, Administrador |
| `/api/v1/caixa` | POST | Abre um novo caixa (`valorInicial`, `usuarioCaixaId`) | Operador, Administrador |
| `/api/v1/caixa/{id}/fechar` | POST | Fecha o caixa (calcula valor final) | Operador, Administrador |
| `/api/v1/caixa/{id}/sangria` | POST | Registra sangria (retirada de dinheiro) | Operador, Administrador |
| `/api/v1/caixa` | GET | Lista todos os caixas (histórico) | Administrador |

## 2.5 Clientes (venda fiada)

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/clientes` | GET | Lista clientes (paginado) | Todos autenticados |
| `/api/v1/clientes/{id}` | GET | Obtém cliente por ID | Todos autenticados |
| `/api/v1/clientes` | POST | Cadastra novo cliente | Administrador |
| `/api/v1/clientes/{id}` | PUT | Atualiza cliente | Administrador |
| `/api/v1/clientes/{id}/ativo` | PATCH | Ativa/desativa cliente | Administrador |
| `/api/v1/clientes/{id}/receber-debito` | POST | Registra pagamento parcial/total do débito | Administrador, Operador |

## 2.6 Fornecedores e Compras

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/fornecedores` | GET/POST/PUT/DELETE | CRUD de fornecedores | Administrador |
| `/api/v1/compras` | GET | Lista compras (paginado) | Administrador |
| `/api/v1/compras/{id}` | GET | Obtém compra com itens | Administrador |
| `/api/v1/compras` | POST | Registra compra (entrada de estoque automática) | Administrador |
| `/api/v1/compras/{id}/cancelar` | POST | Cancela compra (devolve estoque) | Administrador |

## 2.7 Usuários e Permissões

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/usuarios` | GET | Lista usuários (paginado, `apenasAtivos`) | Administrador |
| `/api/v1/usuarios/{id}` | GET | Obtém usuário por ID | Administrador |
| `/api/v1/usuarios` | POST | Cadastra novo usuário (hash de senha no backend) | Administrador |
| `/api/v1/usuarios/{id}` | PUT | Atualiza usuário (não altera senha aqui) | Administrador |
| `/api/v1/usuarios/{id}/ativo` | PATCH | Ativa/desativa usuário (soft delete) | Administrador |
| `/api/v1/usuarios/administradores` | GET | Lista apenas administradores | Administrador |

---

## 2.8 Dashboard e Relatórios

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/v1/dashboard/resumo` | GET | Total período, qtd vendas, pagamentos por forma | Administrador, Operador |
| `/api/v1/dashboard/itens-mais-vendidos` | GET | Ranking de itens mais vendidos (período) | Administrador |
| `/api/v1/dashboard/estoque-minimo` | GET | Produtos abaixo do estoque mínimo | Estoquista, Administrador |
| `/api/v1/dashboard/movimentacoes-por-dia` | GET | Entradas x saídas de estoque por dia | Administrador |
| `/api/v1/dashboard/vendas-por-dia` | GET | Vendas agrupadas por dia (período) | Administrador |
| `/api/v1/relatorios/exportar` | GET | Exporta relatório em PDF/Excel (query `tipo`, `inicio`, `fim`) | Administrador |

## 2.9 Health Check

| Endpoint | Método | Descrição | Acesso |
|----------|--------|-----------|--------|
| `/api/health` | GET | Health check da API (banco, dependências) | Público |
| `/api/health/ready` | GET | Readiness probe (Kubernetes) | Público |

## 2.10 Tratamento de Erros (padrão)

Todas as respostas de erro seguem o contrato RFC 7807 (Problem Details):

```json
{
  "type": "https://pdvstore.erros/validacao-negocio",
  "title": "Erro de validação de negócio",
  "status": 400,
  "detail": "Nenhum caixa aberto. Abra o caixa antes de registrar vendas.",
  "instance": "/api/v1/vendas"
}
```

Códigos HTTP usados:
- `200 OK` — sucesso com conteúdo
- `201 Created` — recurso criado (venda, produto, cliente, etc.)
- `204 No Content` — sucesso sem conteúdo (logout, alterar senha)
- `400 Bad Request` — erro de validação / regra de negócio
- `401 Unauthorized` — não autenticado
- `403 Forbidden` — sem permissão (papel insuficiente)
- `404 Not Found` — recurso não encontrado
- `409 Conflict` — conflito (ex: caixa já aberto)
- `500 Internal Server Error` — erro inesperado

---

Etapa 2 concluída. A seguir, a **Etapa 3: Lógica de Migração de Código** com exemplos práticos de conversão C# WinForms → .NET 8 Web API + React.