# PeruShopHub — Arquitetura Tecnica

> Documento de referencia para decisoes arquiteturais e modelo de dados.

---

## Stack

| Camada | Tecnologia | Versao |
|--------|-----------|--------|
| Backend | C# / ASP.NET Core Web API | .NET 9 |
| ORM | Entity Framework Core | 9 |
| Banco de dados | PostgreSQL | 16 |
| Cache / Fila | Redis | 7+ |
| Real-time | SignalR (Redis backplane) | - |
| Frontend | Angular (standalone components, signals) | 17+ |
| UI Components | Custom (CSS custom properties) | - |
| Charts | Chart.js (ng2-charts) | - |
| Upload | Local disk (IFileStorageService) | - |
| Background Jobs | .NET BackgroundService | - |
| Containers | Docker + Docker Compose | - |
| Reverse Proxy | Nginx | - |

---

## Monolito Modular

Microservices sao overengineering para equipe pequena. Separacao clara de responsabilidades:

```
src/
├── PeruShopHub.Core/            # Dominio: entidades, interfaces, value objects
│   ├── Entities/ (21 entidades)
│   ├── Interfaces/ (7 core interfaces)
│   └── ValueObjects/
├── PeruShopHub.Infrastructure/  # Persistencia, cache, notificacoes, file storage
│   ├── Persistence/ (AppDbContext, configs)
│   ├── Migrations/ (8 migrations)
│   ├── Cache/ (RedisCacheService)
│   ├── Notifications/ (SignalRNotificationDispatcher)
│   └── Services/ (LocalFileStorageService)
├── PeruShopHub.Application/     # Casos de uso, DTOs, services
│   ├── Services/ (11 service pairs: interface + impl)
│   ├── DTOs/ (19 DTO groups)
│   ├── Exceptions/ (NotFoundException, ValidationException, ConflictException)
│   └── DependencyInjection.cs
├── PeruShopHub.API/             # Controllers (16), middleware, hubs, filters
│   ├── Controllers/
│   ├── Middleware/ (TenantMiddleware)
│   ├── Hubs/ (NotificationHub)
│   └── Filters/ (GlobalExceptionFilter)
├── PeruShopHub.Worker/          # Background jobs
└── PeruShopHub.Web/             # Frontend Angular
```

---

## Adapter Pattern (Multi-Marketplace)

Cada marketplace implementa `IMarketplaceAdapter`. O core nunca sabe qual marketplace esta usando:

```csharp
public interface IMarketplaceAdapter
{
    string MarketplaceId { get; }  // "mercadolivre", "amazon", "shopee"
    Task<TokenResult> RefreshTokenAsync(string refreshToken);
    Task<MarketplaceProduct> GetProductAsync(string externalId);
    Task UpdateStockAsync(string externalId, int quantity);
    Task<IReadOnlyList<MarketplaceOrder>> GetOrdersAsync(DateRange period);
    Task<OrderDetails> GetOrderDetailsAsync(string orderId);
    Task<IReadOnlyList<MarketplaceFee>> GetOrderFeesAsync(string orderId);
    Task<WebhookEvent> ParseWebhookAsync(HttpRequest request);
    // ...
}

// Registro via DI keyed services:
services.AddKeyedScoped<IMarketplaceAdapter, MercadoLivreAdapter>("mercadolivre");
services.AddKeyedScoped<IMarketplaceAdapter, AmazonAdapter>("amazon");
```

---

## Modelo de Dados

### Entidades (21 total)

**Multi-Tenancy & Auth:**
- `Tenant` — registro da loja (slug, IsActive)
- `TenantUser` — tabela de juncao com role (Owner, Admin, Manager, Viewer)
- `SystemUser` — email, password hash (BCrypt), IsSuperAdmin, refresh tokens

**Produtos & Catalogo:**
- `Product` (ITenantScoped) — SKU, preco, custos, dimensoes, status, Version (optimistic locking)
- `ProductVariant` — SKU variante, cor, tamanho, estoque, custo
- `Category` (ITenantScoped) — hierarquica com ParentId, SKU prefix, Version
- `VariationField` — campos como "cor", "tamanho" por categoria

**Vendas & Pedidos:**
- `Order` (ITenantScoped) — external order ID, comprador, itens, custos, fulfillment status
- `OrderItem` — vincula produtos/variantes a pedidos
- `OrderCost` — decomposicao granular de custos (comissao, taxas, frete, impostos, etc.)

**Estoque:**
- `ProductCostHistory` — rastreia mudancas de custo ao longo do tempo
- `StockMovement` — trilha de auditoria de ajustes de estoque

**Compras:**
- `PurchaseOrder` (ITenantScoped) — pedidos a fornecedores, itens, custos, status, Version
- `PurchaseOrderItem` — itens do pedido de compra
- `PurchaseOrderCost` — alocacao de custos (by_value/by_quantity)

**Utilitarios:**
- `Customer` (ITenantScoped) — registros de compradores
- `Supply` (ITenantScoped) — consumiveis/materiais, Version
- `MarketplaceConnection` — tokens OAuth (criptografados em repouso)
- `Notification` (ITenantScoped) — notificacoes em tempo real
- `FileUpload` — metadados de arquivos
- `CommissionRule` — regras de comissao por categoria/margem

### Precisao Financeira

**Regra inquebravel**: nunca usar `float`/`double` para dinheiro.

- C#: `decimal`
- PostgreSQL: `NUMERIC(18,4)`
- Frontend: Angular `currency` pipe com `'BRL'`, fonte monospace

### Decomposicao de Custos por Venda (OrderCost)

| Categoria | Descricao |
|-----------|-----------|
| `marketplace_commission` | Comissao da plataforma (varia por categoria) |
| `fixed_fee` | Taxa fixa por venda |
| `shipping_seller` | Custo de frete do vendedor |
| `payment_fee` | Taxa de processamento de pagamento |
| `fulfillment_fee` | Taxa de fulfillment (picking, packing) |
| `storage_daily` | Custo de armazenagem diario |
| `product_cost` | Custo de aquisicao do produto |
| `packaging` | Custo de embalagem |
| `advertising` | Custo de ads atribuido |
| `tax` | Impostos (ICMS, PIS/COFINS, etc.) |

---

## Fluxo de Webhooks

```
[Webhook ML chega] → [API valida + enfileira Redis] → [Worker processa]
                      (responde 200 em < 500ms)              │
                                          ┌───────────────────┤
                                          ▼                   ▼
                                [Atualiza pedido]   [Notifica frontend
                                [Registra custos]    via SignalR]
                                [Atualiza estoque
                                 em todos os
                                 marketplaces]
```

---

## Inventario

Estoque master como fonte de verdade, com alocacao por marketplace:

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Mercado      │     │   ESTOQUE    │     │   Amazon     │
│  Livre        │◄────┤   MASTER     ├────►│              │
│  (alocado:50) │     │  (total:100) │     │ (alocado:30) │
└──────────────┘     └──────┬───────┘     └──────────────┘
                            │
                            ▼
                     ┌──────────────┐
                     │   Shopee     │
                     │ (alocado:20) │
                     └──────────────┘
```

- Webhook de venda → decrementa master → recalcula alocacoes → atualiza todos
- Lock otimista (coluna `version`) para evitar race conditions
- Reconciliacao periodica: worker compara estoque real vs esperado

---

## Gestao de Tokens OAuth

```sql
-- marketplace_connections
access_token    TEXT NOT NULL       -- criptografado AES-256
refresh_token   TEXT NOT NULL       -- criptografado AES-256
token_expires_at TIMESTAMPTZ NOT NULL
refresh_error_count INT DEFAULT 0
```

- Worker renova tokens que expiram nos proximos 30 min
- Circuit breaker: 3 falhas seguidas → marca conexao como inativa → notifica usuario
- Chave de criptografia em variavel de ambiente

---

## API Design Conventions

- **Thin controllers**: zero EF queries, toda logica em services
- **Typed exceptions**: NotFoundException (404), ValidationException (400), ConflictException (409)
- **Validation collection**: todas as errors coletadas antes de lancar (nao fail-on-first)
- **DTOs em todas as fronteiras**: services recebem/retornam DTOs, nunca entidades
- **Semantic query params**: sem boolean flags, presenca/ausencia de parametro
- **Nested resources**: `/api/categories/{id}/variation-fields`
- **Paginacao padrao**: page=1, pageSize=20
- **Responses**:
  ```json
  // Success: { "id": "guid", "name": "..." }
  // Paginated: { "items": [...], "totalCount": 100, "page": 1, "pageSize": 20 }
  // 400: { "errors": { "name": ["required"], "sku": ["already exists"] } }
  // 404: { "error": "Product not found" }
  // 409: { "error": "Este registro foi modificado por outro usuário." }
  ```

---

## Requisitos Nao-Funcionais

| Requisito | Especificacao |
|-----------|---------------|
| Webhooks | Responder em < 500ms (exigencia ML) |
| Consistencia | Optimistic locking em entidades-chave |
| Seguranca | Tokens OAuth criptografados AES-256 em repouso |
| Performance | Rate limiter: 18.000 req/hora para API do ML |
| Extensibilidade | Novo marketplace = novo Adapter, zero mudanca no core |

---

## Riscos e Mitigacoes

| Risco | Mitigacao |
|-------|-----------|
| API ML fora do ar | Fila de retry com dead letter queue |
| Rate limit excedido | Rate limiter no client, priorizar webhooks |
| Token OAuth expira | Worker proativo + alerta + circuit breaker |
| Overselling | Lock otimista + sync em tempo real |
| Mudanca de taxas ML | Tabela versionada + alertas de divergencia |
| ML nao tem sandbox | Usuarios de teste em producao (max 10) |
