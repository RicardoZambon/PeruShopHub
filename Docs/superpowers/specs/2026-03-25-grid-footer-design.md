# GridFooter Feature Design

**Date:** 2026-03-25
**Component:** `DataGridComponent` (`app-data-grid`)

## Summary

Add a sticky minimal footer to `DataGridComponent` that shows the count of loaded items vs total available (e.g., "Mostrando 20 de 147 registros"). Supports custom content projection via `appGridFooter` directive.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Entity label | Generic "registros" default + optional `entityName` input | Works out of the box; consumers customize as needed |
| Footer position | Sticky to bottom of grid container | Always visible, minimal footprint |
| Footer content | Count only — no "Página X de Y" | Infinite scroll grids don't have a page concept |
| Content projection | Append mode — count left, custom right | Count is never lost; custom content enriches |

## New Inputs

| Input | Type | Default | Purpose |
|-------|------|---------|---------|
| `totalCount` | `number` | `0` | Total matching records from backend `PagedResult.totalCount` |
| `entityName` | `string` | `'registros'` | Label for the entity type ("produtos", "clientes", etc.) |

## Footer States

| State | Display |
|-------|---------|
| `loading && data.length === 0` | Footer hidden (skeleton placeholders showing) |
| `data.length < totalCount` | "Mostrando **{loaded}** de **{total}** {entityName}" |
| `data.length === totalCount && totalCount > 0` | "Mostrando todos os **{total}** {entityName}" |
| `totalCount === 0 && !loading` | Footer hidden (empty state showing) |

## `appGridFooter` Directive

New content projection directive for custom footer content. Follows the same pattern as existing `appGridCell`, `appGridHeader`, `appGridCard`, and `appGridEmpty` directives.

### Usage

```html
<app-data-grid
  [columns]="gridColumns"
  [data]="gridData()"
  [totalCount]="totalCount()"
  [entityName]="'produtos'"
>
  <ng-template appGridFooter>
    <span>Total em estoque: <strong>R$ 45.230,00</strong></span>
  </ng-template>
</app-data-grid>
```

### Behavior

- Built-in count always renders on the **left** side
- Custom `appGridFooter` content renders on the **right** side
- If no `appGridFooter` is provided, footer shows only the count (left-aligned)
- Footer container uses `display: flex; justify-content: space-between; align-items: center`

## Styling

- **Position**: Sticky to bottom of `.data-grid` container (below scroll wrapper)
- **Background**: `var(--surface)`
- **Border**: `border-top: 1px solid var(--neutral-100)`
- **Font size**: `var(--text-body-small)`
- **Text color**: `var(--neutral-500)` for labels, `var(--neutral-700)` for bold values
- **Padding**: `var(--space-2) var(--space-4)`
- **Mobile**: Same styling, visible below card layout
- **Dark theme**: Inherits correctly via CSS custom properties

## Consumer Migration

All 6 existing grid consumers need updates:

### 1. Customers (`customers.component.ts`)
- Add `totalCount = signal(0)`
- Set from `response.totalCount` in `loadCustomers()`
- Template: `[totalCount]="totalCount()" entityName="clientes"`

### 2. Products (`products-list.component.ts`)
- Add `totalCount = signal(0)`
- Set from `result.totalCount` in `loadProducts()`
- Template: `[totalCount]="totalCount()" entityName="produtos"`

### 3. Inventory Overview (`inventory.component.ts`)
- Add `invTotalCount = signal(0)`
- Set from `result.totalCount` in `loadInventory()`
- Template: `[totalCount]="invTotalCount()" entityName="itens"`

### 4. Inventory Movements (`inventory.component.ts`)
- Add `movTotalCount = signal(0)`
- Set from `result.totalCount` in `loadMovements()`
- Template: `[totalCount]="movTotalCount()" entityName="movimentações"`

### 5. Purchase Orders (`purchase-orders.component.ts`)
- Add `totalCount = signal(0)`
- Set from `response.totalCount` in `loadOrders()`
- Template: `[totalCount]="totalCount()" entityName="pedidos"`

### 6. Sales (`sales.component.ts`)
- Add `totalCount = signal(0)`
- Set from `response.totalCount` in `loadOrders()`
- Template: `[totalCount]="totalCount()" entityName="vendas"`

## Backend Changes

**None required.** All paginated endpoints already return `totalCount` in `PagedResult<T>`. The Angular `PagedResult<T>` interface already includes `totalCount: number`.

## Files to Modify

### DataGridComponent (core changes)
- `data-grid.component.ts` — Add `totalCount` and `entityName` inputs, add `appGridFooter` directive, computed footer text
- `data-grid.component.html` — Add footer section (desktop table + mobile cards)
- `data-grid.component.scss` — Add footer styles

### Consumers (wire up totalCount)
- `customers.component.ts` + `.html`
- `products-list.component.ts` + `.html`
- `inventory.component.ts` + `.html`
- `purchase-orders.component.ts` + `.html`
- `sales.component.ts` + `.html`
