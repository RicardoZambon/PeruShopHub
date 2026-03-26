# SP4: Product View & Analytics — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add missing product fields to view, create Stock and Analytics sections with date range, build backend analytics endpoints, migrate tables to DataGridComponent.

**Architecture:** New backend endpoints for product analytics and recent orders. Frontend restructures detail page into sections (Info, Stock, Analytics) with DataGridComponent for tables.

**Tech Stack:** C# ASP.NET Core 8, EF Core 8, PostgreSQL, Angular 17+, standalone components, signals

**Spec:** `docs/superpowers/specs/2026-03-26-products-overhaul-design.md` (SP4 section)

---

### Task 1: Replace mocked product data with real API call and add missing info sections (4.1)

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss`
- Modify: `src/PeruShopHub.Web/src/app/services/product.service.ts`

**Context:** The `ProductDetailComponent` currently loads product data via a `setTimeout` mock (lines 122-150 of `product-detail.component.ts`). The `ProductService.getById()` method already exists and calls `GET /api/products/{id}` which returns `ProductDetailDto` including description, categoryId, supplier, price, costs, dimensions, photoUrls, and variants. The frontend `Product` interface already has fields for description, categoryId, supplier, price, acquisitionCost (maps to PurchaseCost), weight/height/width/length, and imageUrl.

- [ ] **Step 1: Update the ProductDetail interface to match the API response**

In `product-detail.component.ts`, replace the `ProductDetail` interface (lines 14-27) to include all fields from the backend `ProductDetailDto`:

```typescript
interface ProductDetail {
  id: string;
  name: string;
  sku: string;
  description: string | null;
  categoryId: string | null;
  categoryPath: string | null; // resolved client-side
  supplier: string | null;
  price: number;
  purchaseCost: number;
  packagingCost: number;
  status: string;
  statusVariant: BadgeVariant;
  imageUrl: string | null;
  photoUrls: string[];
  weight: number;
  height: number;
  width: number;
  length: number;
  stock: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}
```

- [ ] **Step 2: Add CategoryService import and inject it**

Add import for `CategoryService` at the top of the file:

```typescript
import { CategoryService } from '../../services/category.service';
```

Add it to the constructor:

```typescript
constructor(
  private route: ActivatedRoute,
  private variantService: ProductVariantService,
  private productService: ProductService,
  private categoryService: CategoryService,
) {}
```

- [ ] **Step 3: Replace the mocked loadData with real API calls**

Replace the `loadData()` method. Remove the `setTimeout` mock and use `ProductService.getById()`:

```typescript
private async loadData(): Promise<void> {
  this.loading.set(true);
  try {
    const p = await this.productService.getById(this.productId);

    // Resolve category breadcrumb path
    let categoryPath: string | null = null;
    if (p.categoryId) {
      categoryPath = await this.resolveCategoryPath(p.categoryId);
    }

    // Compute stock from variants
    const totalStock = (p as any).variants
      ? (p as any).variants.reduce((sum: number, v: any) => sum + (v.stock || 0), 0)
      : 0;

    const statusVariant: BadgeVariant = p.status === 'Ativo' ? 'success'
      : p.status === 'Inativo' ? 'neutral'
      : p.status === 'Pausado' ? 'warning'
      : 'neutral';

    this.product.set({
      id: p.id,
      name: p.name,
      sku: p.sku,
      description: p.description ?? null,
      categoryId: p.categoryId ?? null,
      categoryPath,
      supplier: p.supplier ?? null,
      price: p.price,
      purchaseCost: p.acquisitionCost ?? p.purchaseCost ?? 0,
      packagingCost: p.packagingCost ?? 0,
      status: p.status,
      statusVariant,
      imageUrl: p.imageUrl ?? ((p as any).photoUrls?.[0] ?? null),
      photoUrls: (p as any).photoUrls ?? [],
      weight: p.weight ?? 0,
      height: p.height ?? 0,
      width: p.width ?? 0,
      length: p.length ?? 0,
      stock: totalStock || p.stock || 0,
      isActive: (p as any).isActive ?? true,
      createdAt: (p as any).createdAt ?? '',
      updatedAt: (p as any).updatedAt ?? '',
    });

    // Load variants
    this.variantService.getByProductId(this.productId).then(v => this.variants.set(v));
  } catch (err) {
    console.error('Failed to load product', err);
  } finally {
    this.loading.set(false);
  }

  // Load cost history from API
  this.productService.getCostHistory(this.productId).subscribe({
    next: (result) => this.costHistory.set(result.items),
    error: () => this.costHistory.set([]),
  });
}
```

- [ ] **Step 4: Add the category path resolver helper**

Add this method to the component class:

```typescript
private async resolveCategoryPath(categoryId: string): Promise<string | null> {
  try {
    // Ensure categories are loaded
    if (this.categoryService.allCategories().length === 0) {
      await this.categoryService.loadAll();
    }
    const categories = this.categoryService.allCategories();
    const buildPath = (id: string): string[] => {
      const cat = categories.find(c => c.id === id);
      if (!cat) return [];
      const parentPath = cat.parentId ? buildPath(cat.parentId) : [];
      return [...parentPath, cat.name];
    };
    const path = buildPath(categoryId);
    return path.length > 0 ? path.join(' > ') : null;
  } catch {
    return null;
  }
}
```

- [ ] **Step 5: Update the kpis computed to remove sales/revenue/profit/margin (moved to analytics section in Task 3)**

The `kpis` computed should now only show Stock KPIs. Remove sales/revenue/profit/margin from here — they will be in the Analytics section. Keep `kpis` for Stock only (or remove it entirely since Stock gets its own section in Task 2). For now, remove the old `kpis` computed entirely:

```typescript
// Remove the old kpis computed — replaced by stockKpis (Task 2) and analyticsKpis (Task 3)
```

- [ ] **Step 6: Add computed for estimated margin**

```typescript
estimatedMargin = computed(() => {
  const p = this.product();
  if (!p || p.price <= 0) return null;
  return ((p.price - p.purchaseCost - p.packagingCost) / p.price) * 100;
});

marginClass = computed(() => {
  const m = this.estimatedMargin();
  if (m === null) return '';
  if (m >= 20) return 'value--positive';
  if (m >= 10) return 'value--warning';
  return 'value--negative';
});
```

- [ ] **Step 7: Add the info sections to the template**

In `product-detail.component.html`, after the header section and before the KPI grid, add the new info sections. Replace the existing `<!-- KPI cards -->` section with structured info sections:

```html
<!-- Info sections grid -->
<div class="info-sections">
  <!-- Informações Gerais -->
  <div class="info-card">
    <h2 class="info-card__title">Informações Gerais</h2>
    <div class="info-card__grid">
      <div class="info-card__field">
        <span class="info-card__label">Categoria</span>
        <span class="info-card__value">{{ product()!.categoryPath ?? '—' }}</span>
      </div>
      <div class="info-card__field info-card__field--full">
        <span class="info-card__label">Descrição</span>
        <span class="info-card__value">{{ product()!.description ?? '—' }}</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Fornecedor</span>
        <span class="info-card__value">{{ product()!.supplier ?? '—' }}</span>
      </div>
    </div>
  </div>

  <!-- Preço e Custos -->
  <div class="info-card">
    <h2 class="info-card__title">Preço e Custos</h2>
    <div class="info-card__grid">
      <div class="info-card__field">
        <span class="info-card__label">Preço de Venda</span>
        <span class="info-card__value mono">{{ product()!.price | brlCurrency }}</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Custo de Aquisição</span>
        <span class="info-card__value mono">{{ product()!.purchaseCost | brlCurrency }}</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Custo de Embalagem</span>
        <span class="info-card__value mono">{{ product()!.packagingCost | brlCurrency }}</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Margem Estimada</span>
        <span class="info-card__value mono" [ngClass]="marginClass()">
          {{ estimatedMargin() !== null ? estimatedMargin()!.toFixed(1) + '%' : '—' }}
        </span>
      </div>
    </div>
  </div>

  <!-- Dimensões -->
  <div class="info-card">
    <h2 class="info-card__title">Dimensões</h2>
    <div class="info-card__grid">
      <div class="info-card__field">
        <span class="info-card__label">Peso</span>
        <span class="info-card__value mono">{{ product()!.weight }} kg</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Altura</span>
        <span class="info-card__value mono">{{ product()!.height }} cm</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Largura</span>
        <span class="info-card__value mono">{{ product()!.width }} cm</span>
      </div>
      <div class="info-card__field">
        <span class="info-card__label">Comprimento</span>
        <span class="info-card__value mono">{{ product()!.length }} cm</span>
      </div>
    </div>
  </div>

  <!-- Galeria -->
  @if (product()!.photoUrls.length > 0) {
    <div class="info-card info-card--full">
      <h2 class="info-card__title">Galeria</h2>
      <div class="gallery-scroll">
        @for (url of product()!.photoUrls; track url) {
          <div class="gallery-scroll__item">
            <img [src]="url" [alt]="product()!.name" />
          </div>
        }
      </div>
    </div>
  }
</div>
```

- [ ] **Step 8: Add SCSS for info sections**

In `product-detail.component.scss`, add styles for the new info sections:

```scss
.info-sections {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
  margin-top: var(--space-6);

  @include m.tablet {
    grid-template-columns: 1fr;
  }

  @include m.mobile {
    grid-template-columns: 1fr;
  }
}

.info-card {
  background: var(--surface);
  border: 1px solid var(--neutral-200);
  border-radius: var(--radius-lg);
  padding: var(--space-4);

  &--full {
    grid-column: 1 / -1;
  }

  &__title {
    font-size: var(--text-base);
    font-weight: 600;
    color: var(--neutral-900);
    margin: 0 0 var(--space-3) 0;
  }

  &__grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: var(--space-3);

    @include m.mobile {
      grid-template-columns: 1fr;
    }
  }

  &__field {
    display: flex;
    flex-direction: column;
    gap: 2px;

    &--full {
      grid-column: 1 / -1;
    }
  }

  &__label {
    font-size: var(--text-xs);
    color: var(--neutral-500);
    font-weight: 500;
  }

  &__value {
    font-size: var(--text-sm);
    color: var(--neutral-800);
  }
}

.value--warning {
  color: var(--warning);
}

.gallery-scroll {
  display: flex;
  gap: var(--space-3);
  overflow-x: auto;
  padding-bottom: var(--space-2);

  &__item {
    flex-shrink: 0;
    width: 120px;
    height: 120px;
    border-radius: var(--radius-md);
    overflow: hidden;
    border: 1px solid var(--neutral-200);
    cursor: pointer;

    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }
}
```

- [ ] **Step 9: Update the Product interface in product.service.ts to include all detail fields**

In `src/PeruShopHub.Web/src/app/services/product.service.ts`, update the `Product` interface to include fields that come from the detail endpoint (some already exist, add missing ones):

```typescript
export interface Product {
  id: string;
  name: string;
  sku: string;
  description?: string;
  categoryId?: string;
  supplier?: string;
  price: number;
  purchaseCost: number;
  acquisitionCost: number; // alias used in detail component
  packagingCost: number;
  weight?: number;
  height?: number;
  width?: number;
  length?: number;
  imageUrl: string | null;
  photoUrls?: string[];
  stock: number;
  status: string;
  margin: number | null;
  variantCount: number;
  needsReview: boolean;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  variants?: any[];
}
```

Note: The backend `ProductDetailDto` returns `PurchaseCost` while the frontend uses `purchaseCost`. The `acquisitionCost` alias is kept for backward compatibility. Ensure `getById` maps correctly — the backend already sends `purchaseCost` in the JSON (camelCase auto-mapping from C# `PurchaseCost`).

- [ ] **Step 10: Run typecheck**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

Expected: No errors. If there are errors from removing the old `kpis` computed, the template references must also be updated (the old KPI grid section in the HTML should be removed since it will be replaced by the Stock and Analytics sections in Tasks 2 and 3).

- [ ] **Step 11: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss src/PeruShopHub.Web/src/app/services/product.service.ts
git commit -m "feat(SP4): 4.1 — Replace mocked product data with real API, add info sections"
```

---

### Task 2: Add Stock section with KPI cards (4.2)

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss`

**Context:** The spec calls for a "Estoque" section with 3 KPI cards: Estoque (total stock), Custo Médio (purchase cost), Custo Total (stock x cost). The `KpiCardComponent` already exists and accepts `label`, `value`, `change`, `changeLabel` inputs. Custo Total is computed client-side.

- [ ] **Step 1: Add computed signals for stock KPIs**

In `product-detail.component.ts`, add:

```typescript
stockKpis = computed(() => {
  const p = this.product();
  if (!p) return [];
  const stock = this.totalVariantStock() || p.stock;
  const avgCost = p.purchaseCost;
  const totalCost = stock * avgCost;
  return [
    { label: 'Estoque', value: `${stock} un.` },
    { label: 'Custo Médio', value: this.formatBrl(avgCost) },
    { label: 'Custo Total', value: this.formatBrl(totalCost) },
  ];
});
```

- [ ] **Step 2: Add the Stock section to the template**

In `product-detail.component.html`, after the info sections and before the `detail-page__sections` div, add:

```html
<!-- Stock section -->
<div class="section-block">
  <h2 class="section-block__title">Estoque</h2>
  <div class="kpi-grid kpi-grid--3">
    @for (kpi of stockKpis(); track kpi.label) {
      <app-kpi-card [label]="kpi.label" [value]="kpi.value"></app-kpi-card>
    }
  </div>
</div>
```

- [ ] **Step 3: Add SCSS for the section block and 3-column KPI grid**

```scss
.section-block {
  margin-top: var(--space-6);

  &__title {
    font-size: var(--text-lg);
    font-weight: 600;
    color: var(--neutral-900);
    margin: 0 0 var(--space-4) 0;
  }
}

.kpi-grid--3 {
  grid-template-columns: repeat(3, 1fr);

  @include m.mobile {
    grid-template-columns: 1fr;
  }
}
```

- [ ] **Step 4: Run typecheck**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss
git commit -m "feat(SP4): 4.2 — Add Stock section with KPI cards"
```

---

### Task 3: Add Analytics section with date range selector and KPI cards (4.3)

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss`

**Context:** The spec requires an "Analytics" section with a date range dropdown (using the existing `SelectDropdownComponent` with presets: 7d, 30d, 60d, 90d, 180d, 1y), 4 KPI cards (Vendas, Receita, Lucro, Margem — each with change % vs previous period), and two tables below (handled in Task 5). The analytics data comes from the backend endpoint built in Task 4. For now, wire the UI with signals and a service method that we will implement in Task 4.

- [ ] **Step 1: Add SelectDropdownComponent to imports**

In `product-detail.component.ts`, add `SelectDropdownComponent` and `SelectOption` to imports:

```typescript
import { SelectDropdownComponent } from '../../shared/components/select-dropdown/select-dropdown.component';
import type { SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
```

Add `SelectDropdownComponent` to the component's `imports` array.

- [ ] **Step 2: Add analytics state signals and date range options**

```typescript
// Analytics state
analyticsDays = signal(30);
analyticsLoading = signal(false);
analyticsData = signal<{
  totalSales: number;
  totalRevenue: number;
  totalProfit: number;
  margin: number | null;
  salesChange: number | null;
  revenueChange: number | null;
  profitChange: number | null;
  marginChange: number | null;
} | null>(null);

dateRangeOptions: SelectOption[] = [
  { value: '7', label: '7 dias' },
  { value: '30', label: '30 dias' },
  { value: '60', label: '60 dias' },
  { value: '90', label: '90 dias' },
  { value: '180', label: '180 dias' },
  { value: '365', label: '1 ano' },
];
```

- [ ] **Step 3: Add computed for analytics KPIs**

```typescript
analyticsKpis = computed(() => {
  const a = this.analyticsData();
  if (!a) return [];
  return [
    { label: 'Vendas', value: String(a.totalSales), change: a.salesChange ?? undefined, changeLabel: 'vs período anterior' },
    { label: 'Receita', value: this.formatBrl(a.totalRevenue), change: a.revenueChange ?? undefined, changeLabel: 'vs período anterior' },
    { label: 'Lucro', value: this.formatBrl(a.totalProfit), change: a.profitChange ?? undefined, changeLabel: 'vs período anterior' },
    { label: 'Margem', value: a.margin !== null ? `${a.margin.toFixed(1)}%` : '—', change: a.marginChange ?? undefined, changeLabel: 'vs período anterior' },
  ];
});
```

- [ ] **Step 4: Add onDateRangeChange method and loadAnalytics method**

```typescript
onDateRangeChange(value: string): void {
  this.analyticsDays.set(Number(value));
  this.loadAnalytics();
}

private async loadAnalytics(): Promise<void> {
  this.analyticsLoading.set(true);
  try {
    const data = await this.productService.getAnalytics(this.productId, this.analyticsDays());
    this.analyticsData.set(data);
  } catch {
    this.analyticsData.set(null);
  } finally {
    this.analyticsLoading.set(false);
  }
}
```

- [ ] **Step 5: Call loadAnalytics in loadData**

At the end of the `loadData()` method (inside the try block, after setting the product), add:

```typescript
this.loadAnalytics();
```

- [ ] **Step 6: Add the Analytics section to the template**

After the Stock section and before `detail-page__sections`:

```html
<!-- Analytics section -->
<div class="section-block">
  <div class="section-block__header">
    <h2 class="section-block__title">Analytics</h2>
    <app-select-dropdown
      [options]="dateRangeOptions"
      [value]="analyticsDays().toString()"
      (valueChange)="onDateRangeChange($event)"
    ></app-select-dropdown>
  </div>
  @if (analyticsLoading() && !analyticsData()) {
    <div class="kpi-grid kpi-grid--4">
      @for (i of [1,2,3,4]; track i) {
        <div class="skeleton skeleton--rect" style="height: 100px; border-radius: 8px;"></div>
      }
    </div>
  } @else {
    <div class="kpi-grid kpi-grid--4">
      @for (kpi of analyticsKpis(); track kpi.label) {
        <app-kpi-card
          [label]="kpi.label"
          [value]="kpi.value"
          [change]="kpi.change"
          [changeLabel]="kpi.changeLabel"
        ></app-kpi-card>
      }
    </div>
  }
</div>
```

- [ ] **Step 7: Add SCSS for Analytics section**

```scss
.section-block__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
  gap: var(--space-3);

  .section-block__title {
    margin-bottom: 0;
  }
}

.kpi-grid--4 {
  grid-template-columns: repeat(4, 1fr);

  @include m.tablet {
    grid-template-columns: repeat(2, 1fr);
  }

  @include m.mobile {
    grid-template-columns: 1fr;
  }
}
```

- [ ] **Step 8: Add a stub `getAnalytics` method to ProductService (will be fully wired in Task 4)**

In `src/PeruShopHub.Web/src/app/services/product.service.ts`, add:

```typescript
async getAnalytics(id: string, days = 30): Promise<{
  totalSales: number;
  totalRevenue: number;
  totalProfit: number;
  margin: number | null;
  salesChange: number | null;
  revenueChange: number | null;
  profitChange: number | null;
  marginChange: number | null;
}> {
  const params = new HttpParams().set('days', days.toString());
  return firstValueFrom(
    this.http.get<any>(`${this.baseUrl}/${id}/analytics`, { params }),
  );
}
```

- [ ] **Step 9: Run typecheck**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **Step 10: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss src/PeruShopHub.Web/src/app/services/product.service.ts
git commit -m "feat(SP4): 4.3 — Add Analytics section with date range selector and KPI cards"
```

---

### Task 4: Backend analytics and recent orders endpoints (4.4)

**Files:**
- Modify: `src/PeruShopHub.Application/DTOs/Products/ProductDtos.cs`
- Modify: `src/PeruShopHub.API/Controllers/ProductsController.cs`

**Context:** The backend needs two new endpoints. The `OrderItems` table has `ProductId` (nullable Guid), `Quantity`, `UnitPrice`, `Subtotal`. The `Orders` table has `CreatedAt`, `Profit`. We need to join `OrderItems` with `Orders` to compute analytics per product. The `Product` entity has `PurchaseCost` and `PackagingCost` for profit calculation per order item. `PagedResult<T>` already exists at `PeruShopHub.Application.Common.PagedResult`.

**Important:** `OrderItem` does NOT have its own profit field. Profit per item must be computed as: `(UnitPrice - Product.PurchaseCost - Product.PackagingCost) * Quantity`. This requires joining `OrderItems` to `Products`.

- [ ] **Step 1: Add ProductAnalyticsDto and ProductRecentOrderDto**

In `src/PeruShopHub.Application/DTOs/Products/ProductDtos.cs`, add at the end of the file:

```csharp
public record ProductAnalyticsDto(
    int TotalSales,
    decimal TotalRevenue,
    decimal TotalProfit,
    decimal? Margin,
    decimal? SalesChange,
    decimal? RevenueChange,
    decimal? ProfitChange,
    decimal? MarginChange);

public record ProductRecentOrderDto(
    Guid OrderId,
    DateTime Date,
    int Quantity,
    decimal UnitPrice,
    decimal Total,
    decimal Profit);
```

- [ ] **Step 2: Add the analytics endpoint to ProductsController**

In `src/PeruShopHub.API/Controllers/ProductsController.cs`, add after the `GetCostHistory` method:

```csharp
[HttpGet("{id:guid}/analytics")]
public async Task<ActionResult<ProductAnalyticsDto>> GetAnalytics(
    Guid id,
    [FromQuery] int days = 30,
    CancellationToken ct = default)
{
    var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    if (product is null)
        return NotFound();

    var now = DateTime.UtcNow;
    var currentStart = now.AddDays(-days);
    var previousStart = now.AddDays(-days * 2);

    // Current period
    var currentItems = await _db.OrderItems
        .AsNoTracking()
        .Where(oi => oi.ProductId == id)
        .Join(_db.Orders.AsNoTracking(),
            oi => oi.OrderId,
            o => o.Id,
            (oi, o) => new { oi, o })
        .Where(x => x.o.CreatedAt >= currentStart && x.o.CreatedAt < now)
        .Select(x => new
        {
            x.oi.Quantity,
            x.oi.UnitPrice,
            x.oi.Subtotal
        })
        .ToListAsync(ct);

    var currentSales = currentItems.Sum(x => x.Quantity);
    var currentRevenue = currentItems.Sum(x => x.Subtotal);
    var unitProfit = product.Price > 0
        ? product.Price - product.PurchaseCost - product.PackagingCost
        : 0m;
    var currentProfit = currentItems.Sum(x => (x.UnitPrice - product.PurchaseCost - product.PackagingCost) * x.Quantity);
    var currentMargin = currentRevenue > 0 ? (currentProfit / currentRevenue) * 100 : (decimal?)null;

    // Previous period
    var previousItems = await _db.OrderItems
        .AsNoTracking()
        .Where(oi => oi.ProductId == id)
        .Join(_db.Orders.AsNoTracking(),
            oi => oi.OrderId,
            o => o.Id,
            (oi, o) => new { oi, o })
        .Where(x => x.o.CreatedAt >= previousStart && x.o.CreatedAt < currentStart)
        .Select(x => new
        {
            x.oi.Quantity,
            x.oi.UnitPrice,
            x.oi.Subtotal
        })
        .ToListAsync(ct);

    var previousSales = previousItems.Sum(x => x.Quantity);
    var previousRevenue = previousItems.Sum(x => x.Subtotal);
    var previousProfit = previousItems.Sum(x => (x.UnitPrice - product.PurchaseCost - product.PackagingCost) * x.Quantity);
    var previousMargin = previousRevenue > 0 ? (previousProfit / previousRevenue) * 100 : (decimal?)null;

    decimal? CalcChange(decimal current, decimal previous) =>
        previous != 0 ? ((current - previous) / Math.Abs(previous)) * 100 : null;

    var dto = new ProductAnalyticsDto(
        currentSales,
        currentRevenue,
        currentProfit,
        currentMargin,
        CalcChange(currentSales, previousSales),
        CalcChange(currentRevenue, previousRevenue),
        CalcChange(currentProfit, previousProfit),
        currentMargin.HasValue && previousMargin.HasValue
            ? currentMargin.Value - previousMargin.Value
            : null);

    return Ok(dto);
}
```

- [ ] **Step 3: Add the recent orders endpoint to ProductsController**

```csharp
[HttpGet("{id:guid}/recent-orders")]
public async Task<ActionResult<PagedResult<ProductRecentOrderDto>>> GetRecentOrders(
    Guid id,
    [FromQuery] int days = 30,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken ct = default)
{
    var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    if (product is null)
        return NotFound();

    var cutoff = DateTime.UtcNow.AddDays(-days);

    var query = _db.OrderItems
        .AsNoTracking()
        .Where(oi => oi.ProductId == id)
        .Join(_db.Orders.AsNoTracking(),
            oi => oi.OrderId,
            o => o.Id,
            (oi, o) => new { oi, o })
        .Where(x => x.o.CreatedAt >= cutoff);

    var totalCount = await query.CountAsync(ct);

    var items = await query
        .OrderByDescending(x => x.o.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new ProductRecentOrderDto(
            x.o.Id,
            x.o.CreatedAt,
            x.oi.Quantity,
            x.oi.UnitPrice,
            x.oi.Subtotal,
            (x.oi.UnitPrice - product.PurchaseCost - product.PackagingCost) * x.oi.Quantity))
        .ToListAsync(ct);

    var result = new PagedResult<ProductRecentOrderDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };

    return Ok(result);
}
```

**Note:** The profit calculation `(UnitPrice - PurchaseCost - PackagingCost) * Quantity` uses the product's current costs. This is a simplification — ideally cost-at-time-of-sale would be tracked, but matching the spec's approach. The `product` variable is captured outside the LINQ expression; if EF Core has trouble translating it, the fallback is to materialize the query and compute profit in memory.

- [ ] **Step 4: Run backend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Application/DTOs/Products/ProductDtos.cs src/PeruShopHub.API/Controllers/ProductsController.cs
git commit -m "feat(SP4): 4.4 — Add product analytics and recent orders backend endpoints"
```

---

### Task 5: Migrate tables to DataGridComponent and wire recent orders endpoint (4.5)

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss`
- Modify: `src/PeruShopHub.Web/src/app/services/product.service.ts`

**Context:** The detail page currently has two plain `<table class="detail-table">` elements for "Histórico de Custos" and "Vendas Recentes". Both need to be migrated to `<app-data-grid>`. The DataGridComponent requires `columns: GridColumn[]`, `data: Record<string, any>[]`, and supports `[totalCount]`, `entityName`, custom cell templates via `appGridCell` directive, and mobile card templates via `appGridCard`. The "Vendas Recentes" table currently uses mocked data — it must be wired to the new `GET /api/products/{id}/recent-orders` endpoint from Task 4. Both tables should appear inside the Analytics section (below the KPI cards).

- [ ] **Step 1: Add DataGridComponent imports**

In `product-detail.component.ts`, add to imports:

```typescript
import {
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
} from '../../shared/components';
import type { GridColumn } from '../../shared/components';
```

Add `DataGridComponent`, `GridCellDirective`, `GridCardDirective` to the component's `imports` array.

- [ ] **Step 2: Add column definitions for both tables**

```typescript
costHistoryColumns: GridColumn[] = [
  { key: 'date', label: 'Data' },
  { key: 'purchaseOrderId', label: 'Ref. Compra' },
  { key: 'quantity', label: 'Qtd', align: 'right' },
  { key: 'unitCostPaid', label: 'Custo Pago', align: 'right' },
  { key: 'previousCost', label: 'Custo Anterior', align: 'right' },
  { key: 'newCost', label: 'Novo Custo', align: 'right' },
];

recentOrderColumns: GridColumn[] = [
  { key: 'orderId', label: 'Pedido' },
  { key: 'date', label: 'Data' },
  { key: 'quantity', label: 'Qtd', align: 'right' },
  { key: 'unitPrice', label: 'Preço Unitário', align: 'right' },
  { key: 'total', label: 'Total', align: 'right' },
  { key: 'profit', label: 'Lucro', align: 'right' },
];
```

- [ ] **Step 3: Add signals for recent orders state and cost history total count**

```typescript
recentOrders = signal<Record<string, any>[]>([]);
recentOrdersTotalCount = signal(0);
recentOrdersLoading = signal(false);
costHistoryTotalCount = signal(0);
```

Note: `costHistory` signal already exists. Update its type from `CostHistoryItem[]` to `Record<string, any>[]` or keep as-is and cast when passing to the grid.

- [ ] **Step 4: Add getRecentOrders method to ProductService**

In `src/PeruShopHub.Web/src/app/services/product.service.ts`:

```typescript
getRecentOrders(id: string, days = 30, page = 1, pageSize = 10): Observable<PagedResult<any>> {
  const params = new HttpParams()
    .set('days', days.toString())
    .set('page', page.toString())
    .set('pageSize', pageSize.toString());
  return this.http.get<PagedResult<any>>(`${this.baseUrl}/${id}/recent-orders`, { params });
}
```

- [ ] **Step 5: Add loadRecentOrders method and call it from loadAnalytics**

In `product-detail.component.ts`:

```typescript
private loadRecentOrders(): void {
  this.recentOrdersLoading.set(true);
  this.productService.getRecentOrders(this.productId, this.analyticsDays()).subscribe({
    next: (result) => {
      this.recentOrders.set(result.items);
      this.recentOrdersTotalCount.set(result.totalCount);
      this.recentOrdersLoading.set(false);
    },
    error: () => {
      this.recentOrders.set([]);
      this.recentOrdersTotalCount.set(0);
      this.recentOrdersLoading.set(false);
    },
  });
}
```

Call `this.loadRecentOrders()` inside `loadAnalytics()` after the analytics fetch. Also call it when the date range changes (it's already called via `onDateRangeChange` -> `loadAnalytics`).

- [ ] **Step 6: Update loadData to store costHistory totalCount**

In the existing `getCostHistory` subscription:

```typescript
this.productService.getCostHistory(this.productId).subscribe({
  next: (result) => {
    this.costHistory.set(result.items);
    this.costHistoryTotalCount.set(result.totalCount);
  },
  error: () => {
    this.costHistory.set([]);
    this.costHistoryTotalCount.set(0);
  },
});
```

- [ ] **Step 7: Replace the cost history table HTML with DataGridComponent**

Remove the old `<table class="detail-table">` for cost history and replace with:

```html
<!-- Cost History -->
<div class="detail-page__section">
  <h2 class="detail-page__section-title">Histórico de Custos</h2>
  <app-data-grid
    [columns]="costHistoryColumns"
    [data]="costHistory()"
    [totalCount]="costHistoryTotalCount()"
    entityName="registros"
    emptyTitle="Nenhum histórico de custo"
    ariaLabel="Histórico de custos do produto"
  >
    <ng-template appGridCell="date" let-row let-value="value">
      {{ formatDate(value) }}
    </ng-template>
    <ng-template appGridCell="purchaseOrderId" let-row let-value="value">
      @if (value) {
        <a class="detail-table__link" [routerLink]="'/compras/' + value">{{ value }}</a>
      } @else {
        <span class="text-muted">—</span>
      }
    </ng-template>
    <ng-template appGridCell="unitCostPaid" let-row let-value="value">
      <span class="mono">{{ value | brlCurrency }}</span>
    </ng-template>
    <ng-template appGridCell="previousCost" let-row let-value="value">
      <span class="mono">{{ value | brlCurrency }}</span>
    </ng-template>
    <ng-template appGridCell="newCost" let-row let-value="value">
      <span class="mono" [ngClass]="getCostChangeClass(row['previousCost'], value)">{{ value | brlCurrency }}</span>
    </ng-template>
    <ng-template appGridCard let-row>
      <div class="mobile-card">
        <div class="mobile-card__row">
          <span class="mobile-card__label">Data</span>
          <span>{{ formatDate(row['date']) }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Qtd</span>
          <span>{{ row['quantity'] }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Custo Pago</span>
          <span class="mono">{{ row['unitCostPaid'] | brlCurrency }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Novo Custo</span>
          <span class="mono" [ngClass]="getCostChangeClass(row['previousCost'], row['newCost'])">{{ row['newCost'] | brlCurrency }}</span>
        </div>
      </div>
    </ng-template>
  </app-data-grid>
</div>
```

- [ ] **Step 8: Replace the recent orders table HTML with DataGridComponent**

Remove the old `<table class="detail-table">` for recent orders and replace with:

```html
<!-- Recent Orders -->
<div class="detail-page__section">
  <h2 class="detail-page__section-title">Vendas Recentes</h2>
  <app-data-grid
    [columns]="recentOrderColumns"
    [data]="recentOrders()"
    [loading]="recentOrdersLoading()"
    [totalCount]="recentOrdersTotalCount()"
    entityName="vendas"
    emptyTitle="Nenhuma venda no período"
    ariaLabel="Vendas recentes do produto"
  >
    <ng-template appGridCell="orderId" let-row let-value="value">
      <span class="mono">#{{ value }}</span>
    </ng-template>
    <ng-template appGridCell="date" let-row let-value="value">
      {{ formatDate(value) }}
    </ng-template>
    <ng-template appGridCell="unitPrice" let-row let-value="value">
      <span class="mono">{{ value | brlCurrency }}</span>
    </ng-template>
    <ng-template appGridCell="total" let-row let-value="value">
      <span class="mono">{{ value | brlCurrency }}</span>
    </ng-template>
    <ng-template appGridCell="profit" let-row let-value="value">
      <span class="mono" [ngClass]="getProfitClass(value)">{{ value | brlCurrency }}</span>
    </ng-template>
    <ng-template appGridCard let-row>
      <div class="mobile-card">
        <div class="mobile-card__row">
          <span class="mobile-card__label">Pedido</span>
          <span class="mono">#{{ row['orderId'] }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Data</span>
          <span>{{ formatDate(row['date']) }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Qtd</span>
          <span>{{ row['quantity'] }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Total</span>
          <span class="mono">{{ row['total'] | brlCurrency }}</span>
        </div>
        <div class="mobile-card__row">
          <span class="mobile-card__label">Lucro</span>
          <span class="mono" [ngClass]="getProfitClass(row['profit'])">{{ row['profit'] | brlCurrency }}</span>
        </div>
      </div>
    </ng-template>
  </app-data-grid>
</div>
```

- [ ] **Step 9: Move both tables inside the Analytics section in the template**

The Cost History and Recent Orders tables should be inside the Analytics section (after the KPI cards), NOT in the old `detail-page__sections` two-column grid. Update the template structure so the Analytics section block contains: header with date range selector, KPI row, then a two-column grid with the two DataGrid tables.

```html
<!-- Analytics section -->
<div class="section-block">
  <div class="section-block__header">
    <h2 class="section-block__title">Analytics</h2>
    <app-select-dropdown ...></app-select-dropdown>
  </div>
  <!-- KPI cards -->
  <div class="kpi-grid kpi-grid--4">...</div>
  <!-- Tables -->
  <div class="analytics-tables">
    <!-- Cost History data-grid here -->
    <!-- Recent Orders data-grid here -->
  </div>
</div>
```

- [ ] **Step 10: Add SCSS for analytics tables and mobile cards**

```scss
.analytics-tables {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
  margin-top: var(--space-4);

  @include m.tablet {
    grid-template-columns: 1fr;
  }

  @include m.mobile {
    grid-template-columns: 1fr;
  }
}

.mobile-card {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);

  &__row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: var(--text-sm);
  }

  &__label {
    color: var(--neutral-500);
    font-size: var(--text-xs);
  }
}
```

- [ ] **Step 11: Clean up — remove old detail-table styles if no longer used**

If the old `<table class="detail-table">` is still used in the Variants section, keep the styles. If the variants section still uses plain tables, keep `.detail-table` styles. Only remove if fully unused. The variant table still uses plain HTML tables, so keep `.detail-table` styles.

- [ ] **Step 12: Run frontend typecheck**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **Step 13: Run backend build to verify everything still compiles**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

- [ ] **Step 14: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts src/PeruShopHub.Web/src/app/pages/products/product-detail.component.html src/PeruShopHub.Web/src/app/pages/products/product-detail.component.scss src/PeruShopHub.Web/src/app/services/product.service.ts
git commit -m "feat(SP4): 4.5 — Migrate tables to DataGridComponent, wire recent orders endpoint"
```

---

### Implementation Notes

**Key patterns used in this codebase:**
- `ProductService` methods use `firstValueFrom()` to convert Observables to Promises (for `async/await`)
- `getCostHistory` and `getRecentOrders` return raw Observables (using `.subscribe()` in the component)
- DataGridComponent expects `data: Record<string, any>[]` — API response objects work directly since TypeScript interfaces are duck-typed
- Custom cell rendering uses `appGridCell="columnKey"` directive with `let-row` and `let-value="value"` template variables
- Mobile cards use `appGridCard` directive
- `KpiCardComponent` accepts `label: string`, `value: string`, optional `change: number`, optional `changeLabel: string`, optional `invertColors: boolean`
- `SelectDropdownComponent` accepts `options: SelectOption[]`, `value: string`, emits `valueChange: string`
- The backend `ProductDetailDto` is a C# record with positional parameters — field names are PascalCase in C# but auto-mapped to camelCase in JSON responses

**EF Core translation concern in Task 4:**
The `product.PurchaseCost` captured variable inside the LINQ `Select` projection may not translate to SQL. If EF Core throws a translation error, the workaround is to materialize the query first (`.ToListAsync()`) then compute profit in memory with `.Select(...)`. This is acceptable for paginated results (10 items max).

**Category breadcrumb in Task 1:**
The `ProductDetailDto` returns `CategoryId` (string GUID) but not the category name or path. The frontend resolves the breadcrumb path by loading all categories via `CategoryService.loadAll()` (which caches them) and walking the parent chain. This avoids adding a new backend field.
