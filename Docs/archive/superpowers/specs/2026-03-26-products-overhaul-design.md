# Products Overhaul Design

**Date:** 2026-03-26
**Scope:** Product Edit Form, View Form, List — fixes, missing features, and backend analytics

## Summary

Complete overhaul of the Products area covering 20+ items across the edit form, view form, and list. Organized into 4 sub-projects: SP1 (Form Fixes), SP2 (Media & Variants), SP3 (List Fixes), SP4 (View & Analytics). SP1 and SP2 are independent. SP3 depends on SP1's category infrastructure. SP4 depends on SP3's margin calculation.

## Sub-project Dependencies

```
SP1 (Form Fixes) ──────┐
                        ├──→ SP3 (List Fixes) ──→ SP4 (View & Analytics)
SP2 (Media & Variants) ─┘
```

---

## SP1: Product Form Fixes

### 1.1 Form Layout & Category Clipping (Edit #1-2)

**Problem:** Category dropdown clips against parent `overflow: hidden`. Form sections may be missing the `.form-layout` class.

**Fix:**
- Audit all form tab panels in `product-form.component.html` and ensure they use `class="form-layout"`
- Fix category dropdown clipping by adjusting `overflow` on the containing section or moving the dropdown to use a portal/overlay pattern
- Ensure the form-layout spacing applies consistently across all tabs

### 1.2 Replace Mocked Categories with TreeSelect (Edit #3-4)

**Problem:** Product form uses a hardcoded 12-item string array. A `TreeSelectComponent` already exists in the codebase but is not wired in.

**Fix:**
- Remove the hardcoded `CATEGORIAS` array from `product-form.component.ts`
- Import and inject `CategoryService`
- Load categories via `CategoryService.getTree()` on component init
- Replace the custom dropdown markup with `<app-tree-select>` component
- The form control `categoria` changes from storing a name string to storing a category **ID (GUID)**
- Update the save DTO mapping: `categoryId: formValue.categoria` (already expects GUID on backend)

**TreeSelect already supports:**
- Hierarchical expand/collapse
- Search by category name
- Keyboard navigation
- Breadcrumb display of selected item

### 1.3 Category Search Endpoint (Edit #4)

**New endpoint:** `GET /api/categories/search?q={query}`

**Behavior:**
- Search categories by `Name` (case-insensitive, contains match)
- For each matching category, reconstruct the full parent chain recursively
- Return a flat list of all matched categories plus their ancestors (deduplicated)
- Frontend TreeSelect uses this for filtered results

**Response:** `IReadOnlyList<CategoryListDto>` (same shape as existing list endpoint)

**Frontend:**
- Add `search(query: string): Promise<Category[]>` to `CategoryService`
- TreeSelect calls this when search input has text, falls back to full tree when empty

### 1.4 Search Debounce (Edit #5)

- Add 300ms debounce on the TreeSelect search input
- Use RxJS `Subject` + `debounceTime` or a simple `setTimeout`/`clearTimeout` pattern
- Prevents rapid API calls while typing

### 1.5 Disable Product (Edit #8)

- Add a toggle or button in the form header: "Desativar Produto" / "Ativar Produto"
- Uses existing `UpdateProductDto.IsActive` (boolean) — backend already supports this
- Show confirmation dialog via `ConfirmDialogService.confirm()` before toggling
- After toggling, update the local product state and show a toast notification
- Only visible in edit mode (not create mode)

### 1.6 Delete Product (Edit #9)

**New endpoint:** `DELETE /api/products/{id}`

**Backend behavior:**
- Soft delete or hard delete (check if product has order history — if yes, soft delete by setting `IsActive = false` and `Status = "Excluído"`, if no, hard delete)
- Return 204 No Content on success
- Return 409 Conflict if product has active orders

**Frontend:**
- Add "Excluir" button (danger style) in form actions, only in edit mode
- Confirmation dialog: "Tem certeza que deseja excluir o produto **{name}**?"
- Show processing spinner during delete
- On success, navigate to `/produtos` list with toast "Produto excluído com sucesso"

**ProductService:** Add `delete(id: string): Promise<void>` method

### 1.7 Cancel Button — Edit Only (Edit #10)

**Current state:** Cancel button exists but shows in both create and edit modes.

**Fix:** Conditionally render Cancel button only when `isEditMode` is true. In create mode, the back arrow in the header is sufficient.

### 1.8 SKU Auto-Suggestion (Edit #11)

**Database changes:**
- Add `SkuPrefix` column (string, max 5 chars, nullable) to `Category` entity
- Migration: `ALTER TABLE Categories ADD COLUMN SkuPrefix VARCHAR(5) NULL`
- Seed data: populate prefixes for existing categories (e.g., "ELE" for Eletrônicos, "CEL" for Celulares)

**New endpoint:** `GET /api/products/next-sku?categoryId={id}`

**Backend behavior:**
1. Look up category's `SkuPrefix`
2. If no prefix, return empty (no suggestion)
3. Query max existing SKU matching pattern `{prefix}-%`
4. Parse the numeric suffix, increment by 1
5. Return: `{ "suggestedSku": "ELE-001" }`

**Frontend behavior:**
- When user selects a category AND the SKU field is empty (or was previously auto-generated):
  - Call the next-sku endpoint
  - Auto-fill the SKU field with the suggestion
  - Mark the field as "auto-generated" (so future category changes re-trigger)
- If user has manually typed a SKU, don't overwrite it
- Show a small "Sugerir" link/button next to the SKU field to manually trigger suggestion

**CategoryService:** Update `CreateCategoryDto` and `UpdateCategoryDto` to include optional `skuPrefix` field.

### 1.9 Save Redirects to View (Edit #13)

**Current behavior:**
- Create mode: saves then navigates to `/produtos/{id}/editar` (edit mode)
- Edit mode: saves and stays on edit form

**New behavior:**
- Both create and edit: after successful save, navigate to `/produtos/{id}` (view/detail page)
- Show toast: "Produto salvo com sucesso"

---

## SP2: Product Form Media & Variants

### 2.1 Image Upload (Edit #6)

**Problem:** "Adicionar" button in the image gallery does nothing.

**Check/Create endpoint:** `POST /api/uploads` (multipart form-data)
- Accepts: file (image), entityType ("product"), entityId (product GUID)
- Stores file (local disk or cloud storage depending on config)
- Returns: `{ id, url, fileName }` saved to `FileUploads` table
- If endpoint doesn't exist, create it

**Frontend fix in MediaGalleryComponent:**
- Wire "Adicionar" button to trigger hidden `<input type="file" accept="image/*">`
- On file selected: show loading state, upload via service, add returned URL to gallery
- Update parent form state via callback

### 2.2 Image Delete Confirmation (Edit #7)

**Current behavior:** Image remove happens immediately without confirmation.

**Fix:**
- Before removing, call `ConfirmDialogService.confirm()` with message: "Tem certeza que deseja remover esta imagem?"
- During processing: `startProcessing()` to show spinner and block interaction
- On confirm: call delete endpoint (if backend-stored), remove from local gallery array
- On cancel: no-op

### 2.3 Variants Component Check (Edit #12)

**Audit:** Check if `VariantManagerComponent` uses DataGridComponent or plain HTML tables.
- If plain HTML, migrate the variants list to use DataGridComponent
- Ensure column definitions match: variant attribute fields + Price + Stock (read-only) + SKU + Status

### 2.4 Variant Behavior Fixes (Edit #14)

**Stock is read-only:**
- Remove any stock input/edit capability from the variant form
- Stock is calculated from inventory movements, not manually set
- Display stock as a read-only value in the variants table
- Manual stock adjustments happen from the Inventory screen, not here

**Variant save not working:**
- Debug the save flow: verify `ProductVariantService.update()` calls the correct endpoint
- Ensure the endpoint `PUT /api/products/{id}/variants/{variantId}` exists and works
- Fields that CAN be edited on a variant: Price, SKU, Status (Ativo/Pausado)

**Variant delete:**
- Variants ARE individually deletable (confirmed in categories-variants plan)
- Each variant row has a delete button
- Show confirmation dialog: "Tem certeza que deseja excluir esta variação?"
- Call `DELETE /api/products/{id}/variants/{variantId}` (verify endpoint exists)
- On success, remove from local list — no re-fetch needed

**Variant creation:**
- Variants should auto-generate based on category's variation fields (e.g., Color has [Preto, Branco], Size has [P, M, G] → 6 variants)
- When category changes, prompt: "Deseja regenerar variações com base na nova categoria?"

---

## SP3: Product List Fixes

### 3.1 Category Filter (List #1)

**Frontend:**
- Add a category filter dropdown in the action bar (alongside search and status filter)
- Use a simplified version of TreeSelect or a flat dropdown with indented names showing hierarchy
- On category change: reset page, reload with `categoryId` parameter

**Backend:**
- Add `categoryId` query parameter to `GET /api/products`
- When `categoryId` is provided, filter products where `CategoryId` is the selected category OR any of its descendants
- Use recursive CTE or `CategoryService.getDescendantIds()` to get all descendant IDs

### 3.2 Stock Column (List #2)

**Problem:** `ProductListDto` has no `Stock` field. The `Product` entity has no stock field — stock lives on variants and inventory.

**Backend fix:**
- Add `Stock` (int) to `ProductListDto`
- In the products list query, compute stock as: `SUM(variant.Stock)` via a left join on `ProductVariants`
- If product has no variants, stock = 0 (or pull from inventory if applicable)

**Frontend:** Column already exists in the grid — it will render once the backend sends the value.

### 3.3 Margin Calculation (List #3)

**Problem:** `ProductListDto` has no `Margin` field. Frontend shows 0.0% for all products.

**Backend fix:**
- Add `Margin` (decimal?) to `ProductListDto`
- Compute in query: `((Price - PurchaseCost - PackagingCost) / Price) * 100` when `Price > 0`
- If `Price = 0`, margin is null (show "—" in UI)

**Frontend:** Column already renders with color coding. Just needs the value from backend.

---

## SP4: Product View & Analytics

### 4.1 Missing Fields in View Mode (View #1)

**Problem:** Detail view only shows name, SKU, status, image thumbnail, and KPIs. Missing: description, category, supplier, price, costs, dimensions, gallery.

**Fix — Add info sections:**

**"Informações Gerais" section:**
- Categoria (with breadcrumb path, e.g., "Eletrônicos > Celulares")
- Descrição
- Fornecedor
- SKU (already shown in header)
- Status (already shown in header)

**"Preço e Custos" section:**
- Preço de Venda (formatted BRL)
- Custo de Aquisição (formatted BRL)
- Custo de Embalagem (formatted BRL)
- Margem Estimada (computed, color-coded)

**"Dimensões" section:**
- Peso, Altura, Largura, Comprimento

**"Galeria" section:**
- Display all product images in a horizontal scrollable gallery
- Click to open lightbox/modal with full-size image
- Show video player if video URL exists

### 4.2 Stock Area (View #2)

**New section: "Estoque"**

| Metric | Source | Format |
|--------|--------|--------|
| Estoque | Sum of variant stocks (or inventory) | Integer with unit "un." |
| Custo Médio | `Product.PurchaseCost` (average acquisition cost) | BRL currency |
| Custo Total | `Estoque × Custo Médio` | BRL currency |

- Display as 3 KPI cards in a row
- Custo Total is computed client-side from the other two values

### 4.3 Analytics Area with Date Range (View #3)

**New section: "Analytics"**

**Date range selector:**
- Dropdown using existing select component
- Presets: `7 dias`, `30 dias`, `60 dias`, `90 dias`, `180 dias`, `1 ano`
- Default: `30 dias`
- On change: re-fetch analytics data

**KPI cards (same row):**
- Vendas (count)
- Receita (BRL)
- Lucro (BRL, green/red)
- Margem (%, color-coded)
- Each shows value + change % vs previous period

**Below KPIs:**
- "Histórico de Custo" table (DataGridComponent)
- "Vendas Recentes" table (DataGridComponent)

### 4.4 Backend Analytics Endpoints (View #4)

**New endpoint:** `GET /api/products/{id}/analytics?days=30`

**Response:**
```csharp
public class ProductAnalyticsDto
{
    public int TotalSales { get; set; }          // count of order items
    public decimal TotalRevenue { get; set; }     // sum of (qty × price)
    public decimal TotalProfit { get; set; }      // sum of (revenue - costs)
    public decimal? Margin { get; set; }          // (profit / revenue) * 100
    public decimal? SalesChange { get; set; }     // % change vs previous period
    public decimal? RevenueChange { get; set; }
    public decimal? ProfitChange { get; set; }
    public decimal? MarginChange { get; set; }
}
```

**Backend logic:**
1. Query `OrderItems` joined with `Orders` where `ProductId = {id}` and `Order.CreatedAt >= now - {days}`
2. Compute totals for current period
3. Compute same totals for previous period (`now - 2*days` to `now - days`)
4. Calculate change percentages: `((current - previous) / previous) * 100`
5. If no data in previous period, changes are null

**New endpoint:** `GET /api/products/{id}/recent-orders?days=30&page=1&pageSize=10`

**Response:** `PagedResult<ProductRecentOrderDto>`

```csharp
public class ProductRecentOrderDto
{
    public Guid OrderId { get; set; }
    public DateTime Date { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public decimal Profit { get; set; }
}
```

**Backend logic:**
- Query `OrderItems` joined with `Orders` filtered by ProductId and date range
- Compute profit per order item: `(UnitPrice - PurchaseCost - PackagingCost) × Quantity`
- Return paginated, sorted by date descending

**Existing endpoint (already works):** `GET /api/products/{id}/cost-history?page=1&pageSize=10`

### 4.5 Tables Using DataGridComponent (View #5)

**Migrate "Histórico de Custo":**
- Replace plain `<table class="detail-table">` with `<app-data-grid>`
- Columns: Data, Ordem de Compra (link), Qtd, Custo Unitário, Custo Anterior, Novo Custo
- Use `appGridCell` for custom formatting (currency, links, color-coded cost changes)
- Wire `[totalCount]` and `entityName="registros"`

**Migrate "Vendas Recentes":**
- Replace plain `<table>` with `<app-data-grid>`
- Columns: Pedido (link), Data, Qtd, Preço Unitário, Total, Lucro
- Custom cells for currency formatting and profit color coding
- Wire `[totalCount]` and `entityName="vendas"`
- Replace mocked data with new `GET /api/products/{id}/recent-orders` endpoint

---

## Existing Infrastructure to Reuse

| Component/Service | Status | Used by |
|-------------------|--------|---------|
| `TreeSelectComponent` | Exists, not wired | SP1 (product form category) |
| `CategoryService` | Exists with getTree(), getAll() | SP1, SP3 |
| `ConfirmDialogService` | Exists | SP1 (delete/disable), SP2 (image delete) |
| `DataGridComponent` | Exists with footer | SP4 (cost history, recent sales) |
| `SelectDropdownComponent` | Exists | SP4 (date range picker) |
| `KpiCardComponent` | Exists | SP4 (analytics KPIs) |
| `MediaGalleryComponent` | Exists | SP2 (image upload fix) |
| `VariantManagerComponent` | Exists | SP2 (variant fixes) |

## Backend Changes Summary

| Change | Type | Sub-project |
|--------|------|-------------|
| `GET /api/categories/search?q=` | New endpoint | SP1 |
| `DELETE /api/products/{id}` | New endpoint | SP1 |
| `GET /api/products/next-sku?categoryId=` | New endpoint | SP1 |
| `Category.SkuPrefix` column | Migration | SP1 |
| `POST /api/uploads` | New or verify endpoint | SP2 |
| `categoryId` param on `GET /api/products` | Modify endpoint | SP3 |
| `Stock` + `Margin` fields on `ProductListDto` | Modify DTO | SP3 |
| `GET /api/products/{id}/analytics?days=` | New endpoint | SP4 |
| `GET /api/products/{id}/recent-orders?days=` | New endpoint | SP4 |

## Files to Modify (Summary)

### Frontend
- `product-form.component.ts/html/scss` — SP1, SP2
- `product-detail.component.ts/html/scss` — SP4
- `products-list.component.ts/html` — SP3
- `media-gallery.component.ts/html` — SP2
- `variant-manager.component.ts/html` — SP2
- `tree-select.component.ts` — SP1 (debounce)
- `product.service.ts` — SP1 (delete, next-sku), SP4 (analytics, recent-orders)
- `category.service.ts` — SP1 (search method)

### Backend
- `ProductsController.cs` — SP1 (delete), SP3 (categoryId filter, stock, margin), SP4 (analytics, recent-orders)
- `CategoriesController.cs` — SP1 (search endpoint)
- `Category.cs` entity — SP1 (SkuPrefix)
- `ProductDtos.cs` — SP3 (Stock, Margin on list DTO), SP4 (analytics DTOs)
- New migration for SkuPrefix — SP1
- New `ProductAnalyticsDto` / `ProductRecentOrderDto` — SP4
- `UploadsController.cs` — SP2 (verify or create)
