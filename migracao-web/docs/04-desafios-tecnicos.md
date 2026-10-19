# PDVStore — Plano de Migração para Web
## Etapa 4: Desafios Técnicos

> **Objetivo:** Explicar como resolver os problemas específicos da web que não existiam no ambiente desktop (acesso a arquivos locais, autenticação/sessão sem estado, impressão térmica sem DLL local, leitor de código de barras, etc.).

---

## 4.1 Substituindo o Acesso a Arquivos Locais

### Problema no desktop
O WinForms usava `System.IO.File`, `PrintDocument`, `Session.CurrentUser`, caminhos locais (`logs/pdvstore-*.log`, `FotoPath`, recibos salvos em disco). No navegador, **não há acesso direto ao sistema de arquivos do cliente** por segurança (sandbox).

### Solução na web

| Recurso desktop | Substituto web | Como implementar |
|-----------------|---------------|------------------|
| Logs em arquivo local (`logs/*.log`) | Serilog → AWS CloudWatch / Azure Monitor | `Serilog.AspNetCore` + sink de nuvem |
| Fotos de usuários (`FotoPath` local) | AWS S3 + URL pública/presigned | Upload via `POST /api/usuarios/{id}/foto`, arquivo armazenado no S3 |
| Recibos salvos em disco (`SalvarRecibo`) | Geração de PDF no backend + download pelo navegador | `GET /api/vendas/{id}/recibo` retorna `application/pdf`; frontend faz `window.open(url)` ou `<a download>` |
| Arquivos de importação/exportação (Excel via EPPlus) | Backend gera Excel/PDF e disponibiliza para download | Endpoint retorna arquivo como blob; frontend dispara download |

### Exemplo: upload de foto de usuário para S3

Backend (.NET):
```csharp
[HttpPost("{id}/foto")]
public async Task<ActionResult> UploadFoto(int id, IFormFile arquivo, CancellationToken ct)
{
    if (arquivo == null || arquivo.Length == 0)
        return BadRequest(new { mensagem = "Arquivo obrigatório." });

    var url = await _s3Service.UploadAsync($"usuarios/{id}/foto", arquivo.OpenReadStream(), arquivo.ContentType, ct);
    await _usuarioService.AtualizarFotoPathAsync(id, url, ct);
    return Ok(new { fotoUrl = url });
}
```

Frontend (React):
```tsx
const handleUpload = async (arquivo: File) => {
  const formData = new FormData();
  formData.append('arquivo', arquivo);
  const { data } = await api.post(`/usuarios/${usuarioId}/foto`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  setFotoUrl(data.fotoUrl);
};
```

---

## 4.2 Autenticação e Sessão com JWT

### Problema no desktop
O WinForms usava `Session.CurrentUser` (variável estática em memória) e autenticação única por processo local. Não havia expiração de token nem necessidade de renovar sessão.

### Solução na web

**Fluxo de tokens:**
1. Usuário faz `POST /api/v1/auth/login` → recebe `accessToken` (curta duração, ex: 15 min) e `refreshToken` (longa duração, ex: 7 dias).
2. O frontend armazena ambos em `localStorage` (ou cookies HttpOnly para maior segurança).
3. Toda requisição envia `Authorization: Bearer <accessToken>`.
4. Quando o `accessToken` expira (401), o interceptor do Axios usa o `refreshToken` para obter um novo par de tokens.
5. Se o `refreshToken` também expirar ou for revogado, o usuário é redirecionado ao login.

**Controle de acesso por papel (RBAC):**
No backend, políticas de autorização:
```csharp
[Authorize(Policy = "ApenasAdministrador")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeletarProduto(int id) { ... }

[Authorize(Policy = "OperadorOuAdministrador")]
[HttpPost]
public async Task<IActionResult> RegistrarVenda(...) { ... }
```

Configuração no `Program.cs`:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApenasAdministrador", p => p.RequireRole("Administrador"));
    options.AddPolicy("OperadorOuAdministrador", p => p.RequireRole("Operador", "Administrador"));
    options.AddPolicy("EstoquistaOuAdministrador", p => p.RequireRole("Estoquista", "Administrador"));
});
```

**Recomendação de segurança:** Armazenar tokens em **cookies HttpOnly, Secure, SameSite=Strict** em vez de `localStorage` para mitigar XSS. Se usar `localStorage`, nunca armazene dados sensíveis além do token.

---

## 4.3 Impressão Térmica sem DLL Local

### Problema no desktop
O WinForms usava `PrintDocument` + `PrintController` para enviar comandos ESC/POS diretamente à impressora térmica via USB, usando DLLs locais do fabricante. No navegador, **não há acesso direto à porta USB/serial da impressora** por padrão.

### Soluções (em ordem de recomendação)

#### Opção A: PDF gerado no backend + `window.print()` (mais simples, funciona em qualquer impressora)
O backend gera o recibo em PDF (iText/QuestPDF) e expõe via endpoint. O frontend abre o PDF e o usuário imprime na impressora térmica configurada no SO (via driver).

```csharp
// Backend
[HttpGet("{id}/recibo")]
public async Task<IActionResult> Recibo(int id, CancellationToken ct)
{
    var pdfBytes = await _reciboService.GerarPdfAsync(id, ct);
    return File(pdfBytes, "application/pdf", $"recibo-venda-{id}.pdf");
}
```

```tsx
// Frontend
const pdfUrl = `${API_URL}/vendas/${vendaId}/recibo`;
const janela = window.open(pdfUrl, '_blank');
janela?.focus();
// O usuário imprime via Ctrl+P no navegador, selecionando a impressora térmica
```

#### Opção B: Web USB / Web Bluetooth API (impressoras térmicas modernas)
Impressoras térmicas que suportam USB/Bluetooth podem ser acessadas diretamente pelo navegador via `navigator.usb.requestDevice()` ou `navigator.bluetooth.requestDevice()`. Comandos ESC/POS são enviados como bytes.

```tsx
// Exemplo conceitual (requer HTTPS e permissão do usuário)
async function imprimirEscPos(comandoBytes: Uint8Array) {
  const device = await navigator.usb.requestDevice({ filters: [{ vendorId: 0x0483 }] });
  await device.open();
  await device.selectConfiguration(1);
  await device.claimInterface(0);
  await device.transferOut(1, comandoBytes);
}
```
*Limitação: Nem todos os navegadores suportam (Chrome/Edge yes, Firefox/Safari limitado). Requer HTTPS.*

#### Opção C: Impressão via nuvem
Usar serviços de impressão em nuvem (ex: PrintNode, AWS IoT) que recebem o PDF por API e enviam à impressora cadastrada.

**Recomendação para PDVStore:** Começar com **Opção A (PDF + window.print)** para entrega rápida, e implementar **Opção B (Web USB)** como evolução para impressão direta sem intervenção do usuário.

---

## 4.4 Leitor de Código de Barras

### No desktop
O leitor era configurado como "teclado em wedge" (emula teclado) ou via DLL do fabricante. O formulário capturava `KeyDown` no `TextBox`.

### Na web
- **Leitor em wedge (teclado):** Funciona igual. O `input` captura os caracteres e o Enter dispara a busca (como no exemplo de `BuscaProduto` da Etapa 3).
- **Leitor USB/HID direto:** Requer Web HID API (`navigator.hid.requestDevice()`), disponível em Chrome/Edge.
- **Câmera como leitor:** Usar biblioteca como `@zxing/browser` ou `html5-qrcode` para ler códigos de barras/QR via câmera do dispositivo.

```tsx
// Exemplo com @zxing/browser
import { BrowserMultiFormatReader } from '@zxing/browser';

const reader = new BrowserMultiFormatReader();
const resultado = await reader.decodeFromInputVideoElement();
const codigo = resultado.getText(); // código de barras lido
```

---

## 4.5 Sincronização de Horário e Fuso Horário

### Problema
No desktop, `DateTime.UtcNow` era confiável porque o relógio do Windows local era usado. Na web, o cliente envia datas em horário local; o servidor deve padronizar em UTC.

### Solução
- Todas as datas no banco: **UTC**.
- API recebe e retorna ISO 8601 com fuso (ex: `2026-10-19T14:30:00Z`).
- Frontend converte para exibição local com `date-fns` ou `dayjs`:
```tsx
import { format } from 'date-fns';
const exibicao = format(new Date('2026-10-19T14:30:00Z'), 'dd/MM/yyyy HH:mm');
```

---

## 4.6 Resumo de Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Impressora térmica não imprime diretamente pelo navegador | Alto (PDV depende de recibo) | PDF + `window.print()` como fallback imediato; Web USB como evolução |
| Usuário fecha o navegador no meio de uma venda | Médio | Transação no backend garante integridade; carrinho é estado local, pode ser perdido. Solução: salvar carrinho rascunho no backend (`/api/carrinho/rascunho`) |
| Token JWT roubado via XSS | Alto | Usar cookies HttpOnly + Secure + SameSite; CSP rígido |
| Perda de conexão durante venda | Médio | Retry automático com axios-retry; toast de erro claro; idempotência nas vendas (header `Idempotency-Key`) |

---

### Arquivos entregues neste plano
- `/workspace/migracao-web/docs/01-arquitetura-e-mapeamento.md` — Etapa 1
- `/workspace/migracao-web/docs/02-modelagem-api.md` — Etapa 2
- `/workspace/migracao-web/docs/03-logica-migracao-codigo.md` — Etapa 3
- `/workspace/migracao-web/docs/04-desafios-tecnicos.md` — Etapa 4