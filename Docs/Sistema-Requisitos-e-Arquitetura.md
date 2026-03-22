# Sistema de Gestao Multi-Marketplace - Requisitos e Arquitetura

## Visao Geral

Sistema intermediario para gestao centralizada de vendas em marketplaces, com foco em **rastreabilidade completa de custos** e **lucratividade real por venda/SKU**. Inicio pelo Mercado Livre, com arquitetura extensivel para Amazon, Shopee e outros.

---

## 1. Analise de Mercado: Solucoes Existentes

### Principais ERPs/Hubs no Brasil

| Solucao | Preco/mes | Foco | Lucro Real/Venda | ML Full Detalhado |
|---------|-----------|------|-------------------|-------------------|
| Bling | R$ 50-130 | ERP + Marketplace | Parcial | Nao |
| Tiny (Olist) | R$ 55-170 | ERP + Marketplace | Parcial | Nao |
| Anymarket | R$ 300-500+ | Hub Marketplace | Nao | Nao |
| Plugg.to | R$ 200-300+ | Hub Marketplace | Nao | Nao |
| Omie | R$ 100-200+ | ERP Empresarial | Nao | Nao |
| ERPNext | Gratuito | ERP Generico (open-source) | Nao | Nao |
| Odoo CE | Gratuito | ERP Generico (open-source) | Nao | Nao |

### Lacunas do Mercado (Justificativa do projeto)

**Nenhuma solucao existente calcula o lucro liquido real por venda** considerando todos os custos:

1. **Comissao do marketplace** (varia por categoria, tipo de anuncio, reputacao)
2. **Taxa fixa por venda**
3. **Custo real do frete** (diferenca entre repasse e custo)
4. **Custos de Fulfillment** (armazenagem diaria, picking, packing)
5. **Custo de advertising** (Mercado Ads, Amazon Ads)
6. **Impostos** (Simples Nacional, ICMS-ST, etc.)
7. **Custo do produto + embalagem**
8. **Desconto de cupons absorvidos pelo seller**

### Outras funcionalidades ausentes no mercado

- Gestao detalhada de custos de Fulfillment por SKU
- Precificacao dinamica baseada em margem minima
- Conciliacao financeira automatizada (depositado vs esperado)
- Dashboard de saude do negocio com margem real (nao apenas faturamento)
- Integracao de dados de Advertising com dados de vendas para ROI real
- Alertas personalizaveis (margem abaixo de X%, estoque baixo, etc.)

---

## 2. Requisitos Funcionais

### 2.1 Gestao de Produtos e Estoque

- [ ] Cadastro unificado de produtos (SKU master)
- [ ] Mapeamento de SKU para cada marketplace (1 SKU → N listings)
- [ ] Estoque centralizado com alocacao por marketplace
- [ ] Sincronizacao de estoque em tempo real (webhook → atualiza todos os canais)
- [ ] Historico de movimentacoes de estoque
- [ ] Alertas de estoque baixo
- [ ] Gestao de variacoes (cor, tamanho)
- [ ] Upload e gestao de imagens por produto
- [ ] CRUD de anuncios via API (criar, editar, pausar, reativar)

### 2.2 Gestao de Vendas e Pedidos

- [ ] Recebimento automatico de pedidos via webhook
- [ ] Painel unificado de pedidos (todos os marketplaces)
- [ ] Status de pedido em tempo real (pago, enviado, entregue, devolvido)
- [ ] Detalhes de envio e rastreamento
- [ ] Gestao de devolucoes e reclamacoes
- [ ] Historico completo por comprador

### 2.3 Rastreabilidade Financeira (Diferencial)

- [ ] Registro de custo de aquisicao por produto (com historico temporal)
- [ ] Decomposicao automatica de custos por venda:
  - Comissao do marketplace (via API)
  - Taxa fixa
  - Custo de frete (seller + buyer)
  - Taxa de pagamento
  - Custo de fulfillment/armazenagem
  - Custo de advertising atribuido
  - Impostos
  - Custo do produto
  - Custo de embalagem
- [ ] Calculo de lucro liquido real por venda
- [ ] Margem de lucro por SKU, por marketplace, por periodo
- [ ] Conciliacao financeira (depositado vs esperado)
- [ ] Identificacao automatica de divergencias em comissoes
- [ ] Curva ABC com base em margem (nao apenas faturamento)
- [ ] Relatorios de lucratividade exportaveis (PDF/Excel)

### 2.4 Gestao de Fulfillment (ML Full)

- [ ] Consulta de estoque no CD via API
- [ ] Historico de operacoes de estoque (entrada, venda, retirada, perda)
- [ ] Custo de armazenagem acumulado por SKU
- [ ] Alerta de produtos com armazenagem cara vs. baixo giro
- [ ] Simulador: Full vs. envio proprio (custo comparativo por SKU)

### 2.5 Comunicacao (Pre e Pos-Venda)

- [ ] Painel unificado de perguntas de compradores
- [ ] Resposta a perguntas via sistema
- [ ] Mensagens pos-venda
- [ ] Templates de resposta
- [ ] Alertas de perguntas nao respondidas

### 2.6 Marketing e Advertising

- [ ] Integracao com Mercado Ads (campanhas, metricas)
- [ ] ACOS por produto (custo de ads / receita)
- [ ] ROI real por campanha (considerando margem liquida, nao apenas receita)
- [ ] Gestao de promocoes e descontos

### 2.7 Precificacao

- [ ] Calculadora de preco baseada em margem desejada
- [ ] Consideracao automatica de todos os custos ao sugerir preco
- [ ] Alertas de margem abaixo do minimo configurado
- [ ] Simulador de cenarios (e se a comissao mudar? e se o frete subir?)

### 2.8 Dashboard e Relatorios

- [ ] Visao geral: faturamento, lucro, vendas, ticket medio
- [ ] Comparativo entre marketplaces
- [ ] Produtos mais lucrativos vs. menos lucrativos
- [ ] Tendencias (vendas, margem, estoque) ao longo do tempo
- [ ] Alertas configuráveis em tempo real

---

## 3. Requisitos Nao-Funcionais

- **Disponibilidade**: Webhooks devem responder em < 500ms (exigencia ML)
- **Consistencia**: Operacoes de estoque com lock otimista (sem overselling)
- **Seguranca**: Tokens OAuth criptografados em repouso (AES-256)
- **Auditoria**: Log de todas as operacoes financeiras e de estoque
- **Performance**: Suportar ate 18.000 req/hora para API do ML (rate limit)
- **Extensibilidade**: Adicionar novo marketplace sem alterar codigo core

---

## 4. Arquitetura Tecnica

### 4.1 Stack Recomendado

| Camada | Tecnologia | Justificativa |
|--------|-----------|---------------|
| **Backend** | C# / .NET 8+ | Tipo `decimal` nativo para calculos financeiros, Background Services de primeira classe, ecossistema maduro |
| **Banco** | PostgreSQL 16 | NUMERIC(18,4) para precisao financeira, JSONB para respostas brutas de API, particionamento nativo |
| **Cache/Fila** | Redis (fase 1) + RabbitMQ (fase 2) | Cache de tokens, rate limiting, filas de webhooks |
| **Frontend** | React 19 + Tailwind + Shadcn/UI | Maior ecossistema de componentes de dashboard, TanStack Query para sync de estado |
| **Real-time** | SignalR | Notificacoes em tempo real no dashboard |
| **Relatorios** | QuestPDF / ClosedXML | Geracao server-side de PDF e Excel |

**Alternativa de backend**: Node.js/NestJS ou Python/FastAPI se houver mais experiencia nessas stacks. O padrao arquitetural importa mais que a linguagem.

### 4.2 Arquitetura: Monolito Modular

Microservices e overengineering para dev solo/equipe pequena. Monolito modular com separacao clara:

```
src/
├── Marketplace.Core/                  # Dominio compartilhado
│   ├── Entities/
│   │   ├── Product.cs
│   │   ├── Order.cs
│   │   ├── Sale.cs
│   │   ├── SaleCost.cs
│   │   ├── FinancialEntry.cs
│   │   └── Inventory.cs
│   ├── Interfaces/
│   │   ├── IMarketplaceAdapter.cs     # Contrato para cada marketplace
│   │   ├── IInventoryService.cs
│   │   └── IFinancialService.cs
│   └── ValueObjects/
│       ├── Money.cs                   # Decimal com moeda
│       └── Sku.cs
│
├── Marketplace.Infrastructure/
│   ├── Adapters/
│   │   ├── MercadoLivre/
│   │   │   ├── MercadoLivreAdapter.cs
│   │   │   ├── MercadoLivreAuthHandler.cs
│   │   │   ├── MercadoLivreMapper.cs
│   │   │   └── DTOs/
│   │   ├── Amazon/
│   │   │   └── AmazonAdapter.cs
│   │   └── Shopee/
│   │       └── ShopeeAdapter.cs
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   └── Repositories/
│   └── Queue/
│       └── WebhookProcessor.cs
│
├── Marketplace.Application/          # Casos de uso
│   ├── Orders/
│   ├── Inventory/
│   ├── Financial/
│   ├── Messaging/
│   └── Pricing/
│
├── Marketplace.API/                  # Controllers + Webhooks
│   ├── Controllers/
│   ├── Webhooks/
│   └── Hubs/                         # SignalR
│
└── Marketplace.Worker/               # Background jobs
    ├── InventorySyncWorker.cs
    ├── TokenRefreshWorker.cs
    ├── FinancialReconciliationWorker.cs
    └── StorageCostCalculatorWorker.cs
```

### 4.3 Adapter Pattern (Peca-chave)

Cada marketplace implementa a mesma interface. O core nunca sabe qual marketplace esta usando:

```csharp
public interface IMarketplaceAdapter
{
    string MarketplaceId { get; }  // "mercadolivre", "amazon", "shopee"

    // Autenticacao
    Task<TokenResult> RefreshTokenAsync(string refreshToken);

    // Produtos
    Task<MarketplaceProduct> GetProductAsync(string externalId);
    Task<string> CreateProductAsync(ProductListing listing);
    Task UpdateStockAsync(string externalId, int quantity);
    Task UpdatePriceAsync(string externalId, decimal price);

    // Pedidos
    Task<IReadOnlyList<MarketplaceOrder>> GetOrdersAsync(DateRange period);
    Task<OrderDetails> GetOrderDetailsAsync(string orderId);
    Task<IReadOnlyList<MarketplaceFee>> GetOrderFeesAsync(string orderId);

    // Envios
    Task<ShippingInfo> GetShippingInfoAsync(string shipmentId);

    // Mensagens
    Task<IReadOnlyList<Message>> GetMessagesAsync(string orderId);
    Task SendMessageAsync(string orderId, string text);

    // Webhooks
    Task<WebhookEvent> ParseWebhookAsync(HttpRequest request);
}
```

Registro via Dependency Injection:

```csharp
services.AddKeyedScoped<IMarketplaceAdapter, MercadoLivreAdapter>("mercadolivre");
services.AddKeyedScoped<IMarketplaceAdapter, AmazonAdapter>("amazon");
```

### 4.4 Fluxo de Webhooks

```
[Webhook ML chega] → [API valida + enfileira] → [Worker processa]
                                                        │
                               ┌────────────────────────┤
                               ▼                        ▼
                     [Atualiza pedido]       [Dispara eventos internos]
                                                        │
                               ┌────────────┬───────────┤
                               ▼            ▼           ▼
                     [Atualiza        [Registra     [Notifica
                      inventario       financeiro    frontend via
                      em todos os      detalhado]    SignalR]
                      marketplaces]
```

### 4.5 Sincronizacao de Inventario

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

1. Webhook de venda chega → decrementa master → recalcula alocacoes → atualiza todos os marketplaces
2. Lock otimista na tabela de inventario (coluna `version`) para evitar race conditions
3. Reconciliacao periodica: worker a cada 15-30 min compara estoque real vs. esperado

---

## 5. Modelo de Dados (Nucleo Financeiro)

### Estoque

```sql
CREATE TABLE inventory (
    id              UUID PRIMARY KEY,
    sku             VARCHAR(100) NOT NULL UNIQUE,
    total_quantity  INT NOT NULL,
    reserved        INT NOT NULL DEFAULT 0,
    available       INT GENERATED ALWAYS AS (total_quantity - reserved) STORED,
    version         INT NOT NULL DEFAULT 1,
    updated_at      TIMESTAMPTZ NOT NULL
);

CREATE TABLE inventory_allocation (
    id                  UUID PRIMARY KEY,
    sku                 VARCHAR(100) NOT NULL,
    marketplace_id      VARCHAR(50) NOT NULL,
    allocated_quantity  INT NOT NULL,
    external_product_id VARCHAR(200),
    last_synced_at      TIMESTAMPTZ,
    sync_status         VARCHAR(20), -- 'synced', 'pending', 'error'
    UNIQUE(sku, marketplace_id)
);
```

### Custos de Produto

```sql
CREATE TABLE product_costs (
    id              UUID PRIMARY KEY,
    sku             VARCHAR(100) NOT NULL,
    cost_type       VARCHAR(50) NOT NULL,  -- 'purchase', 'manufacturing', 'import'
    unit_cost       NUMERIC(18,4) NOT NULL,
    currency        VARCHAR(3) DEFAULT 'BRL',
    effective_from  DATE NOT NULL,
    effective_until DATE,  -- NULL = custo atual
    notes           TEXT,
    created_at      TIMESTAMPTZ
);
```

### Vendas e Custos por Venda

```sql
CREATE TABLE sales (
    id                  UUID PRIMARY KEY,
    marketplace_id      VARCHAR(50) NOT NULL,
    external_order_id   VARCHAR(200) NOT NULL,
    sku                 VARCHAR(100) NOT NULL,
    quantity            INT NOT NULL,
    unit_price          NUMERIC(18,4) NOT NULL,
    gross_revenue       NUMERIC(18,4) NOT NULL,
    sale_date           TIMESTAMPTZ NOT NULL,
    status              VARCHAR(50) NOT NULL,
    raw_data            JSONB,  -- resposta original da API
    created_at          TIMESTAMPTZ
) PARTITION BY RANGE (sale_date);

CREATE TABLE sale_costs (
    id              UUID PRIMARY KEY,
    sale_id         UUID NOT NULL REFERENCES sales(id),
    cost_category   VARCHAR(50) NOT NULL,
    -- 'marketplace_commission', 'fixed_fee', 'shipping_seller',
    -- 'shipping_buyer', 'payment_fee', 'tax_icms', 'tax_pis_cofins',
    -- 'storage_daily', 'storage_prolonged', 'fulfillment_fee',
    -- 'product_cost', 'packaging', 'advertising', 'other'
    description     TEXT,
    amount          NUMERIC(18,4) NOT NULL,
    percentage_rate NUMERIC(8,4),
    source          VARCHAR(50),  -- 'api', 'manual', 'calculated'
    created_at      TIMESTAMPTZ
);
```

### View de Lucratividade por SKU

```sql
CREATE MATERIALIZED VIEW sku_profitability AS
SELECT
    s.sku,
    s.marketplace_id,
    DATE_TRUNC('month', s.sale_date) AS month,
    COUNT(*) AS total_sales,
    SUM(s.quantity) AS units_sold,
    SUM(s.gross_revenue) AS gross_revenue,
    SUM(sc.amount) FILTER (WHERE sc.cost_category = 'product_cost') AS cogs,
    SUM(sc.amount) FILTER (WHERE sc.cost_category = 'marketplace_commission') AS commissions,
    SUM(sc.amount) FILTER (WHERE sc.cost_category LIKE 'shipping%') AS shipping_costs,
    SUM(sc.amount) FILTER (WHERE sc.cost_category LIKE 'tax%') AS taxes,
    SUM(sc.amount) FILTER (WHERE sc.cost_category LIKE 'storage%') AS storage_costs,
    SUM(sc.amount) FILTER (WHERE sc.cost_category = 'advertising') AS ad_costs,
    SUM(sc.amount) AS total_costs,
    SUM(s.gross_revenue) - SUM(sc.amount) AS net_profit,
    ROUND(
        (SUM(s.gross_revenue) - SUM(sc.amount))
        / NULLIF(SUM(s.gross_revenue), 0) * 100, 2
    ) AS profit_margin_pct
FROM sales s
LEFT JOIN sale_costs sc ON sc.sale_id = s.id
WHERE s.status != 'cancelled'
GROUP BY s.sku, s.marketplace_id, DATE_TRUNC('month', s.sale_date);
```

---

## 6. Gestao de Tokens OAuth

```sql
CREATE TABLE marketplace_connections (
    id                  UUID PRIMARY KEY,
    marketplace_id      VARCHAR(50) NOT NULL,
    seller_id           VARCHAR(200) NOT NULL,
    access_token        TEXT NOT NULL,       -- criptografado AES-256
    refresh_token       TEXT NOT NULL,       -- criptografado AES-256
    token_expires_at    TIMESTAMPTZ NOT NULL,
    scopes              TEXT[],
    is_active           BOOLEAN DEFAULT TRUE,
    refresh_error_count INT DEFAULT 0,
    last_refresh_at     TIMESTAMPTZ,
    created_at          TIMESTAMPTZ,
    updated_at          TIMESTAMPTZ
);
```

- Background worker renova tokens que expiram nos proximos 30 min
- Circuit breaker: 3 falhas seguidas → marca conexao como inativa → notifica usuario
- Chave de criptografia em variavel de ambiente, nunca no codigo

---

## 7. Infraestrutura

### Fase 1 - MVP (custo minimo)

| Recurso | Provedor | Custo/mes |
|---------|----------|-----------|
| VPS 4vCPU, 8GB RAM | Hetzner CX32 ou Contabo | R$ 50-100 |
| PostgreSQL + Redis | Na mesma VPS | R$ 0 |
| Backup (snapshots) | Hetzner | R$ 10-20 |
| Dominio + SSL | Let's Encrypt | R$ 0-40 |
| **Total** | | **~R$ 80-160/mes** |

### Fase 2 - Crescimento

- Migrar para Azure (App Service + Azure SQL + Cache for Redis)
- Ou AWS (ECS Fargate + RDS + ElastiCache)

### CI/CD

- GitHub Actions (gratuito ate 2000 min/mes)
- Docker Compose na VPS com Nginx como reverse proxy
- Deploy: `docker compose pull && docker compose up -d`

---

## 8. Fases de Desenvolvimento

### Fase 1 - MVP (4-6 semanas)

**Objetivo**: Vender no Mercado Livre com rastreamento basico de custos

- [ ] Autenticacao OAuth com Mercado Livre
- [ ] Recebimento de webhooks (pedidos, perguntas)
- [ ] Listagem de pedidos e status
- [ ] Cadastro de produtos com custo de aquisicao
- [ ] Calculo basico de lucro por venda (comissao + custo produto)
- [ ] Dashboard simples (vendas, receita, lucro)
- [ ] Gestao de perguntas (listar + responder)

### Fase 2 - Financeiro Completo (4-6 semanas)

- [ ] Decomposicao completa de custos por venda (via Billing API)
- [ ] Conciliacao financeira automatizada
- [ ] Relatorios de lucratividade por SKU
- [ ] Gestao de estoque com historico
- [ ] Alertas configuráveis
- [ ] Exportacao PDF/Excel

### Fase 3 - Fulfillment e Ads (3-4 semanas)

- [ ] Integracao com estoque Full (consulta + operacoes)
- [ ] Custo de armazenagem acumulado por SKU
- [ ] Simulador Full vs. envio proprio
- [ ] Integracao Mercado Ads (campanhas, metricas, ROI real)
- [ ] Precificacao dinamica com margem minima

### Fase 4 - Multi-Marketplace (4-6 semanas)

- [ ] Adapter para Amazon SP-API
- [ ] Adapter para Shopee
- [ ] Estoque centralizado com sync multi-marketplace
- [ ] Dashboard comparativo entre marketplaces
- [ ] Alocacao de estoque por marketplace

### Fase 5 - Automacao e Inteligencia (continuo)

- [ ] Precificacao automatica baseada em concorrencia
- [ ] Templates inteligentes para respostas
- [ ] Sugestao de reposicao de estoque
- [ ] Previsao de demanda por SKU
- [ ] Multi-CNPJ / multi-conta

---

## 9. Riscos e Mitigacoes

| Risco | Impacto | Mitigacao |
|-------|---------|-----------|
| API do ML fora do ar | Pedidos perdidos | Fila de retry com dead letter queue |
| Rate limit excedido (18k req/h) | Sync atrasado | Rate limiter no client, priorizar webhooks |
| Token OAuth expira sem renovar | Integracao para | Worker proativo de renovacao + alerta |
| Overselling (venda sem estoque) | Cancelamento + reputacao | Lock otimista + sync em tempo real |
| Mudanca de taxas/comissoes do ML | Calculos incorretos | Tabela de taxas versionada + alertas de divergencia |
| ML nao tem sandbox | Testes arriscados | Usuarios de teste em producao (max 10) |

---

## 10. Fontes e Referencias

- API Mercado Livre: https://developers.mercadolivre.com.br/
- Fulfillment API: https://developers.mercadolivre.com.br/en_us/fulfillment
- Billing API: https://developers.mercadolivre.com.br/en_us/billing-reports
- Product Ads API: https://developers.mercadolivre.com.br/en_us/product-ads-us-read
- Amazon SP-API: https://developer-docs.amazon.com/sp-api/
- Shopee Open Platform: https://open.shopee.com/
