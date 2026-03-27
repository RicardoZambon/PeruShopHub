# Gestao de Estoque

Guia tecnico sobre a arquitetura de gestao de estoque do PeruShopHub.

## Visao Geral

O sistema de estoque do PeruShopHub segue o conceito de **estoque mestre** (master stock): uma unica fonte de verdade com alocacoes por marketplace. Toda movimentacao e rastreada via trilha de auditoria (`StockMovement`), e o controle de concorrencia usa **optimistic locking** para prevenir overselling.

---

## Implementacao Atual

### Modelo de Dados

O estoque vive em duas entidades principais:

**ProductVariant** — cada variante possui um campo `Stock` (quantidade atual) e `PurchaseCost` (custo medio ponderado):

```csharp
public class ProductVariant : ITenantScoped
{
    public int Stock { get; set; }
    public decimal? PurchaseCost { get; set; }
    // ...
}
```

**StockMovement** — registro de auditoria para cada movimentacao:

```csharp
public class StockMovement : ITenantScoped
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string Type { get; set; }    // "Entrada", "Saida", "Ajuste"
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Reason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Tipos de movimentacao:
- **Entrada** — recebimento de ordem de compra
- **Saida** — venda fulfillment (deducao automatica)
- **Ajuste** — correcao manual via interface

### InventoryService

O servico `InventoryService` expoe tres operacoes:

| Metodo | Descricao |
|--------|-----------|
| `GetOverviewAsync` | Visao geral paginada — SKU, nome, estoque total, reservado, disponivel, custo unitario, valor em estoque |
| `GetMovementsAsync` | Historico de movimentacoes com filtros por produto, tipo e periodo |
| `CreateMovementAsync` | Ajuste manual — recebe `ProductId`, `VariantId`, `Quantity`, `Reason` |

O overview calcula estoque somando `variant.Stock` de todas as variantes ativas do produto. O valor em estoque e `Stock * PurchaseCost` do produto.

### Fluxo de Entrada (Ordem de Compra)

Quando uma ordem de compra e recebida via `CostCalculationService.ReceivePurchaseOrderAsync`:

1. Custos adicionais da PO sao distribuidos proporcionalmente (por valor ou quantidade)
2. Para cada item:
   - Calcula `EffectiveUnitCost = (TotalCost + AllocatedAdditionalCost) / Quantity`
   - Atualiza custo medio ponderado da variante: `(estoqueAtual * custoAtual + novaQtd * novoCusto) / totalQtd`
   - Incrementa `variant.Stock`
   - Cria `StockMovement` tipo "Entrada"
   - Cria `ProductCostHistory` para rastreabilidade
3. Atualiza `Product.PurchaseCost` como media ponderada de todas as variantes ativas
4. Marca PO como "Recebido"
5. Notifica frontend via SignalR

### Fluxo de Saida (Venda)

Quando um pedido e fulfillment via `CostCalculationService.FulfillOrderAsync`:

1. Busca variantes por SKU dos itens do pedido
2. Para cada item:
   - Decrementa `variant.Stock` (minimo 0)
   - Cria `StockMovement` tipo "Saida" com custo unitario da variante
3. Marca pedido como `IsFulfilled = true`
4. Notifica frontend via SignalR

---

## Optimistic Locking

A entidade `Product` possui uma coluna `Version` (int) usada para controle de concorrencia otimista:

```csharp
public class Product : ITenantScoped
{
    public int Version { get; set; }
    // ...
}
```

O EF Core usa essa coluna como concurrency token. Se dois requests tentarem atualizar o mesmo produto simultaneamente, o segundo recebe `DbUpdateConcurrencyException`, que deve ser tratado com retry ou mensagem ao usuario.

Isso previne cenarios de overselling onde duas vendas simultaneas tentam decrementar o mesmo estoque.

---

## Estoque Mestre e Alocacoes por Marketplace

### Conceito

O estoque no PeruShopHub segue o modelo de **estoque mestre unico**:

```
Estoque Mestre (fonte de verdade)
├── Alocacao Mercado Livre: X unidades
├── Alocacao Amazon: Y unidades
├── Alocacao Shopee: Z unidades
└── Reserva interna: W unidades
```

O estoque disponivel total e sempre `sum(variant.Stock)`. As alocacoes por marketplace determinam quanto estoque cada canal pode vender. A soma das alocacoes nunca deve exceder o estoque total.

### Sincronizacao Multi-Marketplace

Quando uma venda ocorre em qualquer marketplace:

1. Webhook recebido → API valida e enfileira no Redis
2. Worker processa → decrementa estoque mestre
3. Worker atualiza estoque em **todos** os outros marketplaces conectados
4. Cada atualizacao usa o adapter do marketplace correspondente

Isso garante que uma venda no ML atualize o estoque na Amazon e Shopee automaticamente.

---

## Integracao Mercado Livre (Planejado)

### Atualizacao de Estoque

Para atualizar estoque de um anuncio no ML:

```
PUT /items/{item_id}
{
  "available_quantity": 50
}
```

Rate limit: 18.000 req/hora. O cliente HTTP deve implementar rate limiting com Polly.

### Estoque Fulfillment (Full)

Para consultar estoque no centro de distribuicao do ML:

```
GET /inventories/{inventory_id}/stock/fulfillment
```

Retorna detalhes de estoque disponivel, reservado e em transito dentro do armazem Full.

### Webhook de Estoque

O ML envia notificacoes de mudanca de estoque via webhook `fbm_stock_operations`:

```json
{
  "topic": "fbm_stock_operations",
  "resource": "/inventories/{inventory_id}/stock/operations/{operation_id}",
  "user_id": 123456789
}
```

O fluxo e:
1. Webhook chega → API responde < 500ms (requisito ML)
2. Operacao enfileirada no Redis
3. Worker processa: consulta detalhes da operacao, reconcilia com estoque local

### Reconciliacao Periodica

Um worker de background executa periodicamente (configuravel, padrao: a cada 6 horas):

1. Para cada marketplace conectado:
   - Consulta estoque atual no marketplace via API
   - Compara com estoque local alocado para aquele marketplace
2. Se houver divergencia:
   - Registra alerta no sistema
   - Se divergencia < threshold configuravel: corrige automaticamente
   - Se divergencia > threshold: notifica usuario para revisao manual
3. Gera relatorio de reconciliacao

---

## Mercado Livre Full — Custos de Armazenagem

### Custos Diarios por Tamanho

O ML Full cobra armazenagem diaria baseada no tamanho do produto:

| Categoria | Custo Diario |
|-----------|-------------|
| Pequeno | R$ 0,007 |
| Medio | R$ 0,015 |
| Grande | R$ 0,035 |
| Especial | R$ 0,050 |
| Extra Grande | R$ 0,107 |

### Penalidades de Armazenagem Prolongada

Produtos armazenados por mais de 90 dias sofrem penalidade progressiva:

- 91-180 dias: custo diario dobra
- 181-365 dias: custo diario triplica
- > 365 dias: custo diario quadruplica

### Status de Estoque Full

O inventario no Full pode ter diferentes status:

| Status | Descricao |
|--------|-----------|
| `available` | Disponivel para venda |
| `not_available` | Nao disponivel (em processamento) |
| `damage` | Danificado no armazem |
| `lost` | Perdido/nao localizado |
| `in_transfer` | Em transferencia entre armazens |

O sistema deve rastrear cada status e seus custos associados.

---

## Decisao Full vs Envio Proprio

Para cada SKU, o sistema calcula qual modalidade e mais lucrativa:

### Formula de Decisao

```
Custo Full por Unidade = (Custo Armazenagem Diario * Dias Medio em Estoque)
                        + Taxa Fulfillment por Venda

Custo Envio Proprio = Custo Frete Medio
                     + Custo Embalagem
                     + Custo Mao de Obra por Envio

Se Custo Full < Custo Envio Proprio → usar Full
Se Custo Full >= Custo Envio Proprio → envio proprio
```

Fatores adicionais na decisao:

- **Velocidade de giro**: produtos com alto giro se beneficiam do Full (menor custo de armazenagem acumulado)
- **Volume de vendas**: Full tem melhor posicionamento no algoritmo do ML
- **Sazonalidade**: considerar picos de demanda onde Full garante entrega rapida
- **Risco de dano**: produtos frageis tem maior risco de dano no Full

O dashboard deve apresentar uma recomendacao por SKU baseada nos dados historicos de vendas e custos.

---

## Arquivos Relevantes

| Arquivo | Descricao |
|---------|-----------|
| `src/PeruShopHub.Core/Entities/StockMovement.cs` | Entidade de movimentacao |
| `src/PeruShopHub.Core/Entities/Product.cs` | Produto com Version (optimistic locking) |
| `src/PeruShopHub.Core/Entities/ProductVariant.cs` | Variante com Stock e PurchaseCost |
| `src/PeruShopHub.Application/Services/InventoryService.cs` | Servico de inventario |
| `src/PeruShopHub.Application/Services/IInventoryService.cs` | Interface do servico |
| `src/PeruShopHub.Infrastructure/Services/CostCalculationService.cs` | Recebimento de PO e fulfillment de pedido |
| `src/PeruShopHub.API/Controllers/InventoryController.cs` | Controller REST |
