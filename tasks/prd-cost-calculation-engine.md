# PRD: Motor de Calculo de Custos (Cost Calculation Engine)

## Introduction

Replace pre-calculated seed data with a dynamic cost calculation engine that computes true per-sale profitability. The system has **two distinct cost domains**:

1. **Product Cost (Custo do Produto)** — weighted average cost computed automatically from stock purchases. Never manually edited. When stock is purchased, the new cost is calculated as: `New Cost = ((Current Qty × Current Cost) + Purchase Total) / (Current Qty + Purchase Qty)`. When stock is sold, quantity decreases but cost doesn't change.

2. **Sale Cost (Custo da Venda)** — decomposition of all costs when a sale happens. Includes the product cost (from domain 1) plus marketplace commission, shipping, taxes, packaging, etc. Calculated automatically when an order is created, with the ability to re-trigger if cost rules change.

This is the core product differentiator — no existing ERP/hub calculates true net profit per sale considering all cost categories.

## Goals

- Implement weighted average cost calculation triggered by stock purchases (Purchase Order flow)
- Support additional purchase costs (shipping, import duties, customs) distributed across PO line items
- Auto-calculate all sale cost categories when an order is created
- Provide a cost history grid on product pages showing how cost evolved over time
- Wire the inventory page (currently mock data) to real backend endpoints
- Maintain financial precision: `decimal` / `NUMERIC(18,4)` everywhere

## User Stories

### US-001: Purchase Order Entity and Creation
**Description:** As a seller, I want to register stock purchases so the system can track how much I paid for each product.

**Acceptance Criteria:**
- [ ] New entities: `PurchaseOrder` (id, supplier, status, notes, createdAt) and `PurchaseOrderItem` (id, purchaseOrderId, productId, productVariantId, quantity, unitCost, totalCost)
- [ ] `PurchaseOrder` statuses: Rascunho (Draft), Recebido (Received), Cancelado (Cancelled)
- [ ] `POST /api/purchase-orders` creates a PO with line items
- [ ] `GET /api/purchase-orders` lists POs (paginated, filterable by status, supplier)
- [ ] `GET /api/purchase-orders/{id}` returns PO detail with items
- [ ] `PUT /api/purchase-orders/{id}` updates a draft PO
- [ ] Each line item references a product (and optionally a specific variant)
- [ ] PO can contain multiple products in a single purchase
- [ ] EF Core migration generated and applied

---

### US-002: Additional Purchase Costs
**Description:** As a seller, I want to register additional costs on a purchase (shipping, import duties, customs fees) and choose how to distribute them across line items, so the product cost accurately reflects what I actually paid.

**Acceptance Criteria:**
- [ ] New entity: `PurchaseOrderCost` (id, purchaseOrderId, description, value, distributionMethod)
- [ ] Distribution methods: `by_value` (proportional to line item total), `by_quantity` (proportional to units), `manual` (user specifies per item)
- [ ] `POST /api/purchase-orders/{id}/costs` adds an additional cost
- [ ] `GET /api/purchase-orders/{id}/costs` lists additional costs for a PO
- [ ] `DELETE /api/purchase-orders/{id}/costs/{costId}` removes an additional cost
- [ ] API returns a preview of how each distribution method would allocate the cost across line items before saving
- [ ] `GET /api/purchase-orders/{id}/cost-preview?value={amount}&method={method}` returns per-item distribution preview
- [ ] Total cost per line item = (unitCost × quantity) + allocated additional costs

---

### US-003: Receive Purchase Order (Stock Entry + Cost Recalculation)
**Description:** As a seller, when I receive a purchase, the system should increase stock quantities and automatically recalculate product costs using the weighted average formula.

**Acceptance Criteria:**
- [ ] `POST /api/purchase-orders/{id}/receive` transitions PO from Rascunho → Recebido
- [ ] On receive, for each line item:
  - Calculate effective unit cost = (line item totalCost + allocated additional costs) / quantity
  - Apply weighted average: `newCost = ((currentQty × currentCost) + (purchaseQty × effectiveUnitCost)) / (currentQty + purchaseQty)`
  - Update `Product.PurchaseCost` (or `ProductVariant.PurchaseCost` if variant-specific)
  - Increase `ProductVariant.Stock` by purchased quantity
- [ ] Create a `ProductCostHistory` record for each affected product/variant
- [ ] If PO has zero current stock (first purchase), cost = effective unit cost
- [ ] Receiving is idempotent — cannot receive an already-received PO (returns 409)
- [ ] Broadcast `DataChanged("product", "updated", productId)` via SignalR for each affected product
- [ ] All calculations use `decimal` with precision maintained throughout

---

### US-004: Product Cost History Entity and Tracking
**Description:** As a seller, I want to see how my product cost has changed over time so I can track cost trends and make pricing decisions.

**Acceptance Criteria:**
- [ ] New entity: `ProductCostHistory` (id, productId, variantId?, previousCost, newCost, quantity, unitCostPaid, purchaseOrderId, reason, createdAt)
- [ ] A history record is created every time product cost changes (stock purchase receipt)
- [ ] `GET /api/products/{id}/cost-history` returns cost history (newest first, paginated)
- [ ] Response includes: date, previous cost, new cost, quantity purchased, unit cost paid, PO reference, reason
- [ ] History is append-only — records are never modified or deleted
- [ ] DTOs in `Application/DTOs/Products/ProductCostHistoryDto.cs`

---

### US-005: Sale Cost Auto-Calculation on Order Creation
**Description:** As the system, when an order is created, I want to automatically calculate all cost categories so the order's profit is accurate from the moment it enters the system.

**Acceptance Criteria:**
- [ ] New service interface: `ICostCalculationService` in Core with method `Task<List<OrderCost>> CalculateOrderCostsAsync(Order order, CancellationToken ct)`
- [ ] Implementation in Infrastructure: `CostCalculationService`
- [ ] On order creation (or manual re-trigger), the service computes:
  - **product_cost**: lookup current `Product.PurchaseCost` (or variant's) × quantity
  - **packaging**: lookup `Product.PackagingCost` × quantity (fallback to default from settings)
  - **marketplace_commission**: `order.TotalAmount × commissionRate` (from configurable commission rules)
  - **fixed_fee**: applied if item price < R$79 (tiered: R$6.25/6.50/6.75 based on price range)
  - **tax**: `order.TotalAmount × taxRate` (single configurable Simples Nacional rate from settings)
- [ ] Each computed cost is saved as an `OrderCost` record with `Source = "Calculated"`
- [ ] `Order.Profit` is updated: `TotalAmount - SUM(all OrderCost values)`
- [ ] Existing manually-entered costs (Source = "Manual") are preserved — only "Calculated" costs are replaced on recalculation
- [ ] `POST /api/orders/{id}/recalculate-costs` triggers recalculation for an existing order

---

### US-006: Commission Rules Configuration
**Description:** As an admin, I want to configure marketplace commission rates per category and listing type so the engine uses accurate rates.

**Acceptance Criteria:**
- [ ] New entity: `CommissionRule` (id, marketplaceId, categoryPattern, listingType, rate, isDefault, createdAt)
- [ ] Seed with ML defaults: Classic 11-14%, Premium 16-19% by category (from ML documentation)
- [ ] `GET /api/settings/commission-rules` returns all rules
- [ ] `POST /api/settings/commission-rules` creates/updates a rule
- [ ] `DELETE /api/settings/commission-rules/{id}` removes a custom rule (falls back to default)
- [ ] Commission lookup logic: match by (marketplace + category + listing type) → (marketplace + category) → (marketplace default) → global default
- [ ] Admin can override any category rate — override takes precedence over defaults
- [ ] Settings page in frontend updated with commission rules section

---

### US-007: Tax Rate Configuration
**Description:** As an admin, I want to set my tax regime and rate so sale costs include taxes.

**Acceptance Criteria:**
- [ ] Extend existing settings endpoint: `GET /api/settings/costs` now also returns `taxRegime` and `taxRate`
- [ ] `PUT /api/settings/costs` updates tax rate, default packaging cost, and other cost defaults
- [ ] For this phase: single global tax rate (Simples Nacional %) applied to all sales
- [ ] Settings page updated with tax rate field
- [ ] Default seed value: 6.0% (Simples Nacional typical rate)

---

### US-008: Purchase Order UI
**Description:** As a seller, I want a UI to create and manage purchase orders, add additional costs, preview cost distribution, and receive stock.

**Acceptance Criteria:**
- [ ] New page: `/compras` (Purchase Orders) — list of POs with status, supplier, total, date
- [ ] Add route to sidebar under CATALOGO group (after Suprimentos)
- [ ] New page: `/compras/novo` — create PO form:
  - Supplier name field
  - Add products: search by name/SKU, select quantity and unit cost
  - Multiple products per PO
  - Additional costs section: add cost (description, value), select distribution method
  - Preview panel showing per-item cost allocation in real-time
  - Total summary: subtotal (products) + additional costs = total
- [ ] New page: `/compras/{id}` — PO detail:
  - PO info (supplier, status, dates)
  - Line items table (product, SKU, qty, unit cost, additional costs allocated, effective unit cost)
  - Additional costs table
  - "Receber" (Receive) button — only visible for Rascunho status
  - After receiving: shows resulting cost changes per product (previous → new cost)
- [ ] Verify in browser: PO creation flow works end-to-end

---

### US-009: Product Cost Grid on Product Detail
**Description:** As a seller, I want to see the cost history on the product detail page so I can understand how costs have evolved.

**Acceptance Criteria:**
- [ ] Product detail page (`/produtos/{id}`) shows a "Historico de Custos" section
- [ ] Cost history table: Date, PO Reference, Qty Purchased, Unit Cost Paid, Additional Costs, Previous Cost, New Cost
- [ ] Current weighted average cost prominently displayed as a KPI card
- [ ] Data loaded from `GET /api/products/{id}/cost-history`
- [ ] Empty state message when no purchase history exists
- [ ] Verify in browser: cost history displays correctly

---

### US-010: Wire Inventory Page to Backend
**Description:** As a seller, I want the inventory page to show real stock data from the backend instead of mock data.

**Acceptance Criteria:**
- [ ] New entity: `StockMovement` (id, productId, variantId?, type, quantity, unitCost?, purchaseOrderId?, orderId?, reason, createdBy, createdAt)
- [ ] Movement types: Entrada (from PO receipt), Saida (from sale), Ajuste (manual adjustment)
- [ ] `GET /api/inventory` returns current stock per product/variant (aggregated from ProductVariant.Stock)
- [ ] `GET /api/inventory/movements?productId=&type=&dateFrom=&dateTo=` returns stock movements (paginated)
- [ ] `POST /api/inventory/adjust` allows manual stock adjustment (creates StockMovement + updates variant stock)
- [ ] Inventory page rewired from mock data to API calls
- [ ] Remove `INITIAL_INVENTORY` and `INITIAL_MOVEMENTS` mock constants from inventory.component.ts
- [ ] KPIs (Total SKUs, Units in Stock, Critical Items, Stock Value) computed from real data
- [ ] Stock movements automatically created when PO is received (Entrada) or order is created (Saida)
- [ ] Verify in browser: inventory page shows real data

---

### US-011: Sale Cost Breakdown Enhancement in Sale Detail
**Description:** As a seller, I want the sale detail page to show both auto-calculated costs and their source so I understand where each cost comes from.

**Acceptance Criteria:**
- [ ] Sale detail page shows cost source badge: "Calculado" (green), "Manual" (blue), "API" (purple)
- [ ] Cost breakdown includes the product cost line (from weighted average at time of sale)
- [ ] "Recalcular" button triggers `POST /api/orders/{id}/recalculate-costs` and refreshes the breakdown
- [ ] Profit and margin update in real-time after recalculation
- [ ] Existing manual cost entry/edit functionality preserved (manual costs are not overwritten by recalculation)
- [ ] Verify in browser: sale detail shows source badges and recalculate button

## Functional Requirements

- FR-1: Purchase Order CRUD with line items and additional costs
- FR-2: Weighted average cost formula: `((currentQty × currentCost) + purchaseTotal) / (currentQty + purchaseQty)` — applied on PO receipt
- FR-3: Additional purchase costs distributed by value, by quantity, or manual allocation — with preview before saving
- FR-4: Product cost is NEVER manually editable — only changes through stock purchases
- FR-5: Product cost history is append-only — every change creates a record
- FR-6: Sale cost auto-calculation on order creation: product_cost, packaging, marketplace_commission, fixed_fee, tax
- FR-7: Commission rules configurable per marketplace/category/listing type with ML defaults seeded
- FR-8: Tax rate configurable globally (single rate for Simples Nacional)
- FR-9: Sale cost recalculation can be triggered manually per order
- FR-10: "Calculated" costs are replaced on recalculation; "Manual" costs are preserved
- FR-11: Stock movements tracked for every stock change (PO receipt, sale, adjustment)
- FR-12: Inventory page wired to real data (remove mock data)
- FR-13: All monetary calculations use `decimal` / `NUMERIC(18,4)` — never float/double
- FR-14: SignalR broadcasts on product cost change and stock movement

## Non-Goals

- **No ML Billing API integration** — commission rates are configured manually (or from seeded defaults), not fetched from ML API. ML API integration is a separate phase.
- **No shipping cost calculation** — shipping costs for sales remain manual or API-sourced in a future phase
- **No payment fee calculation** — Mercado Pago fees are not calculated in this phase
- **No fulfillment/storage cost calculation** — ML Full costs are out of scope
- **No advertising cost attribution** — ad spend per sale is a future phase
- **No coupon absorption calculation** — handled in ML integration phase
- **No multi-currency support** — all costs in BRL
- **No purchase order approval workflow** — no approval chain, any user can create and receive
- **No supplier management CRUD** — supplier is a text field on PO, not a separate entity
- **No cost simulator/what-if page** — deferred to a future phase

## Technical Considerations

- **Existing entities to extend**: `Product.PurchaseCost`, `ProductVariant.PurchaseCost`, `ProductVariant.Stock`
- **New entities**: `PurchaseOrder`, `PurchaseOrderItem`, `PurchaseOrderCost`, `ProductCostHistory`, `StockMovement`, `CommissionRule`
- **New EF Core migration** required for all new entities
- **`ICostCalculationService`** in Core (interface), implementation in Infrastructure — allows future replacement with ML API-sourced data
- **Commission rule lookup**: most-specific-match pattern (marketplace + category + listing type → fallback chain)
- **Stock at variant level**: `ProductVariant.Stock` is the source of truth. Products without variants should still have a "default" variant for stock tracking, or product-level stock is computed as SUM(variants).
- **Concurrency**: PO receipt should use a transaction to ensure stock + cost update is atomic
- **Seed data**: Update existing seed to include a few PurchaseOrders with cost history so the system has sample data
- **Inventory page**: Currently has mock data (`INITIAL_INVENTORY`, `INITIAL_MOVEMENTS` in `inventory.component.ts`) — needs full rewiring like other pages in Phase B

## Success Metrics

- Product cost automatically recalculates when a PO is received — no manual cost entry needed
- Sale detail shows auto-calculated costs with correct source badges
- Profit = Revenue - SUM(costs) holds for every order
- Cost history shows full audit trail of cost changes per product
- Inventory page displays real stock data and movement history

## Resolved Questions

1. **Product cost formula**: Weighted average — `((currentQty × currentCost) + purchaseTotal) / (currentQty + purchaseQty)`. Cost never changes on sale (only quantity decreases).
2. **Additional purchase costs**: User selects distribution method (by value, by quantity, or manual) with preview before confirming.
3. **Tax calculation**: Single global Simples Nacional rate (default 6%) for this phase. Complex tax rules deferred.
4. **Commission rules**: Ship with ML defaults, admin can override per category.
5. **Cost visibility**: Sale detail page + product detail (cost grid) + inventory page.
6. **Manual cost editing**: Product cost is NEVER manually edited. Only changes through stock purchases. Sale costs can have manual entries alongside calculated ones.

## Resolved Questions (continued)

7. **Cost tracking level**: Always at the **variant level**. `ProductVariant.PurchaseCost` and `ProductVariant.Stock` are the source of truth. `Product.PurchaseCost` is an **aggregate** (weighted average across all variants) — computed, not stored separately. PO items always target a variant.
8. **Products without variants**: Auto-create an internal "default" variant when a product has no user-created variants. This default variant is **hidden from the UI** — the user sees the product as having no variants. Internally, stock and cost are tracked on this hidden default variant. When the user creates their first real variant, stock/cost must be migrated from the default.
9. **Default variant behavior**: `ProductVariant.IsDefault` (new bool field) = true for auto-created variants. Frontend filters these out from variant lists, product detail, and variant managers. Backend always operates on variants.

## Open Questions

_None remaining._
