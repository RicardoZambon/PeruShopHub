# Stock Management Completeness — Design Spec

> Date: 2026-03-27

## Goal

Close all gaps in the product stock/cost management lifecycle so that:
- Stock automatically decreases when sales happen
- Stock automatically increases when purchase orders are received (already done)
- Product cost is always current via weighted average (already done)
- Product pages show cost and stock information
- Purchase orders can be edited and cancelled
- The entire purchase → stock → sale → profit cycle is traceable

## What's Already Done

- PO receipt → auto stock increase + weighted avg cost + cost history + stock movements
- Manual stock adjustments via inventory page
- Inventory overview with pagination, movements history
- PO creation with cost distribution (by_value, by_quantity)

## What's Missing

### 1. Auto Stock Deduction on Sales

**Problem:** When an order status changes to "Pago" (paid), stock should decrease. Currently no stock movements of Type="Saída" are created.

**Design:**
- Add `POST /api/orders/{id}/fulfill` endpoint that:
  - Creates StockMovement (Type="Saída") for each order item
  - Decreases ProductVariant.Stock by item quantity
  - Links movement to orderId
- Alternatively, trigger this automatically when order status transitions to "Pago" or "Enviado"
- For the MVP, add a "Fulfill" button on order detail that triggers stock deduction
- Guard against double-fulfillment (check if movements already exist for this order)

### 2. PO Edit Support

**Problem:** Backend PUT endpoint exists but frontend only has create form.

**Design:**
- Reuse PurchaseOrderFormComponent for edit mode (load existing PO data)
- Route: `compras/:id/editar` → same component with edit param
- Only allow editing "Rascunho" (draft) status POs
- PO detail page shows "Editar" button only for drafts

### 3. PO Cancellation

**Problem:** No way to cancel a draft PO.

**Design:**
- Add `DELETE /api/purchase-orders/{id}` that soft-deletes by setting status to "Cancelado"
- Only "Rascunho" status can be cancelled
- Frontend: add "Cancelar" button on PO detail for drafts
- Use ConfirmDialog before cancellation

### 4. Product Detail — Cost & Stock Section

**Problem:** Product detail page doesn't show cost, stock, or purchase history.

**Design:**
- Add a "Estoque & Custos" section to product detail page showing:
  - Current stock (total across variants)
  - Purchase cost (weighted average)
  - Packaging cost
  - Total cost per unit
  - Stock value (stock × cost)
- Add cost history table (from existing `/api/products/{id}/cost-history` endpoint)
- Per-variant stock breakdown if product has variants

### 5. Sale Fulfillment Status Tracking

**Problem:** No way to know if an order's stock has been deducted.

**Design:**
- Add `IsFulfilled` boolean to Order entity
- Set to true after successful stock deduction
- Show fulfillment status on order detail page
- Prevent double-fulfillment

## Out of Scope

- Multi-warehouse stock allocation
- Reserved stock (will come with ML integration)
- Automatic fulfillment on webhook receipt (ML integration phase)
- Partial PO receipt
