# SP1: Product Form Fixes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix product form layout, replace mocked categories with TreeSelect + API, add category search endpoint, SKU auto-suggestion, delete/disable product, and save-redirect-to-view.

**Architecture:** Backend changes (new endpoints, migration) + frontend wiring of existing TreeSelect component + form behavior fixes.

**Tech Stack:** C# ASP.NET Core 8, EF Core 8, PostgreSQL, Angular 17+, standalone components, signals

**Spec:** `docs/superpowers/specs/2026-03-26-products-overhaul-design.md` (SP1 section)

---

## Task 1: Form Layout Fix & Category Dropdown Clipping (Spec 1.1)

**Problem:** Category dropdown clips against parent `overflow: hidden`. The Variações tab-panel is missing `.form-layout` class.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.scss`

**Steps:**

- [ ] **1.1** In `product-form.component.html`, verify all tab panels use `class="tab-panel form-layout"`. Currently the Variações tab-panel (line 240) is missing `form-layout`:

```html
<!-- BEFORE -->
<div class="tab-panel" [class.active]="activeTab() === 'variacoes'" [class.accordion-open]="isAccordionOpen('variacoes')">

<!-- AFTER -->
<div class="tab-panel form-layout" [class.active]="activeTab() === 'variacoes'" [class.accordion-open]="isAccordionOpen('variacoes')">
```

- [ ] **1.2** In `product-form.component.scss`, fix the `.dropdown-list` clipping issue. The `.tab-panel` or `.form-panel` may have `overflow: hidden` inherited. Add `overflow: visible` to the `.form-content` and ensure the `.categoria-dropdown` container can show the dropdown above its ancestors. The dropdown already uses `position: absolute` and `z-index: 10`, so the fix is to ensure no ancestor clips it:

```scss
// Add to .form-content
.form-content {
  max-width: 720px;
  overflow: visible;
}

// Ensure tab-panel does not clip the dropdown
.tab-panel {
  // ... existing rules ...
  overflow: visible;
}
```

Note: After Task 2, the custom `.categoria-dropdown` will be replaced by `<app-tree-select>` which uses `position: absolute` with `z-index: 20`. This fix still applies to the tree-select dropdown.

- [ ] **1.3** Verify build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **1.4** Commit: `fix: SP1-1.1 - Fix form layout classes and category dropdown clipping`

---

## Task 2: Replace Mocked Categories with TreeSelect + Load from API (Spec 1.2)

**Problem:** Product form uses a hardcoded 12-item `CATEGORIAS` string array. The `TreeSelectComponent` exists but is not wired in. The form control `categoria` stores a name string instead of a GUID.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.scss`

**Steps:**

- [ ] **2.1** In `product-form.component.ts`:
  - Remove the `CATEGORIAS` constant (lines 27-31)
  - Remove the `categorias` property (line 50)
  - Remove `categoriaDropdownOpen` signal (line 59)
  - Remove `categoriaFilter` signal (line 60)
  - Remove `filteredCategorias` computed (lines 70-74)
  - Remove `selectCategoria()` method (lines 163-167)
  - Remove `onCategoriaInput()` method (lines 169-175)
  - Remove `toggleCategoriaDropdown()` method (lines 177-179)
  - Remove `onDocumentClick()` host listener (lines 181-187)
  - Add `TreeSelectComponent` to imports array
  - Add `CategoryService` import and inject:
    ```typescript
    import { CategoryService } from '../../services/category.service';
    import { TreeSelectComponent } from './tree-select.component';
    ```
    ```typescript
    private readonly categoryService = inject(CategoryService);
    ```
  - Add a `categoryTree` signal:
    ```typescript
    categoryTree = signal<Category[]>([]);
    ```
  - Import `Category` type:
    ```typescript
    import { Category } from '../../models/category.model';
    ```
  - Load categories on init. Since the component uses constructor, add an `ngOnInit` call or load in constructor:
    ```typescript
    constructor(...) {
      // ... existing form setup ...
      this.loadCategories();
    }

    private async loadCategories(): Promise<void> {
      try {
        const tree = await this.categoryService.getTree();
        this.categoryTree.set(tree);
      } catch {
        // Silent fail — categories will be empty
      }
    }
    ```
  - Add a method to handle tree-select value change:
    ```typescript
    onCategoryChange(categoryId: string): void {
      this.form.patchValue({ categoria: categoryId });
      this.form.get('categoria')?.markAsTouched();
    }
    ```

- [ ] **2.2** In `product-form.component.html`, replace the entire custom category dropdown (lines 71-106) with `<app-tree-select>`:

```html
<!-- BEFORE: the entire <div class="field"> block for Categoria -->

<!-- AFTER -->
<app-form-field
  label="Categoria"
  [required]="true"
  [error]="categoria.touched && categoria.hasError('required') ? 'Categoria é obrigatória' : ''">
  <app-tree-select
    [categories]="categoryTree()"
    [value]="form.get('categoria')?.value || null"
    (valueChange)="onCategoryChange($event)"
  ></app-tree-select>
</app-form-field>
```

- [ ] **2.3** In `product-form.component.scss`, remove the custom `.categoria-dropdown`, `.dropdown-trigger`, `.dropdown-list`, `.dropdown-item`, and `.dropdown-empty` styles (lines 309-398) since they are no longer used.

- [ ] **2.4** The form control `categoria` now stores a GUID (from `TreeSelectComponent.valueChange`). The `loadProduct` method already patches `categoria: product.categoryId` (line 129), and `onSave` already maps `categoryId: formValue.categoria` (line 231). Both are correct for GUID. No changes needed here.

- [ ] **2.5** Verify build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **2.6** Commit: `feat: SP1-1.2 - Replace mocked categories with TreeSelect + API`

---

## Task 3: Category Search Endpoint + Frontend Service (Spec 1.3)

**Problem:** Need a `GET /api/categories/search?q={query}` endpoint that returns matching categories plus their ancestor chains, and a frontend `search()` method in `CategoryService`.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.API/Controllers/CategoriesController.cs`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/services/category.service.ts`

**Steps:**

- [ ] **3.1** In `CategoriesController.cs`, add a new `[HttpGet("search")]` endpoint before the `[HttpGet("{id:guid}")]` endpoint (route ordering matters — specific routes before parameterized):

```csharp
[HttpGet("search")]
public async Task<ActionResult<IReadOnlyList<CategoryListDto>>> SearchCategories(
    [FromQuery] string q = "")
{
    if (string.IsNullOrWhiteSpace(q))
        return Ok(Array.Empty<CategoryListDto>());

    var term = q.ToLower();

    // Find all categories matching the search term
    var matchingIds = await _db.Categories
        .AsNoTracking()
        .Where(c => c.Name.ToLower().Contains(term))
        .Select(c => c.Id)
        .ToListAsync();

    if (matchingIds.Count == 0)
        return Ok(Array.Empty<CategoryListDto>());

    // Load all categories to reconstruct ancestor chains
    var allCategories = await _db.Categories
        .AsNoTracking()
        .ToListAsync();

    var resultIds = new HashSet<Guid>(matchingIds);

    // For each match, walk up the parent chain and include ancestors
    foreach (var id in matchingIds)
    {
        var current = allCategories.FirstOrDefault(c => c.Id == id);
        while (current?.ParentId != null)
        {
            resultIds.Add(current.ParentId.Value);
            current = allCategories.FirstOrDefault(c => c.Id == current.ParentId.Value);
        }
    }

    // Build HasChildren lookup
    var parentIds = allCategories
        .Where(c => c.ParentId != null && resultIds.Contains(c.ParentId.Value))
        .Select(c => c.ParentId!.Value)
        .Distinct()
        .ToHashSet();

    var result = allCategories
        .Where(c => resultIds.Contains(c.Id))
        .OrderBy(c => c.Name)
        .Select(c => new CategoryListDto(
            c.Id,
            c.Name,
            c.Slug,
            c.ParentId,
            c.Icon,
            c.IsActive,
            c.ProductCount,
            c.Order,
            parentIds.Contains(c.Id)))
        .ToList();

    return Ok(result);
}
```

- [ ] **3.2** In `category.service.ts`, add a `search()` method:

```typescript
async search(query: string): Promise<Category[]> {
  const params = new HttpParams().set('q', query);
  const categories = await firstValueFrom(
    this.http.get<Category[]>(`${this.baseUrl}/search`, { params }),
  );
  return categories;
}
```

- [ ] **3.3** Verify backend build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

- [ ] **3.4** Verify frontend build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **3.5** Commit: `feat: SP1-1.3 - Add category search endpoint with ancestor chain reconstruction`

---

## Task 4: Search Debounce in TreeSelect (Spec 1.4)

**Problem:** TreeSelect's search input fires on every keystroke. Need 300ms debounce when calling the API search.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/tree-select.component.ts`

**Steps:**

- [ ] **4.1** In `tree-select.component.ts`, add a debounced search mechanism. The component currently does client-side filtering via `searchQuery` signal in the `flatNodes` computed. We need to add an API-based search that debounces:

```typescript
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { DestroyRef, inject as injectDR } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
```

Add to the class:

```typescript
private readonly destroyRef = inject(DestroyRef);
private readonly searchSubject = new Subject<string>();
readonly searchResults = signal<Category[] | null>(null); // null = use full tree

constructor() {
  // Note: TreeSelectComponent doesn't have a constructor yet, add one
  this.searchSubject.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    takeUntilDestroyed(this.destroyRef),
  ).subscribe(async (query) => {
    if (!query.trim()) {
      this.searchResults.set(null);
      return;
    }
    try {
      const results = await this.categoryService.search(query);
      this.searchResults.set(results);
    } catch {
      this.searchResults.set(null);
    }
  });
}
```

- [ ] **4.2** Update `onSearchInput` to push to the subject:

```typescript
onSearchInput(event: Event): void {
  const value = (event.target as HTMLInputElement).value;
  this.searchQuery.set(value);
  this.focusedIndex.set(-1);
  this.searchSubject.next(value);
}
```

- [ ] **4.3** Update `flatNodes` computed to use `searchResults` when available (API results) or fall back to client-side filtering of the `categories()` input:

```typescript
/** The effective tree: API search results when searching, or the full input tree */
private effectiveCategories = computed((): Category[] => {
  const apiResults = this.searchResults();
  if (apiResults !== null && this.searchQuery().trim()) {
    // Build tree from flat API results
    return this.buildTreeFromFlat(apiResults);
  }
  return this.categories();
});

private buildTreeFromFlat(flat: Category[]): Category[] {
  const map = new Map<string, Category>();
  const roots: Category[] = [];

  for (const cat of flat) {
    map.set(cat.id, { ...cat, children: [] });
  }

  for (const cat of map.values()) {
    if (cat.parentId && map.has(cat.parentId)) {
      map.get(cat.parentId)!.children.push(cat);
    } else if (!cat.parentId || !map.has(cat.parentId)) {
      roots.push(cat);
    }
  }

  return roots;
}
```

Update `flatNodes` to use `this.effectiveCategories()` instead of `this.categories()`:

```typescript
flatNodes = computed((): FlatNode[] => {
  const query = this.searchQuery().toLowerCase();
  const expanded = this.expandedIds();
  const nodes: FlatNode[] = [];

  // ... matchesSearch stays the same ...

  const flatten = (cats: Category[], depth: number) => {
    // ... same logic ...
  };

  flatten(this.effectiveCategories(), 0);  // <-- changed from this.categories()
  return nodes;
});
```

- [ ] **4.4** Reset search results when closing:

```typescript
close(): void {
  this.isOpen.set(false);
  this.searchQuery.set('');
  this.searchResults.set(null);
}
```

- [ ] **4.5** Verify build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **4.6** Commit: `feat: SP1-1.4 - Add 300ms debounced search to TreeSelect with API fallback`

---

## Task 5: Disable Product Toggle (Spec 1.5)

**Problem:** No way to toggle a product's `IsActive` status from the form. Backend already supports `UpdateProductDto.IsActive`.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.scss`

**Steps:**

- [ ] **5.1** In `product-form.component.ts`:
  - Import `ConfirmDialogService` and `ToastService`:
    ```typescript
    import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
    import { ToastService } from '../../services/toast.service';
    ```
  - Inject them:
    ```typescript
    private readonly confirmDialog = inject(ConfirmDialogService);
    private readonly toast = inject(ToastService);
    ```
  - Add an `isActive` signal:
    ```typescript
    isActive = signal(true);
    ```
  - In `loadProduct()`, after patching the form, set `isActive`:
    ```typescript
    this.isActive.set(product.isActive ?? true);
    ```
    Note: The `Product` interface in `product.service.ts` does not currently have `isActive`. The `ProductDetailDto` backend does return it. Need to check if the frontend `Product` interface already has it — it doesn't have `isActive` explicitly but the backend returns it. **Add `isActive: boolean;` to the `Product` interface in `product.service.ts`.**
  - Add `toggleActive()` method:
    ```typescript
    async toggleActive(): Promise<void> {
      const currentlyActive = this.isActive();
      const action = currentlyActive ? 'desativar' : 'ativar';
      const confirmed = await this.confirmDialog.confirm({
        title: `${currentlyActive ? 'Desativar' : 'Ativar'} Produto`,
        message: `Tem certeza que deseja ${action} o produto "${this.productName()}"?`,
        confirmLabel: currentlyActive ? 'Desativar' : 'Ativar',
        variant: currentlyActive ? 'warning' : 'primary',
      });

      if (!confirmed) return;

      try {
        await this.productService.update(this.productId(), {
          isActive: !currentlyActive,
        });
        this.isActive.set(!currentlyActive);
        this.confirmDialog.done();
        this.toast.show(
          `Produto ${!currentlyActive ? 'ativado' : 'desativado'} com sucesso`,
          'success',
        );
      } catch {
        this.confirmDialog.done();
        this.toast.show('Erro ao alterar status do produto', 'danger');
      }
    }
    ```

- [ ] **5.2** In `product-form.component.html`, add a toggle button in the page header, only in edit mode. After the `<h1>` tag (line 14), before the closing `</div>` of `.page-header`:

```html
@if (isEditMode()) {
  <app-button
    [variant]="isActive() ? 'ghost' : 'accent'"
    size="sm"
    (click)="toggleActive()"
  >
    {{ isActive() ? 'Desativar' : 'Ativar' }}
  </app-button>
}
```

- [ ] **5.3** In `product.service.ts`, add `isActive` to the `Product` interface if not present. Currently missing — add it after `status`:
```typescript
isActive: boolean;
```

- [ ] **5.4** Verify build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **5.5** Commit: `feat: SP1-1.5 - Add disable/enable product toggle with confirmation`

---

## Task 6: Delete Product — Backend Endpoint + Frontend (Spec 1.6)

**Problem:** No `DELETE /api/products/{id}` endpoint exists. Need soft/hard delete logic and frontend button with confirmation.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.API/Controllers/ProductsController.cs`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/services/product.service.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`

**Steps:**

- [ ] **6.1** In `ProductsController.cs`, add a `[HttpDelete("{id:guid}")]` endpoint after the `UpdateProduct` method:

```csharp
[HttpDelete("{id:guid}")]
public async Task<IActionResult> DeleteProduct(Guid id)
{
    var product = await _db.Products
        .Include(p => p.Variants)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (product is null)
        return NotFound();

    // Check if product has order history
    var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);

    if (hasOrders)
    {
        // Soft delete: deactivate and set status
        product.IsActive = false;
        product.Status = "Excluído";
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
    else
    {
        // Hard delete: remove product and its variants
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
    }

    await _dispatcher.BroadcastDataChangeAsync("product", "deleted", id.ToString(), default);
    return NoContent();
}
```

- [ ] **6.2** In `product.service.ts`, add the `delete` method:

```typescript
async delete(id: string): Promise<void> {
  await firstValueFrom(
    this.http.delete<void>(`${this.baseUrl}/${id}`),
  );
}
```

- [ ] **6.3** In `product-form.component.ts`, add an `import` for `Trash2` icon from lucide and add `deleteProduct()` method:

```typescript
import { LucideAngularModule, ArrowLeft, Save, X, ChevronDown, ChevronUp, Trash2 } from 'lucide-angular';
```

```typescript
readonly trashIcon = Trash2;
```

```typescript
async deleteProduct(): Promise<void> {
  const confirmed = await this.confirmDialog.confirm({
    title: 'Excluir Produto',
    message: `Tem certeza que deseja excluir o produto "${this.productName()}"?`,
    confirmLabel: 'Excluir',
    variant: 'danger',
  });

  if (!confirmed) return;

  try {
    await this.productService.delete(this.productId());
    this.confirmDialog.done();
    this.toast.show('Produto excluído com sucesso', 'success');
    this.router.navigate(['/produtos']);
  } catch {
    this.confirmDialog.done();
    this.toast.show('Erro ao excluir produto', 'danger');
  }
}
```

- [ ] **6.4** In `product-form.component.html`, add a delete button in the bottom bar, only in edit mode. Inside `.bottom-bar`, before the existing cancel button:

```html
@if (isEditMode()) {
  <app-button variant="danger" [icon]="trashIcon" [disabled]="loading()" (click)="deleteProduct()">
    Excluir
  </app-button>
}
```

Restructure the bottom bar so delete is on the left and save/cancel are on the right:

```html
<div class="bottom-bar">
  <div class="bottom-bar-left">
    @if (isEditMode()) {
      <app-button variant="danger" [icon]="trashIcon" [disabled]="loading()" (click)="deleteProduct()">
        Excluir
      </app-button>
    }
  </div>
  <div class="bottom-bar-actions">
    @if (isEditMode()) {
      <app-button variant="ghost" [disabled]="loading()" (click)="onCancel()">
        Cancelar
      </app-button>
    }
    <app-button variant="accent" [icon]="saveIcon" [loading]="loading()" [disabled]="loading()" (click)="onSave()">
      {{ loading() ? 'Salvando...' : 'Salvar' }}
    </app-button>
  </div>
</div>
```

- [ ] **6.5** In `product-form.component.scss`, add styling for `.bottom-bar-left`:

```scss
.bottom-bar-left {
  display: flex;
  gap: var(--space-3);
}
```

- [ ] **6.6** Verify backend build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

- [ ] **6.7** Verify frontend build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **6.8** Commit: `feat: SP1-1.6 - Add delete product endpoint and frontend button with confirmation`

---

## Task 7: Cancel Button — Edit Only (Spec 1.7)

**Problem:** Cancel button shows in both create and edit modes. Should only show in edit mode.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`

**Steps:**

- [ ] **7.1** This is already handled in Task 6 step 6.4 — the cancel button is wrapped in `@if (isEditMode())`. If Task 6 was implemented correctly, this is already done.

If implementing independently (without Task 6's restructuring), wrap the existing cancel button in `product-form.component.html` (line 263):

```html
<!-- BEFORE -->
<app-button variant="ghost" [disabled]="loading()" (click)="onCancel()">
  Cancelar
</app-button>

<!-- AFTER -->
@if (isEditMode()) {
  <app-button variant="ghost" [disabled]="loading()" (click)="onCancel()">
    Cancelar
  </app-button>
}
```

- [ ] **7.2** Verify build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **7.3** Commit: `fix: SP1-1.7 - Show cancel button only in edit mode`

---

## Task 8: SKU Auto-Suggestion — Backend Migration + Endpoint + Frontend (Spec 1.8)

**Problem:** No SKU auto-generation. Need `SkuPrefix` on Category entity, a migration, a `GET /api/products/next-sku?categoryId={id}` endpoint, and frontend logic.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Core/Entities/Category.cs`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Application/DTOs/Categories/CategoryDtos.cs`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.API/Controllers/CategoriesController.cs`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.API/Controllers/ProductsController.cs`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/models/category.model.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/services/product.service.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`

**Steps:**

### 8a: Backend — Category.SkuPrefix

- [ ] **8.1** In `Category.cs`, add the `SkuPrefix` property:

```csharp
public string? SkuPrefix { get; set; }
```

- [ ] **8.2** Create the EF migration:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet ef migrations add AddCategorySkuPrefix --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API
```

Review the generated migration to ensure it only adds `SkuPrefix VARCHAR(5) NULL` to Categories.

- [ ] **8.3** Update `CategoryListDto` and `CategoryDetailDto` to include `SkuPrefix`:

In `CategoryDtos.cs`:

```csharp
public record CategoryListDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    string? Icon,
    bool IsActive,
    int ProductCount,
    int Order,
    bool HasChildren,
    string? SkuPrefix);  // <-- add

public record CategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    string? ParentName,
    string? Icon,
    bool IsActive,
    int ProductCount,
    int Order,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<CategoryListDto> Children,
    string? SkuPrefix);  // <-- add

public record CreateCategoryDto(
    string Name,
    string Slug,
    Guid? ParentId,
    string? Icon,
    int Order,
    string? SkuPrefix);  // <-- add

public record UpdateCategoryDto(
    string? Name,
    string? Slug,
    Guid? ParentId,
    string? Icon,
    bool? IsActive,
    int? Order,
    string? SkuPrefix);  // <-- add
```

- [ ] **8.4** Update ALL places in `CategoriesController.cs` that construct `CategoryListDto` and `CategoryDetailDto` to include the new `SkuPrefix` parameter. There are multiple `new CategoryListDto(...)` and `new CategoryDetailDto(...)` calls in:
  - `GetCategories` — two places (initial select + result with HasChildren)
  - `GetCategory` — children select + main dto
  - `CreateCategory` — result dto
  - `UpdateCategory` — children select + result dto

For each `CategoryListDto` construction, add `c.SkuPrefix` as the last parameter.
For each `CategoryDetailDto` construction, add `category.SkuPrefix` as the last parameter.

Also update `CreateCategory` to set `SkuPrefix`:
```csharp
var category = new Category
{
    // ... existing properties ...
    SkuPrefix = dto.SkuPrefix,
};
```

And `UpdateCategory`:
```csharp
if (dto.SkuPrefix is not null) category.SkuPrefix = dto.SkuPrefix;
```

### 8b: Backend — Next SKU Endpoint

- [ ] **8.5** In `ProductsController.cs`, add the `next-sku` endpoint. Place it BEFORE the `[HttpGet("{id:guid}")]` route to avoid routing conflicts:

```csharp
[HttpGet("next-sku")]
public async Task<ActionResult> GetNextSku([FromQuery] Guid? categoryId)
{
    if (!categoryId.HasValue)
        return Ok(new { suggestedSku = (string?)null });

    var category = await _db.Categories
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == categoryId.Value);

    if (category?.SkuPrefix is null or "")
        return Ok(new { suggestedSku = (string?)null });

    var prefix = category.SkuPrefix;
    var pattern = $"{prefix}-";

    // Find the max existing SKU with this prefix
    var maxSku = await _db.Products
        .AsNoTracking()
        .Where(p => p.Sku.StartsWith(pattern))
        .Select(p => p.Sku)
        .OrderByDescending(s => s)
        .FirstOrDefaultAsync();

    int nextNumber = 1;
    if (maxSku is not null)
    {
        var suffix = maxSku[(pattern.Length)..];
        if (int.TryParse(suffix, out var parsed))
        {
            nextNumber = parsed + 1;
        }
    }

    var suggestedSku = $"{prefix}-{nextNumber:D3}";
    return Ok(new { suggestedSku });
}
```

- [ ] **8.6** Verify backend build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

### 8c: Frontend — Category Model + Service + Form

- [ ] **8.7** In `category.model.ts`, add `skuPrefix` to `Category`, `CreateCategoryDto`, and `UpdateCategoryDto`:

```typescript
// In Category interface:
skuPrefix: string | null;

// In CreateCategoryDto interface:
skuPrefix?: string | null;

// In UpdateCategoryDto interface:
skuPrefix?: string | null;
```

- [ ] **8.8** In `product.service.ts`, add a `getNextSku` method:

```typescript
async getNextSku(categoryId: string): Promise<string | null> {
  const result = await firstValueFrom(
    this.http.get<{ suggestedSku: string | null }>(
      `${this.baseUrl}/next-sku`,
      { params: new HttpParams().set('categoryId', categoryId) },
    ),
  );
  return result.suggestedSku;
}
```

- [ ] **8.9** In `product-form.component.ts`, add SKU auto-suggestion logic:

Add a signal to track if SKU was auto-generated:
```typescript
skuAutoGenerated = signal(false);
```

Update `onCategoryChange()` to trigger SKU suggestion:
```typescript
async onCategoryChange(categoryId: string): Promise<void> {
  this.form.patchValue({ categoria: categoryId });
  this.form.get('categoria')?.markAsTouched();

  // Auto-suggest SKU if field is empty or was previously auto-generated
  const currentSku = this.form.get('sku')?.value || '';
  if (!currentSku || this.skuAutoGenerated()) {
    await this.suggestSku(categoryId);
  }
}

private async suggestSku(categoryId: string): Promise<void> {
  try {
    const suggested = await this.productService.getNextSku(categoryId);
    if (suggested) {
      this.form.patchValue({ sku: suggested });
      this.skuAutoGenerated.set(true);
    }
  } catch {
    // Silent fail — user can still type SKU manually
  }
}
```

Add a method to manually trigger suggestion:
```typescript
async onSuggestSku(): Promise<void> {
  const categoryId = this.form.get('categoria')?.value;
  if (categoryId) {
    await this.suggestSku(categoryId);
  }
}
```

Track manual SKU edits to clear auto-generated flag. In the constructor, after form creation:
```typescript
this.form.get('sku')?.valueChanges.subscribe(() => {
  // If user is typing, mark as manually edited
  // (suggestSku sets skuAutoGenerated AFTER patchValue, so this fires first then gets overridden)
});
```

Actually, a simpler approach: listen for user input on the SKU field via `(input)` event in the template to set `skuAutoGenerated.set(false)`.

- [ ] **8.10** In `product-form.component.html`, add a "Sugerir" link next to the SKU field. Replace the SKU `<app-form-field>` block:

```html
<app-form-field label="SKU">
  <div class="sku-field-row">
    <input
      id="sku"
      type="text"
      formControlName="sku"
      placeholder="Ex: TWS-PRO-001"
      (input)="skuAutoGenerated.set(false)"
    />
    @if (form.get('categoria')?.value) {
      <button type="button" class="sku-suggest-link" (click)="onSuggestSku()">
        Sugerir
      </button>
    }
  </div>
</app-form-field>
```

- [ ] **8.11** In `product-form.component.scss`, add styling for the suggest link:

```scss
.sku-field-row {
  display: flex;
  align-items: center;
  gap: var(--space-2);

  input {
    flex: 1;
  }
}

.sku-suggest-link {
  border: none;
  background: none;
  color: var(--primary);
  font-size: var(--body-small-size);
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
  padding: var(--space-1) var(--space-2);
  border-radius: var(--radius-sm);

  &:hover {
    background: var(--primary-light);
  }
}
```

- [ ] **8.12** Verify frontend build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **8.13** Commit: `feat: SP1-1.8 - Add SkuPrefix to Category, next-sku endpoint, and SKU auto-suggestion in form`

---

## Task 9: Save Redirects to View (Spec 1.9)

**Problem:** After save, create mode navigates to edit, and edit mode stays on edit. Both should navigate to the view/detail page.

**Files:**
- `/workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`

**Steps:**

- [ ] **9.1** In `product-form.component.ts`, update the `onSave()` method. Replace the navigation and success logic (inside the `try` block, after the `if/else`):

```typescript
async onSave(): Promise<void> {
  if (this.form.invalid) {
    this.form.markAllAsTouched();
    if (this.titulo.invalid || this.categoria.invalid) {
      this.activeTab.set('basicas');
    } else if (this.precoVenda.invalid) {
      this.activeTab.set('preco');
    }
    return;
  }

  this.loading.set(true);
  try {
    const formValue = this.form.value;
    const dto = {
      name: formValue.titulo,
      sku: formValue.sku || undefined,
      description: formValue.descricao || undefined,
      categoryId: formValue.categoria || undefined,
      supplier: formValue.fornecedor || undefined,
      price: formValue.precoVenda,
      acquisitionCost: formValue.custoAquisicao ?? undefined,
      weight: formValue.peso ?? undefined,
      height: formValue.altura ?? undefined,
      width: formValue.largura ?? undefined,
      length: formValue.comprimento ?? undefined,
    };

    let productId: string;
    if (this.isEditMode()) {
      await this.productService.update(this.productId(), dto);
      productId = this.productId();
    } else {
      const created = await this.productService.create(dto);
      productId = created.id;
    }

    this.form.markAsPristine();
    this.toast.show('Produto salvo com sucesso', 'success');
    this.router.navigate(['/produtos', productId]);
  } catch {
    this.toast.show('Erro ao salvar produto', 'danger');
  } finally {
    this.loading.set(false);
  }
}
```

Key changes:
- Both create and edit now navigate to `/produtos/{id}` (the view/detail page) instead of `/produtos/{id}/editar`
- Toast notification on success and error
- `productId` is extracted to a local variable for clarity

- [ ] **9.2** Verify that the route `/produtos/:id` exists and maps to the product detail/view component. Check the routing module:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && grep -r "produtos" src/app/app.routes.ts 2>/dev/null || grep -rn "produtos" src/app/ --include="*.routes.ts" | head -20
```

If the route does not exist, the navigation will fail silently. Verify and add if missing.

- [ ] **9.3** Verify build:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **9.4** Commit: `feat: SP1-1.9 - Save redirects to product view page with toast notification`

---

## Summary

| Task | Spec Item | Description | Backend | Frontend |
|------|-----------|-------------|---------|----------|
| 1 | 1.1 | Form layout & dropdown clipping | - | HTML + SCSS |
| 2 | 1.2 | Replace mocked categories with TreeSelect | - | TS + HTML + SCSS |
| 3 | 1.3 | Category search endpoint | C# endpoint | Service method |
| 4 | 1.4 | Search debounce (300ms) | - | TreeSelect TS |
| 5 | 1.5 | Disable/enable product toggle | - | TS + HTML |
| 6 | 1.6 | Delete product | C# endpoint | Service + TS + HTML |
| 7 | 1.7 | Cancel button edit-only | - | HTML |
| 8 | 1.8 | SKU auto-suggestion | C# migration + endpoint | Model + Service + TS + HTML + SCSS |
| 9 | 1.9 | Save redirects to view | - | TS |

**Recommended execution order:** Tasks 1-4 (category chain), then 5-7 (form actions), then 8 (SKU — has DB migration), then 9 (save behavior). Tasks 5, 6, 7 can be done in parallel if needed.

**Migration note:** Task 8 includes an EF Core migration (`AddCategorySkuPrefix`). Run `dotnet ef database update` after the migration is created to apply it to the dev database.
