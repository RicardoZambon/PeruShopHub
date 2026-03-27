# GridFooter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a sticky minimal footer to DataGridComponent showing loaded vs total count with optional custom content projection.

**Architecture:** New `totalCount` and `entityName` inputs on DataGridComponent, a `GridFooterDirective` for content projection, and a sticky footer bar rendered outside the scroll wrapper. All 6 existing consumers wire up `totalCount` from their existing `response.totalCount` data.

**Tech Stack:** Angular 17+ (standalone components, signals, OnPush), SCSS with CSS custom properties

**Spec:** `docs/superpowers/specs/2026-03-25-grid-footer-design.md`

---

### Task 1: Add GridFooterDirective and footer inputs to DataGridComponent

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/shared/components/data-grid/data-grid.component.ts`

- [ ] **Step 1: Add the GridFooterDirective**

Add after the `GridCardDirective` class (after line 81):

```typescript
@Directive({
  selector: '[appGridFooter]',
  standalone: true,
})
export class GridFooterDirective {
  constructor(public templateRef: TemplateRef<void>) {}
}
```

- [ ] **Step 2: Add new inputs and ContentChildren query**

In the `DataGridComponent` class, add after `@Input() ariaLabel` (line 100):

```typescript
@Input() totalCount = 0;
@Input() entityName = 'registros';
```

Add after the `@ContentChildren(GridCardDirective)` line (line 109):

```typescript
@ContentChildren(GridFooterDirective) footerTemplates!: QueryList<GridFooterDirective>;
```

- [ ] **Step 3: Add computed properties for footer**

Add after the `cardTemplate` getter (after line 209):

```typescript
get footerTemplate(): TemplateRef<void> | null {
  return this.footerTemplates?.first?.templateRef ?? null;
}

get showFooter(): boolean {
  return this.totalCount > 0 && !this.showSkeleton;
}

get allLoaded(): boolean {
  return this.data.length >= this.totalCount;
}
```

Note: footer text formatting is done in the HTML template (Task 2) to allow `<strong>` tags around the numbers.

- [ ] **Step 4: Export the new directive**

Ensure `GridFooterDirective` is exported from the file (it's already exported via the `export class` declaration — just verify the file compiles).

- [ ] **Step 5: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors related to data-grid.component.ts

- [ ] **Step 6: Commit**

```bash
git add src/PeruShopHub.Web/src/app/shared/components/data-grid/data-grid.component.ts
git commit -m "feat: add GridFooterDirective and footer inputs to DataGridComponent"
```

---

### Task 2: Add footer HTML to DataGridComponent template

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/shared/components/data-grid/data-grid.component.html`

- [ ] **Step 1: Add footer section after the scroll wrapper closing div**

Insert after the `</div>` that closes `.data-grid__scroll-wrapper` (line 157) and before the closing `</div>` of `.data-grid` (line 158):

```html
  <!-- Footer with count and optional custom content -->
  <div *ngIf="showFooter" class="data-grid__footer">
    <span class="data-grid__footer-count">
      <ng-container *ngIf="allLoaded; else partialCount">
        Mostrando todos os <strong>{{ totalCount | number:'1.0-0':'pt-BR' }}</strong> {{ entityName }}
      </ng-container>
      <ng-template #partialCount>
        Mostrando <strong>{{ data.length | number:'1.0-0':'pt-BR' }}</strong> de <strong>{{ totalCount | number:'1.0-0':'pt-BR' }}</strong> {{ entityName }}
      </ng-template>
    </span>
    <ng-container *ngIf="footerTemplate as customFooter">
      <div class="data-grid__footer-custom">
        <ng-container *ngTemplateOutlet="customFooter"></ng-container>
      </div>
    </ng-container>
  </div>
```

The footer is **outside** the scroll wrapper so it stays sticky at the bottom.

- [ ] **Step 2: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/shared/components/data-grid/data-grid.component.html
git commit -m "feat: add footer HTML to DataGridComponent template"
```

---

### Task 3: Add footer styles to DataGridComponent

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/shared/components/data-grid/data-grid.component.scss`

- [ ] **Step 1: Add footer styles**

Add before the closing `}` of the `.data-grid` block (before line 282, after `&__spinner`):

```scss
  &__footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: var(--space-2) var(--space-4);
    border-top: 1px solid var(--neutral-100);
    background: var(--surface);
    font-size: var(--text-body-small);
    color: var(--neutral-500);
    flex-shrink: 0;

    strong {
      color: var(--neutral-700);
      font-weight: 600;
    }
  }

  &__footer-count {
    white-space: nowrap;
  }

  &__footer-custom {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    color: var(--neutral-600);
  }
```

- [ ] **Step 2: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/shared/components/data-grid/data-grid.component.scss
git commit -m "feat: add footer styles to DataGridComponent"
```

---

### Task 4: Wire up totalCount in Customers component

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/customers/customers.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/customers/customers.component.html`

- [ ] **Step 1: Add totalCount signal**

In `customers.component.ts`, add after `readonly hasMore = signal(true);` (line 35):

```typescript
readonly totalCount = signal(0);
```

- [ ] **Step 2: Set totalCount from response**

In the `loadCustomers` method, after `this.hasMore.set(totalLoaded < response.totalCount);` (line 90), add:

```typescript
this.totalCount.set(response.totalCount);
```

- [ ] **Step 3: Add totalCount and entityName bindings to template**

In `customers.component.html`, add to the `<app-data-grid>` element (after the `[hasMore]` binding on line 28):

```html
[totalCount]="totalCount()"
entityName="clientes"
```

- [ ] **Step 4: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/customers/customers.component.ts src/PeruShopHub.Web/src/app/pages/customers/customers.component.html
git commit -m "feat: wire up grid footer totalCount in Customers"
```

---

### Task 5: Wire up totalCount in Products component

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.html`

- [ ] **Step 1: Add totalCount signal**

In `products-list.component.ts`, add after `readonly hasMore = signal(true);` (line 53):

```typescript
readonly totalCount = signal(0);
```

- [ ] **Step 2: Set totalCount from response**

After `this.hasMore.set(totalLoaded < result.totalCount);` (line 120), add:

```typescript
this.totalCount.set(result.totalCount);
```

- [ ] **Step 3: Add totalCount and entityName bindings to template**

In `products-list.component.html`, add to the `<app-data-grid>` element (after the `[hasMore]` binding):

```html
[totalCount]="totalCount()"
entityName="produtos"
```

- [ ] **Step 4: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/products/products-list.component.ts src/PeruShopHub.Web/src/app/pages/products/products-list.component.html
git commit -m "feat: wire up grid footer totalCount in Products"
```

---

### Task 6: Wire up totalCount in Inventory component (both grids)

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/inventory/inventory.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/inventory/inventory.component.html`

- [ ] **Step 1: Add totalCount signals for both grids**

In `inventory.component.ts`, add after `invSearch = signal('');` (line 62):

```typescript
invTotalCount = signal(0);
```

Add after `movHasMore = signal(true);` (line 82):

```typescript
movTotalCount = signal(0);
```

- [ ] **Step 2: Set invTotalCount from inventory response**

After `this.invHasMore.set(totalLoaded < result.totalCount);` (line 209), add:

```typescript
this.invTotalCount.set(result.totalCount);
```

- [ ] **Step 3: Set movTotalCount from movements response**

After `this.movHasMore.set(totalLoaded < result.totalCount);` (line 278), add:

```typescript
this.movTotalCount.set(result.totalCount);
```

- [ ] **Step 4: Add totalCount and entityName bindings to inventory grid**

In `inventory.component.html`, add to the first `<app-data-grid>` (inventory overview, after the `[hasMore]` binding around line 45):

```html
[totalCount]="invTotalCount()"
entityName="itens"
```

- [ ] **Step 5: Add totalCount and entityName bindings to movements grid**

In `inventory.component.html`, add to the second `<app-data-grid>` (movements, after the `[hasMore]` binding around line 198):

```html
[totalCount]="movTotalCount()"
entityName="movimentações"
```

- [ ] **Step 6: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 7: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/inventory/inventory.component.ts src/PeruShopHub.Web/src/app/pages/inventory/inventory.component.html
git commit -m "feat: wire up grid footer totalCount in Inventory (overview + movements)"
```

---

### Task 7: Wire up totalCount in Purchase Orders component

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-orders-list.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-orders-list.component.html`

- [ ] **Step 1: Add totalCount signal**

In `purchase-orders-list.component.ts`, add after `readonly hasMore = signal(true);` (line 47):

```typescript
readonly totalCount = signal(0);
```

- [ ] **Step 2: Set totalCount from response**

After `this.hasMore.set(totalLoaded < response.totalCount);` (line 100), add:

```typescript
this.totalCount.set(response.totalCount);
```

- [ ] **Step 3: Add totalCount and entityName bindings to template**

In `purchase-orders-list.component.html`, add to the `<app-data-grid>` element (after the `[hasMore]` binding):

```html
[totalCount]="totalCount()"
entityName="pedidos"
```

- [ ] **Step 4: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-orders-list.component.ts src/PeruShopHub.Web/src/app/pages/purchase-orders/purchase-orders-list.component.html
git commit -m "feat: wire up grid footer totalCount in Purchase Orders"
```

---

### Task 8: Wire up totalCount in Sales component

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/sales/sales-list.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/sales/sales-list.component.html`

- [ ] **Step 1: Add totalCount signal**

In `sales-list.component.ts`, add after `readonly hasMore = signal(true);` (line 55):

```typescript
readonly totalCount = signal(0);
```

- [ ] **Step 2: Set totalCount from response**

After `this.hasMore.set(totalLoaded < response.totalCount);` (line 117), add:

```typescript
this.totalCount.set(response.totalCount);
```

- [ ] **Step 3: Add totalCount and entityName bindings to template**

In `sales-list.component.html`, add to the `<app-data-grid>` element (after the `[hasMore]` binding):

```html
[totalCount]="totalCount()"
entityName="vendas"
```

- [ ] **Step 4: Run typecheck**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/sales/sales-list.component.ts src/PeruShopHub.Web/src/app/pages/sales/sales-list.component.html
git commit -m "feat: wire up grid footer totalCount in Sales"
```

---

### Task 9: Final build verification

**Files:** None (verification only)

- [ ] **Step 1: Full build**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development`
Expected: Build succeeds with no errors

- [ ] **Step 2: Verify all grid consumers export correctly**

Run: `cd src/PeruShopHub.Web && grep -rn 'totalCount' src/app/pages/ --include='*.ts' | grep 'signal'`
Expected: 7 matches (1 per consumer, 2 for inventory)

- [ ] **Step 3: Verify all templates bind totalCount**

Run: `cd src/PeruShopHub.Web && grep -rn 'totalCount' src/app/ --include='*.html'`
Expected: 7 matches (1 per grid instance)
