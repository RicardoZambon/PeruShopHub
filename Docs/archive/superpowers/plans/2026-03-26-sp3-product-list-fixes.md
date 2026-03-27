# SP3: Product List Fixes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add category filter to product list, fix stock column (computed from variants), fix margin calculation (computed server-side).

**Architecture:** Backend adds categoryId filter param, Stock and Margin computed fields to ProductListDto. Frontend adds category dropdown filter.

**Tech Stack:** C# ASP.NET Core 8, EF Core 8, PostgreSQL, Angular 17+, standalone components, signals

**Spec:** `docs/superpowers/specs/2026-03-26-products-overhaul-design.md` (SP3 section)

---

### Task 1: Add Stock and Margin computed fields to ProductListDto

**Files:**
- Modify: `src/PeruShopHub.Application/DTOs/Products/ProductDtos.cs`

- [ ] **Step 1: Add Stock and Margin fields to ProductListDto**

The current `ProductListDto` has no `Stock` or `Margin` fields. Add them so the backend can return computed values.

Replace the existing `ProductListDto` record:

```csharp
public record ProductListDto(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    decimal PurchaseCost,
    decimal PackagingCost,
    string Status,
    bool NeedsReview,
    bool IsActive,
    int VariantCount,
    string? PhotoUrl,
    DateTime CreatedAt);
```

With:

```csharp
public record ProductListDto(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    decimal PurchaseCost,
    decimal PackagingCost,
    string Status,
    bool NeedsReview,
    bool IsActive,
    int VariantCount,
    int Stock,
    decimal? Margin,
    string? PhotoUrl,
    DateTime CreatedAt);
```

`Stock` = sum of all variant stocks for the product (0 if no variants).
`Margin` = `((Price - PurchaseCost - PackagingCost) / Price) * 100` when `Price > 0`, otherwise `null`.

- [ ] **Step 2: Run backend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

Expected: Build will FAIL because `ProductsController.cs` constructs `ProductListDto` without the new fields. This is fixed in Task 2.

- [ ] **Step 3: Commit DTO change**

```bash
git add src/PeruShopHub.Application/DTOs/Products/ProductDtos.cs
git commit -m "feat(sp3): add Stock and Margin fields to ProductListDto"
```

---

### Task 2: Update ProductsController to compute Stock, Margin, and support categoryId filter

**Files:**
- Modify: `src/PeruShopHub.API/Controllers/ProductsController.cs`

- [ ] **Step 1: Add categoryId query parameter to GetProducts**

Add the `categoryId` parameter and `CategoriesController`-style descendant filtering. Update the method signature from:

```csharp
public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? status = null,
    [FromQuery] string sortBy = "name",
    [FromQuery] string sortDir = "asc",
    CancellationToken ct = default)
```

To:

```csharp
public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? status = null,
    [FromQuery] Guid? categoryId = null,
    [FromQuery] string sortBy = "name",
    [FromQuery] string sortDir = "asc",
    CancellationToken ct = default)
```

- [ ] **Step 2: Add categoryId to the cache key**

Update the cache key to include the categoryId:

```csharp
var cacheKey = $"products:list:{page}:{pageSize}:{search}:{status}:{categoryId}:{sortBy}:{sortDir}";
```

- [ ] **Step 3: Add category filter logic (with descendant support)**

After the existing `status` filter block (around line 53), add:

```csharp
if (categoryId.HasValue)
{
    // Get all descendant category IDs recursively
    var categoryIds = new List<Guid> { categoryId.Value };
    var toCheck = new Queue<Guid>();
    toCheck.Enqueue(categoryId.Value);
    while (toCheck.Count > 0)
    {
        var parentId = toCheck.Dequeue();
        var childIds = await _db.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == parentId)
            .Select(c => c.Id)
            .ToListAsync(ct);
        foreach (var childId in childIds)
        {
            categoryIds.Add(childId);
            toCheck.Enqueue(childId);
        }
    }
    var categoryIdStrings = categoryIds.Select(id => id.ToString()).ToList();
    query = query.Where(p => p.CategoryId != null && categoryIdStrings.Contains(p.CategoryId));
}
```

Note: `Product.CategoryId` is `string?` in the entity, so we compare against string representations of the GUIDs.

- [ ] **Step 4: Add Stock sorting support**

In the sort switch expression, add a case for stock. Insert before the default `_` case:

```csharp
"stock" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
    ? query.OrderByDescending(p => p.Variants.Sum(v => v.Stock))
    : query.OrderBy(p => p.Variants.Sum(v => v.Stock)),
"margin" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
    ? query.OrderByDescending(p => p.Price > 0 ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100 : 0)
    : query.OrderBy(p => p.Price > 0 ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100 : 0),
```

- [ ] **Step 5: Update the Select projection to include Stock and Margin**

Replace the existing `.Select(p => new ProductListDto(...))` block (lines 79-95) with:

```csharp
.Select(p => new ProductListDto(
    p.Id,
    p.Sku,
    p.Name,
    p.Price,
    p.PurchaseCost,
    p.PackagingCost,
    p.Status,
    p.NeedsReview,
    p.IsActive,
    p.Variants.Count,
    p.Variants.Sum(v => v.Stock),
    p.Price > 0
        ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100m
        : (decimal?)null,
    _db.FileUploads
        .Where(f => f.EntityType == "product" && f.EntityId == p.Id)
        .OrderBy(f => f.SortOrder)
        .Select(f => f.StoragePath)
        .FirstOrDefault(),
    p.CreatedAt))
.ToListAsync(ct);
```

Key changes:
- `p.Variants.Sum(v => v.Stock)` computes total stock across all variants
- Margin formula: `((Price - PurchaseCost - PackagingCost) / Price) * 100` when Price > 0, else null
- Added `ct` (CancellationToken) to `ToListAsync`

- [ ] **Step 6: Run backend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add src/PeruShopHub.API/Controllers/ProductsController.cs
git commit -m "feat(sp3): add categoryId filter, computed Stock and Margin to products list endpoint"
```

---

### Task 3: Update frontend Product interface and ProductService for categoryId filter

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/services/product.service.ts`

- [ ] **Step 1: Add categoryId to ProductListParams**

Update the `ProductListParams` interface:

```typescript
export interface ProductListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  categoryId?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}
```

- [ ] **Step 2: Pass categoryId to the HTTP request**

In the `list()` method, add after the `if (params.status)` line:

```typescript
if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
```

- [ ] **Step 3: Run frontend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/PeruShopHub.Web/src/app/services/product.service.ts
git commit -m "feat(sp3): add categoryId parameter to ProductService.list()"
```

---

### Task 4: Add category filter dropdown to products list component

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.html`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.scss`

- [ ] **Step 1: Add CategoryService import and category state to the component**

In `products-list.component.ts`, add the CategoryService import at the top:

```typescript
import { CategoryService } from '../../services/category.service';
```

Add the import for `Category` model:

```typescript
import type { Category } from '../../models/category.model';
```

Inside the component class, add after the `ProductService` injection:

```typescript
private readonly categoryService = inject(CategoryService);
```

Add category-related signals after `statusFilter`:

```typescript
readonly categoryFilter = signal<string>('');
readonly categoryOptions = signal<SelectOption[]>([{ value: '', label: 'Todas as categorias' }]);
```

- [ ] **Step 2: Load categories on init and build flat options with indentation**

Add a helper method to the component class:

```typescript
private async loadCategories(): Promise<void> {
  try {
    const tree = await this.categoryService.getTree();
    const options: SelectOption[] = [{ value: '', label: 'Todas as categorias' }];
    const flatten = (categories: Category[], depth: number): void => {
      for (const cat of categories) {
        const indent = '\u00A0\u00A0'.repeat(depth);
        options.push({ value: cat.id, label: `${indent}${cat.name}` });
        if (cat.children?.length) {
          flatten(cat.children, depth + 1);
        }
      }
    };
    flatten(tree, 0);
    this.categoryOptions.set(options);
  } catch {
    // Silently fail — category filter just won't be populated
  }
}
```

Update `ngOnInit` to call `loadCategories` in parallel with `loadProducts`:

```typescript
ngOnInit(): void {
  this.loadCategories();
  this.loadProducts(true);
}
```

- [ ] **Step 3: Add onCategoryChange handler**

Add after the `onStatusChange` method:

```typescript
onCategoryChange(value: string): void {
  this.categoryFilter.set(value);
  this.loadProducts(true);
  this.gridRef?.scrollToTop();
}
```

- [ ] **Step 4: Pass categoryId to loadProducts**

In the `loadProducts` method, update the service call to include `categoryId`:

```typescript
const result = await this.productService.list({
  page: this.currentPage(),
  pageSize: this.pageSize(),
  search: this.searchQuery() || undefined,
  status: this.statusFilter() === 'Todos' ? undefined : this.statusFilter(),
  categoryId: this.categoryFilter() || undefined,
  sortBy: this.sortBy() ?? undefined,
  sortDirection: this.sortDirection(),
});
```

Also update the `hasData` computation at line 123 to account for the category filter:

```typescript
this.hasData.set(totalLoaded > 0 || this.searchQuery().length > 0 || this.statusFilter() !== 'Todos' || this.categoryFilter() !== '');
```

- [ ] **Step 5: Add category dropdown to the HTML template**

In `products-list.component.html`, inside the `produtos__action-bar` div (after the status dropdown on line 31), add:

```html
<app-select-dropdown
  [options]="categoryOptions()"
  [value]="categoryFilter()"
  (valueChange)="onCategoryChange($event)"
></app-select-dropdown>
```

- [ ] **Step 6: Update action-bar SCSS for 3 items**

In `products-list.component.scss`, update the `&__action-bar` rule to ensure the search input takes remaining space while both dropdowns have fixed width:

```scss
&__action-bar {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  margin-bottom: var(--space-4);

  app-search-input {
    flex: 1;
    min-width: 0;
  }

  app-select-dropdown {
    flex-shrink: 0;
  }

  @include m.mobile {
    flex-direction: column;
    align-items: stretch;

    app-search-input,
    app-select-dropdown {
      flex: unset;
    }
  }
}
```

- [ ] **Step 7: Run frontend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/products/products-list.component.ts src/PeruShopHub.Web/src/app/pages/products/products-list.component.html src/PeruShopHub.Web/src/app/pages/products/products-list.component.scss
git commit -m "feat(sp3): add category filter dropdown to products list"
```

---

### Task 5: Verify Stock and Margin rendering in the frontend

**Files:**
- Verify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.ts`
- Verify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.html`
- Verify: `src/PeruShopHub.Web/src/app/services/product.service.ts`

The frontend `Product` interface already has `stock: number` and `margin: number | null`, and the template already renders both columns with appropriate formatting (stock with low/zero styling, margin with `<app-margin-badge>`). The `gridData` computed already maps both fields. No frontend changes are needed for the data to render once the backend returns the values.

- [ ] **Step 1: Verify the Product interface matches the updated backend DTO**

Confirm these fields exist in the `Product` interface in `product.service.ts`:

```typescript
stock: number;          // matches backend Stock (int)
margin: number | null;  // matches backend Margin (decimal?)
```

These already exist on lines 20 and 23. No change needed.

- [ ] **Step 2: Verify the stock cell template handles null/zero correctly**

In `products-list.component.html`, the stock cell (lines 74-79) already handles `value === 0` and `value <= 10` with appropriate CSS classes. No change needed.

- [ ] **Step 3: Verify the margin cell template handles null**

The `<app-margin-badge [margin]="value">` component (line 103) should already handle `null` margin gracefully (showing "---" or similar). Confirm the MarginBadgeComponent handles null input.

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

Expected: Build succeeds. No code changes needed for this task — it's a verification step confirming that the existing frontend code will correctly render the new backend data.

---

### Task 6: Full integration build verification

**Files:** None (verification only)

- [ ] **Step 1: Run full backend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 2: Run full frontend build**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

Expected: Build completes with no errors.

- [ ] **Step 3: Verify all commits are in place**

```bash
git log --oneline -5
```

Expected commits (newest first):
1. `feat(sp3): add category filter dropdown to products list`
2. `feat(sp3): add categoryId parameter to ProductService.list()`
3. `feat(sp3): add categoryId filter, computed Stock and Margin to products list endpoint`
4. `feat(sp3): add Stock and Margin fields to ProductListDto`
