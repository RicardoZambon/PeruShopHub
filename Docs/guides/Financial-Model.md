# Modelo Financeiro

Guia tecnico sobre o modelo de calculo de custos e lucratividade do PeruShopHub.

## Conceito Central

O diferencial do PeruShopHub e o **calculo de lucratividade real por venda**. Nenhum ERP/hub existente calcula o lucro liquido verdadeiro considerando todos os custos: comissao do marketplace, taxas fixas, frete real, fulfillment, publicidade, impostos, custo do produto, embalagem e absorcao de cupons.

O sistema decompoe cada venda em categorias de custo granulares e calcula o lucro real:

```
Lucro = Valor Total da Venda - Soma(todos os custos)
```

---

## Precisao Financeira

**Regra absoluta**: nunca usar `float` ou `double` para valores monetarios.

| Camada | Tipo |
|--------|------|
| C# (backend) | `decimal` |
| PostgreSQL | `NUMERIC(18,4)` |
| Frontend | Exibir com `currency` pipe + `'BRL'` |
| Value Object | `Money` (decimal + currency) |

O value object `Money` encapsula valor decimal com moeda, garantindo que operacoes monetarias sao type-safe.

---

## OrderCost — Decomposicao de Custos por Venda

A entidade `OrderCost` registra cada componente de custo de um pedido:

```csharp
public class OrderCost : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Category { get; set; }    // Tipo do custo
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public string Source { get; set; }       // "Calculated", "Manual", "API"
}
```

### Categorias de Custo

| Categoria | Descricao | Fonte |
|-----------|-----------|-------|
| `product_cost` | Custo dos produtos (custo medio ponderado * quantidade) | Calculated |
| `packaging` | Custo de embalagem por item | Calculated |
| `marketplace_commission` | Comissao percentual do marketplace | Calculated |
| `fixed_fee` | Taxa fixa por item (produtos < R$ 79) | Calculated |
| `tax` | Impostos (configuravel, padrao 6%) | Calculated |
| `shipping_seller` | Frete pago pelo vendedor | API/Manual |
| `payment_fee` | Taxa de processamento de pagamento | API/Manual |
| `fulfillment_fee` | Taxa de fulfillment (Full) | API/Manual |
| `storage_daily` | Custo de armazenagem (Full) | API/Manual |
| `advertising` | Custo de publicidade atribuido | Manual |

O campo `Source` indica a origem do custo:
- **Calculated** — calculado automaticamente pelo `CostCalculationService`
- **API** — obtido via API do marketplace (billing, shipping)
- **Manual** — inserido manualmente pelo usuario

### Calculo Automatico

O `CostCalculationService.CalculateOrderCostsAsync` executa o calculo para cada pedido:

1. **product_cost**: busca variantes por SKU → `PurchaseCost * Quantity` para cada item
2. **packaging**: busca `Product.PackagingCost * Quantity` para cada item
3. **marketplace_commission**: resolve taxa de comissao via `CommissionRule` → `TotalAmount * taxa`
4. **fixed_fee**: aplica tabela de taxa fixa para itens com preco < R$ 79
5. **tax**: `TotalAmount * taxRate` (configuravel via `CostSettings:TaxRate`)

O lucro do pedido e calculado como:

```csharp
order.Profit = order.TotalAmount - order.Costs.Sum(c => c.Value);
```

### Recalculo

O metodo `RecalculateOrderCostsAsync` permite recalcular custos de um pedido existente:

1. Remove custos com `Source = "Calculated"` (preserva custos manuais e de API)
2. Recalcula todos os custos automaticos
3. Atualiza `Order.Profit`
4. Salva no banco

Isso e util quando o custo de um produto muda ou uma regra de comissao e atualizada.

---

## Comissoes do Mercado Livre

### CommissionRule

A entidade `CommissionRule` permite configurar taxas de comissao por marketplace, categoria e tipo de anuncio:

```csharp
public class CommissionRule : ITenantScoped
{
    public string MarketplaceId { get; set; }     // "mercadolivre"
    public string? CategoryPattern { get; set; }  // ID da categoria ou null
    public string? ListingType { get; set; }      // "classico", "premium" ou null
    public decimal Rate { get; set; }             // Ex: 0.13 = 13%
    public bool IsDefault { get; set; }           // Regra padrao do marketplace
}
```

### Resolucao de Taxa

O algoritmo de resolucao segue prioridade decrescente:

1. Match especifico: marketplace + categoria + tipo de anuncio
2. Fallback por categoria: marketplace + categoria (sem tipo)
3. Fallback padrao: marketplace + `IsDefault = true`
4. Fallback hardcoded: 13% (ultimo recurso, com warning no log)

### Taxas por Categoria (ML)

As comissoes do ML variam entre 11% e 19% dependendo da categoria e tipo de anuncio:

| Tipo | Faixa de Comissao |
|------|------------------|
| Classico | 11% - 16% |
| Premium | 13% - 19% |

Produtos na categoria Premium tem maior exposicao no algoritmo de busca do ML, mas pagam comissao mais alta.

---

## Taxas Fixas (ML)

Para produtos com preco unitario abaixo de R$ 79, o ML cobra uma taxa fixa adicional:

| Faixa de Preco | Taxa Fixa |
|----------------|-----------|
| Ate R$ 12,50 | 50% do preco |
| R$ 12,51 - R$ 29,00 | R$ 6,25 |
| R$ 29,01 - R$ 50,00 | R$ 6,50 |
| R$ 50,01 - R$ 79,00 | R$ 6,75 |
| Acima de R$ 79,00 | Sem taxa fixa |

A implementacao esta em `CostCalculationService.CalculateFixedFee`:

```csharp
private static decimal CalculateFixedFee(decimal unitPrice) => unitPrice switch
{
    <= 12.50m => unitPrice * 0.50m,
    <= 29m    => 6.25m,
    <= 50m    => 6.50m,
    <= 79m    => 6.75m,
    _         => 0m
};
```

---

## Frete

### Divisao de Custos de Frete

O Mercado Livre impoe frete gratis obrigatorio para produtos acima de R$ 79. Nesse caso, o custo do frete e parcialmente absorvido pelo vendedor:

- **Acima de R$ 79**: frete gratis para o comprador, vendedor paga parcialmente
- **Abaixo de R$ 79**: comprador paga o frete (vendedor pode optar por frete gratis para melhor posicionamento)

O custo real de frete para o vendedor vem da API de billing do ML e e registrado como `OrderCost` com categoria `shipping_seller`.

---

## Historico de Custos de Produto

### ProductCostHistory

Cada mudanca no custo de um produto e registrada:

```csharp
public class ProductCostHistory : ITenantScoped
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal PreviousCost { get; set; }    // Custo anterior
    public decimal NewCost { get; set; }          // Novo custo (media ponderada)
    public int Quantity { get; set; }             // Quantidade recebida
    public decimal UnitCostPaid { get; set; }     // Custo unitario efetivo pago
    public Guid? PurchaseOrderId { get; set; }    // PO que gerou a mudanca
    public string Reason { get; set; }            // Motivo da alteracao
}
```

O custo medio ponderado e calculado assim:

```
NovoCusto = (EstoqueAtual * CustoAtual + NovaQtd * NovoCustoUnitario) / (EstoqueAtual + NovaQtd)
```

Esse historico permite:
- Rastrear evolucao de custos ao longo do tempo
- Auditar mudancas de preco de fornecedores
- Recalcular lucratividade historica com custos corretos

---

## Ordens de Compra e Alocacao de Custos

### PurchaseOrder

A entidade `PurchaseOrder` gerencia compras de fornecedores:

```csharp
public class PurchaseOrder : ITenantScoped
{
    public string? Supplier { get; set; }
    public string Status { get; set; }          // "Rascunho", "Recebido"
    public decimal Subtotal { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal Total { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; }
    public ICollection<PurchaseOrderCost> Costs { get; set; }
}
```

### PurchaseOrderCost — Custos Adicionais

Cada custo adicional (frete do fornecedor, seguro, impostos de importacao) e registrado com metodo de distribuicao:

```csharp
public class PurchaseOrderCost : ITenantScoped
{
    public string Description { get; set; }            // Ex: "Frete fornecedor"
    public decimal Value { get; set; }
    public string DistributionMethod { get; set; }     // "by_value", "by_quantity", "manual"
}
```

### Metodos de Distribuicao

| Metodo | Formula | Quando Usar |
|--------|---------|-------------|
| `by_value` | `custo * (valorItem / valorTotal)` | Custos proporcionais ao valor (impostos, seguro) |
| `by_quantity` | `custo * (qtdItem / qtdTotal)` | Custos proporcionais a quantidade (frete por peso uniforme) |
| `manual` | Alocacao direta pelo usuario | Custos ja distribuidos no frontend |

Exemplo com `by_value`:
- PO com 2 itens: Item A (R$ 800), Item B (R$ 200)
- Custo adicional de frete: R$ 100
- Item A recebe: R$ 100 * (800/1000) = R$ 80
- Item B recebe: R$ 100 * (200/1000) = R$ 20

---

## Integracao com API de Billing (Planejado)

### Endpoint

```
GET /billing/integration/group/ML/order/details?order_id={order_id}
```

Esse endpoint retorna os valores reais cobrados pelo ML para um pedido especifico, incluindo:

- Comissao efetivamente cobrada
- Taxas fixas
- Custo de frete para o vendedor
- Taxa de fulfillment (se Full)
- Taxa de pagamento

O sistema deve priorizar dados da API de billing sobre calculos estimados. Quando disponivel, os custos com `Source = "API"` substituem os custos com `Source = "Calculated"`.

---

## Conceitos Futuros

### Materialized View de Lucratividade por SKU

Uma materialized view consolidando metricas por SKU:

```sql
-- Conceito (nao implementado ainda)
CREATE MATERIALIZED VIEW sku_profitability AS
SELECT
    oi.sku,
    COUNT(DISTINCT o.id) as total_orders,
    SUM(oi.quantity) as total_units,
    SUM(oi.total_price) as total_revenue,
    SUM(oc_product.value) as total_product_cost,
    SUM(oc_commission.value) as total_commission,
    SUM(o.profit) as total_profit,
    AVG(o.profit / NULLIF(o.total_amount, 0)) as avg_margin
FROM order_items oi
JOIN orders o ON oi.order_id = o.id
-- joins com order_costs por categoria...
GROUP BY oi.sku;
```

### Classificacao ABC

Classificacao automatica de produtos por contribuicao ao faturamento:

| Classe | Criterio | Acao |
|--------|----------|------|
| **A** | 80% do faturamento (top ~20% SKUs) | Foco em otimizacao de margem, nunca deixar sem estoque |
| **B** | 15% do faturamento (~30% SKUs) | Monitorar, manter estoque adequado |
| **C** | 5% do faturamento (~50% SKUs) | Avaliar descontinuacao, reduzir estoque |

A classificacao deve ser recalculada periodicamente (semanal ou mensal) e exibida no dashboard de produtos.

---

## Arquivos Relevantes

| Arquivo | Descricao |
|---------|-----------|
| `src/PeruShopHub.Core/Entities/OrderCost.cs` | Entidade de custo por pedido |
| `src/PeruShopHub.Core/Entities/CommissionRule.cs` | Regra de comissao configuravel |
| `src/PeruShopHub.Core/Entities/ProductCostHistory.cs` | Historico de custos de produto |
| `src/PeruShopHub.Core/Entities/PurchaseOrder.cs` | Ordem de compra |
| `src/PeruShopHub.Core/Entities/PurchaseOrderCost.cs` | Custo adicional de PO |
| `src/PeruShopHub.Core/Interfaces/ICostCalculationService.cs` | Interface do servico de calculo |
| `src/PeruShopHub.Infrastructure/Services/CostCalculationService.cs` | Implementacao completa |
| `src/PeruShopHub.Core/ValueObjects/Money.cs` | Value object monetario |
| `src/PeruShopHub.Infrastructure/Persistence/Configurations/OrderCostConfiguration.cs` | Configuracao EF Core |
