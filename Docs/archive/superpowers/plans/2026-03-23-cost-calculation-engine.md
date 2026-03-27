# Cost Calculation Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace pre-calculated seed data with a dynamic cost calculation engine — weighted average product cost from purchase orders, and auto-calculated sale costs on order creation.

**Architecture:** New entities (PurchaseOrder, ProductCostHistory, StockMovement, CommissionRule) extend the existing modular monolith. `ICostCalculationService` in Core with implementation in Infrastructure. Controllers follow existing pattern (inject DbContext directly). Frontend adds PO pages and rewires inventory page.

**Tech Stack:** .NET 9 / EF Core 9 / PostgreSQL 16 / Angular 21 / SignalR

**PRD:** `tasks/prd-cost-calculation-engine.md`

**Branch:** `ralph/backend-wiring`

---

## Execution Strategy

| Phase | Agents | Mode | Depends On |
|-------|--------|------|------------|
| **1 — Backend Entities + Migration** | 1 sequential | on branch | — |
| **2 — Backend Services + Controllers** | 3 parallel | worktrees | Phase 1 |
| **3 — Frontend** | 3 parallel | worktrees | Phase 2 |
| **4 — Cleanup + Review** | 1 sequential + 1 reviewer | on branch | Phase 3 |

---

## File Map

### New Backend Files

```
src/PeruShopHub.Core/
├── Entities/
│   ├── PurchaseOrder.cs              ← NEW
│   ├── PurchaseOrderItem.cs          ← NEW
│   ├── PurchaseOrderCost.cs          ← NEW
│   ├── ProductCostHistory.cs         ← NEW
│   ├── StockMovement.cs              ← NEW
│   ├── CommissionRule.cs             ← NEW
│   └── ProductVariant.cs             ← MODIFY: add IsDefault bool
├── Interfaces/
│   └── ICostCalculationService.cs    ← NEW

src/PeruShopHub.Infrastructure/
├── Persistence/
│   ├── PeruShopHubDbContext.cs        ← MODIFY: add 6 new DbSets
│   ├── Configurations/
│   │   ├── PurchaseOrderConfiguration.cs       ← NEW
│   │   ├── PurchaseOrderItemConfiguration.cs   ← NEW
│   │   ├── PurchaseOrderCostConfiguration.cs   ← NEW
│   │   ├── ProductCostHistoryConfiguration.cs  ← NEW
│   │   ├── StockMovementConfiguration.cs       ← NEW
│   │   ├── CommissionRuleConfiguration.cs      ← NEW
│   │   └── ProductVariantConfiguration.cs      ← MODIFY: IsDefault
│   ├── Migrations/                    ← auto-generated
│   └── Seeds/
│       └── SeedCommissionRules.sql    ← NEW
├── Services/
│   └── CostCalculationService.cs     ← NEW

src/PeruShopHub.Application/DTOs/
├── PurchaseOrders/
│   ├── PurchaseOrderListDto.cs       ← NEW
│   ├── PurchaseOrderDetailDto.cs     ← NEW
│   ├── CreatePurchaseOrderDto.cs     ← NEW
│   └── CostDistributionPreviewDto.cs ← NEW
├── Inventory/
│   ├── InventoryItemDto.cs           ← NEW
│   ├── StockMovementDto.cs           ← NEW
│   └── StockAdjustmentDto.cs         ← NEW
├── Products/
│   └── ProductCostHistoryDto.cs      ← NEW
├── Settings/
│   └── CommissionRuleDto.cs          ← NEW

src/PeruShopHub.API/Controllers/
├── PurchaseOrdersController.cs       ← NEW
├── InventoryController.cs            ← NEW
├── OrdersController.cs               ← MODIFY: add recalculate-costs endpoint
├── ProductsController.cs             ← MODIFY: add cost-history endpoint
├── SettingsController.cs             ← MODIFY: add commission-rules endpoints
```

### New/Modified Angular Files

```
src/PeruShopHub.Web/src/app/
├── services/
│   ├── purchase-order.service.ts     ← NEW
│   ├── inventory.service.ts          ← NEW
│   └── order.service.ts              ← MODIFY: add recalculateCosts method
├── pages/
│   ├── purchase-orders/
│   │   ├── purchase-orders-list.component.ts   ← NEW
│   │   ├── purchase-orders-list.component.html ← NEW
│   │   ├── purchase-orders-list.component.scss ← NEW
│   │   ├── purchase-order-form.component.ts    ← NEW
│   │   ├── purchase-order-form.component.html  ← NEW
│   │   ├── purchase-order-form.component.scss  ← NEW
│   │   ├── purchase-order-detail.component.ts  ← NEW
│   │   ├── purchase-order-detail.component.html← NEW
│   │   └── purchase-order-detail.component.scss← NEW
│   ├── inventory/
│   │   └── inventory.component.ts    ← MODIFY: remove mocks, wire to API
│   ├── products/
│   │   ├── product-detail.component.ts   ← MODIFY: add cost history section
│   │   └── product-detail.component.html ← MODIFY: cost history table
│   └── sales/
│       ├── sale-detail.component.ts  ← MODIFY: add source badges + recalculate
│       └── sale-detail.component.html← MODIFY: source badges
├── shared/components/sidebar/
│   └── sidebar.component.ts          ← MODIFY: add Compras nav item
├── app.routes.ts                     ← MODIFY: add /compras routes
```

---

## Phase 1 — Backend Entities + Migration (Sequential, 1 Agent)

### Task 1.1: New Entities

**Files to create:**

**`src/PeruShopHub.Core/Entities/PurchaseOrder.cs`:**
```csharp
namespace PeruShopHub.Core.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string? Supplier { get; set; }
    public string Status { get; set; } = "Rascunho"; // Rascunho, Recebido, Cancelado
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }       // sum of items
    public decimal AdditionalCosts { get; set; } // sum of PurchaseOrderCosts
    public decimal Total { get; set; }           // subtotal + additionalCosts
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PurchaseOrderCost> Costs { get; set; } = new List<PurchaseOrderCost>();
}
```

**`src/PeruShopHub.Core/Entities/PurchaseOrderItem.cs`:**
```csharp
namespace PeruShopHub.Core.Entities;

public class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }           // quantity * unitCost
    public decimal AllocatedAdditionalCost { get; set; } // distributed from PO costs
    public decimal EffectiveUnitCost { get; set; }   // (totalCost + allocatedAdditionalCost) / quantity
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
```

**`src/PeruShopHub.Core/Entities/PurchaseOrderCost.cs`:**
```csharp
namespace PeruShopHub.Core.Entities;

public class PurchaseOrderCost
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string DistributionMethod { get; set; } = "by_value"; // by_value, by_quantity, manual
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
}
```

**`src/PeruShopHub.Core/Entities/ProductCostHistory.cs`:**
```csharp
namespace PeruShopHub.Core.Entities;

public class ProductCostHistory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal PreviousCost { get; set; }
    public decimal NewCost { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostPaid { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
```

**`src/PeruShopHub.Core/Entities/StockMovement.cs`:**
```csharp
namespace PeruShopHub.Core.Entities;

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string Type { get; set; } = "Entrada"; // Entrada, Saída, Ajuste
    public int Quantity { get; set; }  // positive for in, negative for out
    public decimal? UnitCost { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Reason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
```

**`src/PeruShopHub.Core/Entities/CommissionRule.cs`:**
```csharp
namespace PeruShopHub.Core.Entities;

public class CommissionRule
{
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = "mercadolivre";
    public string? CategoryPattern { get; set; }  // null = default for marketplace
    public string? ListingType { get; set; }       // Classic, Premium, null = any
    public decimal Rate { get; set; }              // 0.13 = 13%
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 1:** Create all 6 entity files above
- [ ] **Step 2:** Add `IsDefault` to `ProductVariant.cs`: `public bool IsDefault { get; set; }`
- [ ] **Step 3:** Create `ICostCalculationService` in `Core/Interfaces/`:
```csharp
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Core.Interfaces;

public interface ICostCalculationService
{
    Task<List<OrderCost>> CalculateOrderCostsAsync(Order order, CancellationToken ct = default);
    Task RecalculateOrderCostsAsync(Guid orderId, CancellationToken ct = default);
}
```
- [ ] **Step 4:** `dotnet build` — verify 0 errors
- [ ] **Step 5:** Commit: `feat: add cost calculation entities and ICostCalculationService interface`

---

### Task 1.2: DbContext + Configurations + Migration

**Files to modify/create:**

- [ ] **Step 1:** Add 6 new `DbSet<T>` properties to `PeruShopHubDbContext.cs`:
```csharp
public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
public DbSet<PurchaseOrderCost> PurchaseOrderCosts => Set<PurchaseOrderCost>();
public DbSet<ProductCostHistory> ProductCostHistories => Set<ProductCostHistory>();
public DbSet<StockMovement> StockMovements => Set<StockMovement>();
public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
```

- [ ] **Step 2:** Create 6 `IEntityTypeConfiguration<T>` files in `Configurations/`. Key rules:
  - All decimal properties: `.HasPrecision(18, 4)`
  - PurchaseOrder: status max 50, supplier max 300
  - PurchaseOrderItem: FKs to PurchaseOrder (Cascade), Product (Restrict), ProductVariant (Restrict)
  - ProductCostHistory: FKs to Product (Cascade), optional Variant, optional PurchaseOrder
  - StockMovement: FKs to Product (Cascade), optional Variant, optional PurchaseOrder, optional Order
  - CommissionRule: index on (MarketplaceId, CategoryPattern, ListingType)
  - ProductVariantConfiguration: add `builder.Property(v => v.IsDefault).HasDefaultValue(false);`

- [ ] **Step 3:** Generate migration:
```bash
dotnet ef migrations add AddCostCalculationEntities --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API
```

- [ ] **Step 4:** Apply migration:
```bash
dotnet ef database update --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API
```

- [ ] **Step 5:** Commit: `feat: add DbContext, configurations, and migration for cost entities`

---

### Task 1.3: Seed Commission Rules + DTOs

- [ ] **Step 1:** Create `src/PeruShopHub.Infrastructure/Persistence/Seeds/SeedCommissionRules.sql` with ML defaults:
```sql
-- Mercado Livre Classic commission rates by category
INSERT INTO "CommissionRules" ("Id", "MarketplaceId", "CategoryPattern", "ListingType", "Rate", "IsDefault", "CreatedAt") VALUES
(gen_random_uuid(), 'mercadolivre', NULL, NULL, 0.13, true, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Eletrônicos', 'Classic', 0.13, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Eletrônicos', 'Premium', 0.18, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Informática', 'Classic', 0.11, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Informática', 'Premium', 0.16, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Moda', 'Classic', 0.14, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Moda', 'Premium', 0.19, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Casa e Decoração', 'Classic', 0.115, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Casa e Decoração', 'Premium', 0.165, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Esportes', 'Classic', 0.14, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Esportes', 'Premium', 0.19, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Beleza', 'Classic', 0.14, false, NOW()),
(gen_random_uuid(), 'mercadolivre', 'Beleza', 'Premium', 0.19, false, NOW());
```

- [ ] **Step 2:** Create seed migration and apply:
```bash
dotnet ef migrations add SeedCommissionRules --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API
```
Modify migration to execute embedded SQL (same pattern as SeedData).

- [ ] **Step 3:** Create all DTOs in `Application/DTOs/`:

**PurchaseOrders/PurchaseOrderListDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.PurchaseOrders;
public record PurchaseOrderListDto(Guid Id, string? Supplier, string Status, int ItemCount, decimal Total, DateTime CreatedAt, DateTime? ReceivedAt);
```

**PurchaseOrders/PurchaseOrderDetailDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.PurchaseOrders;

public record PurchaseOrderDetailDto(
    Guid Id, string? Supplier, string Status, string? Notes,
    decimal Subtotal, decimal AdditionalCosts, decimal Total,
    DateTime CreatedAt, DateTime? ReceivedAt,
    IReadOnlyList<PurchaseOrderItemDto> Items,
    IReadOnlyList<PurchaseOrderCostDto> Costs);

public record PurchaseOrderItemDto(
    Guid Id, Guid ProductId, Guid VariantId, string ProductName, string Sku,
    int Quantity, decimal UnitCost, decimal TotalCost,
    decimal AllocatedAdditionalCost, decimal EffectiveUnitCost);

public record PurchaseOrderCostDto(Guid Id, string Description, decimal Value, string DistributionMethod);
```

**PurchaseOrders/CreatePurchaseOrderDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.PurchaseOrders;

public record CreatePurchaseOrderDto(
    string? Supplier, string? Notes,
    List<CreatePurchaseOrderItemDto> Items,
    List<CreatePurchaseOrderCostDto>? Costs);

public record CreatePurchaseOrderItemDto(Guid ProductId, Guid VariantId, int Quantity, decimal UnitCost);
public record CreatePurchaseOrderCostDto(string Description, decimal Value, string DistributionMethod);
```

**PurchaseOrders/CostDistributionPreviewDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.PurchaseOrders;

public record CostDistributionPreviewDto(IReadOnlyList<ItemAllocationDto> Allocations);
public record ItemAllocationDto(Guid ItemId, string ProductName, string Sku, decimal AllocatedAmount, decimal EffectiveUnitCost);
```

**Inventory/InventoryItemDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.Inventory;
public record InventoryItemDto(string Sku, string ProductName, int TotalStock, int Reserved, int Available, decimal UnitCost, decimal StockValue);
```

**Inventory/StockMovementDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.Inventory;
public record StockMovementDto(Guid Id, string Sku, string ProductName, string Type, int Quantity, decimal? UnitCost, string? Reason, string? CreatedBy, DateTime CreatedAt);
```

**Inventory/StockAdjustmentDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.Inventory;
public record StockAdjustmentDto(Guid ProductId, Guid VariantId, int Quantity, string Reason);
```

**Products/ProductCostHistoryDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.Products;
public record ProductCostHistoryDto(Guid Id, DateTime Date, decimal PreviousCost, decimal NewCost, int Quantity, decimal UnitCostPaid, Guid? PurchaseOrderId, string Reason);
```

**Settings/CommissionRuleDto.cs:**
```csharp
namespace PeruShopHub.Application.DTOs.Settings;
public record CommissionRuleDto(Guid Id, string MarketplaceId, string? CategoryPattern, string? ListingType, decimal Rate, bool IsDefault);
public record CreateCommissionRuleDto(string MarketplaceId, string? CategoryPattern, string? ListingType, decimal Rate);
```

- [ ] **Step 4:** `dotnet build` — verify 0 errors
- [ ] **Step 5:** Commit: `feat: add seed commission rules and cost calculation DTOs`

---

## Phase 2 — Backend Services + Controllers (3 Parallel Agents)

### Task 2.1: CostCalculationService + PO Receipt Logic (Agent: `cost-engine`)

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Services/CostCalculationService.cs`
- Modify: `src/PeruShopHub.API/Program.cs` — register `ICostCalculationService`
- Modify: `src/PeruShopHub.API/Controllers/OrdersController.cs` — add recalculate endpoint

**`CostCalculationService.cs`** implements `ICostCalculationService`:

```csharp
// Key methods:

// CalculateOrderCostsAsync(Order order):
//   1. For each OrderItem, lookup variant's PurchaseCost → product_cost = purchaseCost × quantity
//   2. For each OrderItem, lookup product's PackagingCost → packaging = packagingCost × quantity
//   3. Lookup commission rate: match CommissionRule by (marketplace, category, listingType) with fallback chain
//      → marketplace_commission = order.TotalAmount × rate
//   4. Calculate fixed_fee: if any item price < R$79, apply tiered fee (≤12.50→50%, ≤29→6.25, ≤50→6.50, ≤79→6.75)
//   5. Lookup tax rate from settings (default 6%) → tax = order.TotalAmount × taxRate
//   6. Return list of OrderCost records with Source = "Calculated"

// RecalculateOrderCostsAsync(Guid orderId):
//   1. Load order with items and costs
//   2. Remove existing costs where Source == "Calculated"
//   3. Call CalculateOrderCostsAsync
//   4. Add new costs, update Order.Profit = TotalAmount - SUM(costs)
//   5. Save

// ReceivePurchaseOrderAsync(Guid purchaseOrderId):
//   1. Load PO with items and costs
//   2. Validate status == "Rascunho"
//   3. Distribute additional costs across items (by selected method)
//   4. For each item:
//      a. Load variant
//      b. Calculate effective unit cost
//      c. Apply weighted average formula
//      d. Update variant.PurchaseCost and variant.Stock
//      e. Create ProductCostHistory record
//      f. Create StockMovement (type: Entrada)
//   5. Update product-level PurchaseCost as weighted average across variants
//   6. Set PO status = "Recebido", ReceivedAt = now
//   7. Save in transaction
//   8. Broadcast SignalR changes
```

- [ ] **Step 1:** Create `CostCalculationService.cs` with full implementation
- [ ] **Step 2:** Register in `Program.cs`: `builder.Services.AddScoped<ICostCalculationService, CostCalculationService>();`
- [ ] **Step 3:** Add `POST /api/orders/{id}/recalculate-costs` endpoint to `OrdersController`
- [ ] **Step 4:** `dotnet build` — verify 0 errors
- [ ] **Step 5:** Commit: `feat: add CostCalculationService with weighted average and sale cost calculation`

---

### Task 2.2: PurchaseOrdersController + InventoryController (Agent: `po-inventory-api`)

**Files:**
- Create: `src/PeruShopHub.API/Controllers/PurchaseOrdersController.cs`
- Create: `src/PeruShopHub.API/Controllers/InventoryController.cs`

**PurchaseOrdersController endpoints:**
- `GET /api/purchase-orders?page=1&pageSize=20&status=&supplier=&sortBy=createdAt&sortDir=desc` → `PagedResult<PurchaseOrderListDto>`
- `GET /api/purchase-orders/{id}` → `PurchaseOrderDetailDto`
- `POST /api/purchase-orders` → create PO from `CreatePurchaseOrderDto`
- `PUT /api/purchase-orders/{id}` → update draft PO
- `POST /api/purchase-orders/{id}/receive` → inject `ICostCalculationService`, call `ReceivePurchaseOrderAsync`
- `POST /api/purchase-orders/{id}/costs` → add additional cost
- `DELETE /api/purchase-orders/{id}/costs/{costId}` → remove additional cost
- `GET /api/purchase-orders/{id}/cost-preview?value={amount}&method={method}` → return distribution preview

**InventoryController endpoints:**
- `GET /api/inventory` → aggregate from ProductVariant.Stock (grouped by product)
- `GET /api/inventory/movements?productId=&type=&dateFrom=&dateTo=&page=&pageSize=` → `PagedResult<StockMovementDto>`
- `POST /api/inventory/adjust` → create StockMovement (Ajuste) + update variant stock

Cost distribution preview logic:
```csharp
// by_value: item.allocation = additionalCostValue * (item.totalCost / sumOfAllItemTotals)
// by_quantity: item.allocation = additionalCostValue * (item.quantity / sumOfAllQuantities)
// manual: not computed server-side (frontend sends explicit allocations)
```

- [ ] **Step 1:** Create `PurchaseOrdersController.cs` with all endpoints
- [ ] **Step 2:** Create `InventoryController.cs` with all endpoints
- [ ] **Step 3:** `dotnet build` — verify 0 errors
- [ ] **Step 4:** Commit: `feat: add PurchaseOrders and Inventory API controllers`

---

### Task 2.3: Settings + Product Cost History Endpoints (Agent: `settings-history-api`)

**Files:**
- Modify: `src/PeruShopHub.API/Controllers/SettingsController.cs` — add commission rules CRUD
- Modify: `src/PeruShopHub.API/Controllers/ProductsController.cs` — add cost-history endpoint

**SettingsController additions:**
- `GET /api/settings/commission-rules` → list all `CommissionRuleDto`
- `POST /api/settings/commission-rules` → create/update rule
- `DELETE /api/settings/commission-rules/{id}` → delete (only if not IsDefault)
- Extend `GET /api/settings/costs` to also return `taxRate` (from appsettings or DB)
- `PUT /api/settings/costs` → update tax rate and default packaging cost

**ProductsController addition:**
- `GET /api/products/{id}/cost-history?page=1&pageSize=20` → `PagedResult<ProductCostHistoryDto>` (newest first)

- [ ] **Step 1:** Add commission rules endpoints to `SettingsController`
- [ ] **Step 2:** Add cost-history endpoint to `ProductsController`
- [ ] **Step 3:** `dotnet build` — verify 0 errors
- [ ] **Step 4:** Commit: `feat: add commission rules and product cost history endpoints`

---

## Phase 2 Merge

- [ ] Merge all 3 worktree branches into `ralph/backend-wiring`
- [ ] Resolve any Program.cs conflicts (add `ICostCalculationService` registration)
- [ ] `dotnet build` — verify 0 errors

---

## Phase 3 — Frontend (3 Parallel Agents)

### Task 3.1: Purchase Order Pages (Agent: `frontend-po`)

**Files to create:**
- `src/PeruShopHub.Web/src/app/services/purchase-order.service.ts`
- `src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-orders-list.component.ts` (+html, +scss)
- `src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-order-form.component.ts` (+html, +scss)
- `src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-order-detail.component.ts` (+html, +scss)

**Files to modify:**
- `src/PeruShopHub.Web/src/app/app.routes.ts` — add `/compras`, `/compras/novo`, `/compras/{id}` routes
- `src/PeruShopHub.Web/src/app/shared/components/sidebar/sidebar.component.ts` — add "Compras" nav item in CATALOGO group (after Suprimentos, before Financeiro)

**PurchaseOrderService methods:**
- `list(params)` → `GET /api/purchase-orders`
- `getById(id)` → `GET /api/purchase-orders/{id}`
- `create(dto)` → `POST /api/purchase-orders`
- `update(id, dto)` → `PUT /api/purchase-orders/{id}`
- `receive(id)` → `POST /api/purchase-orders/{id}/receive`
- `addCost(poId, cost)` → `POST /api/purchase-orders/{id}/costs`
- `removeCost(poId, costId)` → `DELETE /api/purchase-orders/{id}/costs/{costId}`
- `previewCostDistribution(poId, value, method)` → `GET /api/purchase-orders/{id}/cost-preview`

**PO List page** (`/compras`):
- DataTable with columns: Fornecedor, Status (badge), Itens, Total (BRL), Data, Recebido em
- Filter by status, search by supplier
- Button "Nova Compra" → `/compras/novo`

**PO Form page** (`/compras/novo`):
- Supplier field
- Products section: search product by name/SKU, select variant, set quantity and unit cost. Add multiple products.
- Additional costs section: add cost line (description, value), select distribution method (by_value/by_quantity/manual)
- Preview panel: shows per-item allocation of additional costs in real-time (calls preview endpoint on change)
- Summary: subtotal + additional costs = total
- Save button → creates PO, redirects to detail

**PO Detail page** (`/compras/{id}`):
- PO header: supplier, status, dates, notes
- Items table: product, SKU, qty, unit cost, allocated additional, effective unit cost
- Additional costs table
- If status == Rascunho: "Receber Estoque" button (calls receive, shows loading, then shows result)
- After receive: show cost change summary (previous → new cost per product)

Follow existing Angular patterns: standalone components, signals, inject(HttpClient), BRL formatting.

- [ ] **Step 1:** Create `PurchaseOrderService`
- [ ] **Step 2:** Create PO list page
- [ ] **Step 3:** Create PO form page with cost distribution preview
- [ ] **Step 4:** Create PO detail page with receive flow
- [ ] **Step 5:** Add routes and sidebar nav item
- [ ] **Step 6:** `ng build` — verify 0 errors
- [ ] **Step 7:** Commit: `feat: add purchase order pages with cost distribution preview`

---

### Task 3.2: Inventory Page Rewiring + Product Cost History (Agent: `frontend-inventory`)

**Files to modify:**
- `src/PeruShopHub.Web/src/app/pages/inventory/inventory.component.ts` — remove all mocks, wire to API
- `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts` — add cost history section
- `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html` — cost history table

**Files to create:**
- `src/PeruShopHub.Web/src/app/services/inventory.service.ts`

**InventoryService methods:**
- `getInventory()` → `GET /api/inventory`
- `getMovements(params)` → `GET /api/inventory/movements`
- `adjust(dto)` → `POST /api/inventory/adjust`

**Inventory page rewiring:**
- Remove `INITIAL_INVENTORY` and `INITIAL_MOVEMENTS` mock constants
- Inject `InventoryService`
- Load overview data from `inventoryService.getInventory()`
- Load movements from `inventoryService.getMovements()`
- Stock entry modal calls `adjust()` (or links to PO flow for proper purchases)
- KPIs computed from real data

**Product detail cost history:**
- Add "Historico de Custos" section below existing content
- Load from `productService` (add `getCostHistory(id)` method calling `GET /api/products/{id}/cost-history`)
- Table columns: Data, Ref. Compra, Qtd, Custo Unitário Pago, Custo Anterior, Novo Custo
- Current weighted average cost shown as KPI card (already exists as part of product data)

- [ ] **Step 1:** Create `InventoryService`
- [ ] **Step 2:** Rewire inventory page (remove mocks)
- [ ] **Step 3:** Add cost history to product detail page
- [ ] **Step 4:** `ng build` — verify 0 errors
- [ ] **Step 5:** Commit: `feat: wire inventory page and add product cost history grid`

---

### Task 3.3: Sale Detail Enhancements + Settings Update (Agent: `frontend-sale-settings`)

**Files to modify:**
- `src/PeruShopHub.Web/src/app/pages/sales/sale-detail.component.ts` — add source badges + recalculate button
- `src/PeruShopHub.Web/src/app/pages/sales/sale-detail.component.html` — source badge rendering
- `src/PeruShopHub.Web/src/app/services/order.service.ts` — add `recalculateCosts(orderId)`
- `src/PeruShopHub.Web/src/app/pages/settings/settings.component.ts` — add commission rules tab
- `src/PeruShopHub.Web/src/app/pages/settings/settings.component.html` — commission rules CRUD

**Sale detail changes:**
- Each cost item shows a source badge: "Calculado" (green badge), "Manual" (blue), "API" (purple)
- New "Recalcular Custos" button in the cost breakdown header
- Button calls `orderService.recalculateCosts(orderId)` then reloads order detail
- Loading state during recalculation

**OrderService addition:**
```typescript
recalculateCosts(orderId: string): Observable<any> {
  return this.http.post(`/api/orders/${orderId}/recalculate-costs`, {});
}
```

**Settings page additions:**
- New tab or section: "Regras de Comissão"
- Table: Marketplace, Categoria, Tipo Anúncio, Taxa (%), Padrão
- Add/edit/delete commission rules
- Tax rate field in costs section (already partially exists)

- [ ] **Step 1:** Add source badges to sale detail
- [ ] **Step 2:** Add recalculate button + service method
- [ ] **Step 3:** Add commission rules to settings page
- [ ] **Step 4:** `ng build` — verify 0 errors
- [ ] **Step 5:** Commit: `feat: add cost source badges, recalculate button, and commission rules settings`

---

## Phase 3 Merge

- [ ] Merge all 3 worktree branches into `ralph/backend-wiring`
- [ ] Resolve any conflicts (app.routes.ts, sidebar.component.ts)
- [ ] `ng build` — verify 0 errors
- [ ] `dotnet build` — verify 0 errors

---

## Phase 4 — Cleanup + Review

### Task 4.1: Cleanup

- [ ] Search for remaining mock data in inventory page: `grep -r "INITIAL_INVENTORY\|INITIAL_MOVEMENTS" src/PeruShopHub.Web/src/app/`
- [ ] `dotnet build` — 0 errors
- [ ] `ng build` — 0 errors
- [ ] Commit any fixes

### Task 4.2: Team Lead Review

Dispatch `code-reviewer` agent to verify:
- [ ] Weighted average formula is correct in `CostCalculationService`
- [ ] PO receipt is transactional (atomic stock + cost update)
- [ ] Sale cost calculation covers all 5 categories (product_cost, packaging, commission, fixed_fee, tax)
- [ ] Commission rule lookup uses fallback chain (specific → category default → marketplace default)
- [ ] Product cost history is append-only
- [ ] Stock movements created for PO receipt and adjustments
- [ ] All monetary fields use `decimal` / `NUMERIC(18,4)`
- [ ] SignalR broadcasts on product cost change
- [ ] "Calculated" costs preserved on recalculation, "Manual" costs untouched
- [ ] Default variant is hidden from UI
- [ ] No orphan endpoints or service methods
