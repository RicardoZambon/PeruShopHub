# SP2: Product Form Media & Variants — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix image upload, add delete confirmation for images, fix variant save/delete behavior, make variant stock read-only.

**Architecture:** Wire existing MediaGalleryComponent to real file upload endpoint (FilesController), add confirmation dialogs, fix VariantManagerComponent behavior.

**Tech Stack:** Angular 17+, standalone components, signals, CDK drag-drop, C# ASP.NET Core 8

**Spec:** `docs/superpowers/specs/2026-03-26-products-overhaul-design.md` (SP2 section)

---

## Task 1: Wire Image Upload to Real Backend (Spec 2.1)

**Problem:** The "Adicionar" button in `MediaGalleryComponent` calls `addMockImage()` which creates a colored placeholder instead of uploading a real file. The backend endpoint `POST /api/files/upload` and the frontend `FileUploadService` both exist but are not wired to the gallery.

**Files to modify:**
- `src/PeruShopHub.Web/src/app/pages/products/media-gallery.component.ts`
- `src/PeruShopHub.Web/src/app/pages/products/media-gallery.component.html`
- `src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`
- `src/PeruShopHub.Web/src/app/pages/products/product-form.component.html`

### Steps

- [ ] **1.1** Update the `GalleryImage` interface in `media-gallery.component.ts` to include real file data:

```typescript
export interface GalleryImage {
  id: string;
  url: string;         // real URL from backend (or color placeholder during upload)
  fileName: string;
  order: number;
}
```

- [ ] **1.2** Add a new `@Input()` for the product ID and inject `FileUploadService` and add a hidden file input ref. Add an `uploading` signal. Replace `addMockImage()` with `triggerFileInput()` and a new `onFileSelected()` method:

```typescript
import { FileUploadService } from '../../services/file-upload.service';
import { firstValueFrom } from 'rxjs';

// Add to component:
readonly productId = input<string>('');
readonly uploading = signal(false);

private fileUploadService = inject(FileUploadService);

triggerFileInput(): void {
  // Programmatically open file picker — the template has a hidden <input type="file">
  const input = document.createElement('input');
  input.type = 'file';
  input.accept = 'image/jpeg,image/png,image/webp';
  input.onchange = (event) => {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.onFileSelected(file);
  };
  input.click();
}

async onFileSelected(file: File): Promise<void> {
  if (this.images().length >= this.maxImages) return;
  if (file.size > 10 * 1024 * 1024) return; // 10MB limit per spec hint text

  const productId = this.productId();
  if (!productId) {
    // Fallback for create mode — queue locally (can't upload without entity ID)
    return;
  }

  this.uploading.set(true);
  try {
    const response = await firstValueFrom(
      this.fileUploadService.upload(file, 'product', productId, this.images().length)
    );
    const newImage: GalleryImage = {
      id: response.id,
      url: response.url,
      fileName: response.fileName,
      order: this.images().length,
    };
    this.images.set([...this.images(), newImage]);
    this.imageAdd.emit();
  } catch {
    // Error handling — could emit an error event
  } finally {
    this.uploading.set(false);
  }
}
```

- [ ] **1.3** Remove the `addMockImage()` method and the `PLACEHOLDER_COLORS` constant entirely (no longer needed).

- [ ] **1.4** Update `media-gallery.component.html` to call `triggerFileInput()` instead of `addMockImage()` on empty slot clicks:

```html
<!-- Replace this line: -->
<div class="image-slot empty" (click)="addMockImage()">

<!-- With: -->
<div class="image-slot empty" (click)="triggerFileInput()">
```

- [ ] **1.5** Update filled image slots in the template to show real images instead of colored placeholders:

```html
<!-- Replace the colored div: -->
<div class="image-content" [style.background-color]="image.color">
  <span class="image-number">{{ i + 1 }}</span>

<!-- With: -->
<div class="image-content">
  @if (image.url) {
    <img [src]="image.url" [alt]="image.fileName" class="image-preview" />
  }
  <span class="image-number">{{ i + 1 }}</span>
```

- [ ] **1.6** Add CSS for the image preview in `media-gallery.component.scss`:

```scss
.image-preview {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: var(--radius-md);
}
```

- [ ] **1.7** Show a loading spinner on empty slots while uploading — add an `@if (uploading())` overlay inside the empty slot area.

- [ ] **1.8** Pass `productId` from the parent `product-form.component.html`:

```html
<app-media-gallery
  [productId]="productId()"
  [images]="galleryImages()"
  [videoUrl]="galleryVideoUrl()"
  (imagesChange)="onGalleryImagesChange($event)"
  (videoUrlChange)="onGalleryVideoChange($event)"
></app-media-gallery>
```

- [ ] **1.9** In `product-form.component.ts`, load existing images when editing a product. Add to `loadProduct()`:

```typescript
// After loading product data, load associated images:
const images = await firstValueFrom(this.fileUploadService.getFiles('product', id));
this.galleryImages.set(images.map((f, i) => ({
  id: f.id,
  url: f.url,
  fileName: f.fileName,
  order: f.sortOrder,
})));
```

This requires injecting `FileUploadService` and importing `firstValueFrom` in `product-form.component.ts`.

- [ ] **1.10** Verify build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **1.11** Commit:

```
feat: SP2-2.1 - Wire image upload to real backend endpoint
```

---

## Task 2: Image Delete Confirmation (Spec 2.2)

**Problem:** `removeImage(index)` in `MediaGalleryComponent` removes the image immediately without any confirmation dialog. Per spec, it must show a `ConfirmDialogService.confirm()` prompt before deleting, and call the backend delete endpoint.

**Files to modify:**
- `src/PeruShopHub.Web/src/app/pages/products/media-gallery.component.ts`

### Steps

- [ ] **2.1** Inject `ConfirmDialogService` into `MediaGalleryComponent`:

```typescript
import { ConfirmDialogService } from '../../shared/components';

private confirmDialog = inject(ConfirmDialogService);
```

- [ ] **2.2** Replace the existing `removeImage(index)` method with an async version that shows confirmation and calls backend delete:

```typescript
async removeImage(index: number): Promise<void> {
  const image = this.images()[index];
  if (!image) return;

  const confirmed = await this.confirmDialog.confirm({
    title: 'Remover imagem',
    message: 'Tem certeza que deseja remover esta imagem?',
    confirmLabel: 'Remover',
    variant: 'danger',
  });

  if (!confirmed) return;

  try {
    // Delete from backend if the image has a real ID (not a local placeholder)
    await firstValueFrom(this.fileUploadService.delete(image.id));
    this.confirmDialog.done();
  } catch {
    this.confirmDialog.done();
    return;
  }

  const updated = this.images()
    .filter((_, i) => i !== index)
    .map((img, i) => ({ ...img, order: i }));
  this.images.set(updated);
  this.imageRemove.emit(index);
}
```

- [ ] **2.3** Verify build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **2.4** Commit:

```
feat: SP2-2.2 - Add confirmation dialog before image deletion
```

---

## Task 3: Variant Backend Endpoints — Create, Update, Delete (Spec 2.3 & 2.4)

**Problem:** The backend only has `GET /api/products/{id}/variants`. The frontend `ProductVariantService` calls `POST /api/products/{productId}/variants`, `PUT /api/variants/{variantId}`, and `DELETE /api/variants/{variantId}`, but these endpoints do not exist on the backend. Variant save, create, and delete all fail silently.

**Files to modify:**
- `src/PeruShopHub.API/Controllers/ProductsController.cs`

**Files to reference:**
- `src/PeruShopHub.Web/src/app/services/product-variant.service.ts` (to see expected URL patterns)
- `src/PeruShopHub.Core/Entities/ProductVariant.cs` (entity shape)

### Steps

- [ ] **3.1** Check the `ProductVariant` entity to understand the database columns:

```bash
cat src/PeruShopHub.Core/Entities/ProductVariant.cs
```

- [ ] **3.2** Check if DTOs for create/update variant exist. Look in `src/PeruShopHub.Application/DTOs/Products/` for variant DTOs. If they don't exist, create them:

```csharp
// CreateProductVariantDto.cs
public record CreateProductVariantDto(
    string Sku,
    Dictionary<string, string> Attributes,
    decimal? Price,
    bool IsActive
);

// UpdateProductVariantDto.cs
public record UpdateProductVariantDto
{
    public string? Sku { get; init; }
    public decimal? Price { get; init; }
    public bool? IsActive { get; init; }
    public int? Stock { get; init; }  // kept for compat but ignored — stock is read-only
}
```

- [ ] **3.3** Add `POST /api/products/{id}/variants` endpoint to `ProductsController.cs`:

```csharp
[HttpPost("{id:guid}/variants")]
public async Task<ActionResult<ProductVariantDto>> CreateVariant(Guid id, CreateProductVariantDto dto)
{
    var product = await _db.Products.FindAsync(id);
    if (product is null) return NotFound();

    var variant = new ProductVariant
    {
        Id = Guid.NewGuid(),
        ProductId = id,
        Sku = dto.Sku,
        Attributes = dto.Attributes,
        Price = dto.Price,
        Stock = 0,  // Stock is managed via inventory, not set on creation
        IsActive = dto.IsActive,
    };

    _db.ProductVariants.Add(variant);
    await _db.SaveChangesAsync();

    return Ok(MapToVariantDto(variant));
}
```

- [ ] **3.4** Add `PUT /api/products/{productId}/variants/{variantId}` endpoint. Note: the frontend calls `PUT /api/variants/{variantId}` (no product ID in URL). Add BOTH routes for compatibility:

```csharp
[HttpPut("{id:guid}/variants/{variantId:guid}")]
public async Task<ActionResult<ProductVariantDto>> UpdateVariant(Guid id, Guid variantId, UpdateProductVariantDto dto)
{
    var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
    if (variant is null) return NotFound();

    if (dto.Sku is not null) variant.Sku = dto.Sku;
    if (dto.Price.HasValue) variant.Price = dto.Price;
    if (dto.IsActive.HasValue) variant.IsActive = dto.IsActive.Value;
    // Stock is NOT updated here — it comes from inventory movements

    await _db.SaveChangesAsync();
    return Ok(MapToVariantDto(variant));
}
```

- [ ] **3.5** Since the frontend `ProductVariantService.update()` calls `PUT /api/variants/{variantId}` (without product ID), either:
  - (a) Add a separate `VariantsController` with a flat route, OR
  - (b) Fix the frontend service URL to include productId

Choose **(b)** — update `ProductVariantService.update()` to accept `productId`:

```typescript
// In product-variant.service.ts, change:
async update(variantId: string, dto: UpdateVariantDto): Promise<ProductVariant | undefined> {
  // Find productId from cache
  const cached = this.variantsSignal().find(v => v.id === variantId);
  const productId = cached?.productId;
  if (!productId) return undefined;

  const updated = await firstValueFrom(
    this.http.put<ProductVariant>(`/api/products/${productId}/variants/${variantId}`, dto),
  );
  // ...rest stays the same
}
```

Similarly update `delete()`:

```typescript
async delete(variantId: string): Promise<boolean> {
  const cached = this.variantsSignal().find(v => v.id === variantId);
  const productId = cached?.productId;
  if (!productId) return false;

  try {
    await firstValueFrom(
      this.http.delete<void>(`/api/products/${productId}/variants/${variantId}`),
    );
    // ...rest stays the same
  }
}
```

- [ ] **3.6** Add `DELETE /api/products/{id}/variants/{variantId}` endpoint to `ProductsController.cs`:

```csharp
[HttpDelete("{id:guid}/variants/{variantId:guid}")]
public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId)
{
    var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
    if (variant is null) return NotFound();

    _db.ProductVariants.Remove(variant);
    await _db.SaveChangesAsync();
    return NoContent();
}
```

- [ ] **3.7** Add a private helper `MapToVariantDto()` in `ProductsController` if it doesn't already exist, mapping the entity to the `ProductVariantDto`.

- [ ] **3.8** Verify backend build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub && dotnet build src/PeruShopHub.API 2>&1 | tail -10
```

- [ ] **3.9** Verify frontend build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **3.10** Commit:

```
feat: SP2-2.3/2.4 - Add variant CRUD endpoints and fix frontend service URLs
```

---

## Task 4: Make Variant Stock Read-Only (Spec 2.4)

**Problem:** The variant table has an editable `<input type="number">` for stock in both desktop table and mobile card views. Per spec, stock is calculated from inventory movements and must be displayed as read-only text, not an editable input.

**Files to modify:**
- `src/PeruShopHub.Web/src/app/pages/products/variant-manager.component.html`
- `src/PeruShopHub.Web/src/app/pages/products/variant-manager.component.ts`

### Steps

- [ ] **4.1** In `variant-manager.component.html`, replace the stock input in the **desktop table** (around line 173-180) with a read-only display:

```html
<!-- Replace: -->
<td>
  <input
    type="number"
    class="inline-input inline-stock"
    min="0"
    [value]="variant.stock"
    (change)="updateVariantStock(variant.id, +$any($event.target).value)"
  />
</td>

<!-- With: -->
<td>
  <span class="stock-value">{{ variant.stock }}</span>
</td>
```

- [ ] **4.2** In the **mobile card view** (around line 316), the stock is already displayed as read-only text (`Est: {{ variant.stock }}`). No change needed there.

- [ ] **4.3** Remove the `updateVariantStock()` method from `variant-manager.component.ts` since stock is no longer editable from this component:

```typescript
// DELETE this entire method:
async updateVariantStock(variantId: string, stock: number): Promise<void> {
  await this.variantService.update(variantId, { stock });
  this.variantsSignal.update(list => list.map(v => v.id === variantId ? { ...v, stock } : v));
}
```

- [ ] **4.4** Add minimal styling for the read-only stock display in `variant-manager.component.scss` (if needed):

```scss
.stock-value {
  font-variant-numeric: tabular-nums;
  font-family: var(--font-mono);
  color: var(--neutral-700);
}
```

- [ ] **4.5** Verify build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **4.6** Commit:

```
feat: SP2-2.4 - Make variant stock read-only in product form
```

---

## Task 5: Audit Variant Table — Confirm Plain HTML (Spec 2.3)

**Problem:** Spec 2.3 requires checking whether `VariantManagerComponent` uses `DataGridComponent` or plain HTML tables, and migrating if needed. Based on code review, it uses a plain `<table class="vm-table">`. However, the variant table has a unique UX — expandable rows with inline editing, bulk price, attribute columns that vary per product. This is very different from DataGridComponent's column-definition-based approach. The right call is to **keep the custom table** but document the decision.

**Files to modify:**
- None (documentation-only decision)

### Steps

- [ ] **5.1** Confirm the variant table uses plain HTML (`<table class="vm-table">`) — already confirmed by code review.

- [ ] **5.2** Evaluate DataGridComponent compatibility:
  - DataGridComponent uses `GridColumnDef[]` with fixed column definitions
  - Variant table needs: dynamic attribute columns (vary per product/category), expandable detail rows with inline editing, drag-and-drop reordering potential
  - **Decision: Keep custom table.** The variant table's expandable rows, inline editing, and dynamic attribute columns make it a poor fit for DataGridComponent. DataGridComponent is designed for read-only paginated lists.

- [ ] **5.3** Add a code comment at the top of the `vm-table-wrapper` section in the HTML explaining the decision:

```html
<!-- Variants use a custom table (not DataGridComponent) because of expandable detail rows,
     inline editing, and dynamic attribute columns that vary per product category. -->
<div class="vm-table-wrapper">
```

- [ ] **5.4** Verify build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **5.5** Commit:

```
feat: SP2-2.3 - Audit variant table, keep custom implementation with rationale
```

---

## Task 6: Variant Delete Confirmation — Verify Behavior (Spec 2.4)

**Problem:** The spec requires delete confirmation for variants. Code review shows `deleteVariant()` in `variant-manager.component.ts` already calls `this.confirmDialog.confirm()` with a danger dialog and removes from local list on success. This is already correctly implemented.

**Files to verify:**
- `src/PeruShopHub.Web/src/app/pages/products/variant-manager.component.ts` (lines 367-382)

### Steps

- [ ] **6.1** Verify the existing `deleteVariant()` method matches spec requirements:
  - Shows confirmation dialog with "Tem certeza que deseja excluir esta variante?" — **YES** (line 370: `'Tem certeza que deseja excluir esta variante?'`)
  - Calls `DELETE` endpoint — **YES** (line 376: `this.variantService.delete(variantId)`)
  - Removes from local list without re-fetch — **YES** (line 378: `this.variantsSignal.update(...)`)
  - Shows processing state — **YES** (uses `confirmDialog.done()` pattern)

- [ ] **6.2** Confirm the `confirmDialog.done()` call properly handles both success and error (currently in try/catch with `done()` in both branches — correct).

- [ ] **6.3** Update the confirmation message to match spec wording exactly ("Tem certeza que deseja excluir esta **variação**?" instead of "variante"):

```typescript
// In deleteVariant(), change:
message: 'Tem certeza que deseja excluir esta variante?',
// To:
message: 'Tem certeza que deseja excluir esta variação?',
```

- [ ] **6.4** Also update the title to match:

```typescript
title: 'Excluir variação',
```

- [ ] **6.5** Verify build:

```bash
cd /workspaces/Repos/GitHub/PeruShopHub/src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | tail -10
```

- [ ] **6.6** Commit:

```
feat: SP2-2.4 - Update variant delete dialog wording to match spec
```

---

## Summary of All Changes

| Task | Spec | What Changes | Key Files |
|------|------|-------------|-----------|
| 1 | 2.1 | Wire upload button to `POST /api/files/upload` via `FileUploadService` | `media-gallery.component.ts/html`, `product-form.component.ts/html` |
| 2 | 2.2 | Add `ConfirmDialogService.confirm()` before image removal + backend delete | `media-gallery.component.ts` |
| 3 | 2.3/2.4 | Create variant POST/PUT/DELETE backend endpoints, fix frontend service URLs | `ProductsController.cs`, `product-variant.service.ts`, new DTOs |
| 4 | 2.4 | Replace stock `<input>` with read-only `<span>`, remove `updateVariantStock()` | `variant-manager.component.ts/html/scss` |
| 5 | 2.3 | Audit table implementation, document decision to keep custom table | `variant-manager.component.html` (comment only) |
| 6 | 2.4 | Fix variant delete dialog wording to match spec Portuguese | `variant-manager.component.ts` |

### Existing Infrastructure Used
- `FileUploadService` (`src/PeruShopHub.Web/src/app/services/file-upload.service.ts`) — already has `upload()`, `getFiles()`, `delete()`
- `FilesController` (`src/PeruShopHub.API/Controllers/FilesController.cs`) — already has `POST /api/files/upload`, `GET /api/files`, `DELETE /api/files/{id}`
- `ConfirmDialogService` (`src/PeruShopHub.Web/src/app/shared/components/confirm-dialog/confirm-dialog.service.ts`) — `confirm()`, `done()` pattern
- `ProductVariantService` (`src/PeruShopHub.Web/src/app/services/product-variant.service.ts`) — has CRUD methods but URLs need fixing

### New Backend Infrastructure Needed
- `POST /api/products/{id}/variants` — create variant
- `PUT /api/products/{id}/variants/{variantId}` — update variant (price, SKU, isActive)
- `DELETE /api/products/{id}/variants/{variantId}` — delete variant
- `CreateProductVariantDto` and `UpdateProductVariantDto` if they don't exist
