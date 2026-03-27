# Product Categories & Variants Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Each agent MUST use the `ui-ux-pro-max:ui-ux-pro-max` skill for all UI work and the `frontend-design:frontend-design` skill for component design to maintain design system consistency.

**Goal:** Add category management with tree navigation, custom variation fields per category, and product variants with independent stock/pricing to the PeruShopHub Angular frontend.

**Architecture:** Three parallel workstreams — (1) Foundation: shared models, services, and category page shell, (2) Category Features: tree CRUD, drag-drop, variation fields, (3) Product Integration: tree-select dropdown, variant manager tab, product list/detail updates. All data is mocked via Angular services with signals. No backend.

**Tech Stack:** Angular 17+ (standalone components, signals, new control flow), Angular CDK (drag-drop), Lucide icons, CSS custom properties (design tokens from `src/styles/tokens.css`), SCSS with responsive mixins from `src/styles/mixins.scss`.

**PRD:** `tasks/prd-categories-variants.md` (US-034 through US-044)

---

## File Structure

```
src/PeruShopHub.Web/src/app/
├── models/
│   ├── category.model.ts              # Category, VariationField interfaces
│   └── product-variant.model.ts       # ProductVariant interface
├── services/
│   ├── category.service.ts            # CategoryService (mock CRUD + tree ops)
│   └── product-variant.service.ts     # ProductVariantService (mock CRUD + combo gen)
├── pages/
│   └── categorias/
│       ├── categorias.component.ts    # Main page: split panel layout
│       ├── categorias.component.html
│       ├── categorias.component.scss
│       ├── category-tree.component.ts         # Left panel: recursive tree + search
│       ├── category-tree.component.html
│       ├── category-tree.component.scss
│       ├── category-tree-node.component.ts    # Single recursive tree node
│       ├── category-tree-node.component.html
│       ├── category-tree-node.component.scss
│       ├── category-detail.component.ts       # Right panel: edit form + variation fields
│       ├── category-detail.component.html
│       ├── category-detail.component.scss
│       ├── category-form-dialog.component.ts  # Create category modal
│       ├── category-form-dialog.component.html
│       ├── category-form-dialog.component.scss
│       ├── variation-fields.component.ts      # Variation field list + inline add/edit
│       ├── variation-fields.component.html
│       └── variation-fields.component.scss
├── pages/produtos/
│   ├── tree-select.component.ts       # Reusable tree-select dropdown
│   ├── tree-select.component.html
│   ├── tree-select.component.scss
│   ├── variant-manager.component.ts   # Variant tab: field values + combo table
│   ├── variant-manager.component.html
│   └── variant-manager.component.scss
```

**Modified files:**
- `src/app/app.routes.ts` — add `/categorias` route
- `src/app/shared/components/sidebar/sidebar.component.ts` — add "Categorias" nav item
- `src/app/pages/produtos/produto-form.component.ts` — replace flat dropdown with tree-select, add Variações tab
- `src/app/pages/produtos/produto-form.component.html` — update template
- `src/app/pages/produtos/produto-form.component.scss` — add variant tab styles
- `src/app/pages/produtos/produtos-list.component.ts` — add variant count column
- `src/app/pages/produtos/produtos-list.component.html` — add variant count cell
- `src/app/pages/produtos/produtos-list.component.scss` — variant badge styles
- `src/app/pages/produtos/produto-detail.component.ts` — add variant section
- `src/app/pages/produtos/produto-detail.component.html` — add variant table
- `src/app/pages/produtos/produto-detail.component.scss` — variant section styles

---

## Workstream A: Foundation + Category Page (Agent 1)

### Task A1: Data models

**Files:**
- Create: `src/app/models/category.model.ts`
- Create: `src/app/models/product-variant.model.ts`

- [ ] **Step 1: Create category model**

```typescript
// src/app/models/category.model.ts

export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId: string | null;
  children: Category[];
  icon: string | null;
  isActive: boolean;
  productCount: number;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface VariationField {
  id: string;
  categoryId: string;
  name: string;
  type: 'text' | 'select';
  options: string[];   // used when type === 'select'
  required: boolean;
  order: number;
}

export interface InheritedVariationField extends VariationField {
  inheritedFrom: string;       // category name
  inheritedFromId: string;     // category id
}

export type CreateCategoryDto = Pick<Category, 'name' | 'parentId' | 'isActive'>;
export type UpdateCategoryDto = Partial<Pick<Category, 'name' | 'parentId' | 'isActive' | 'order'>>;
export type CreateVariationFieldDto = Pick<VariationField, 'name' | 'type' | 'options' | 'required'>;
export type UpdateVariationFieldDto = Partial<CreateVariationFieldDto & { order: number }>;
```

- [ ] **Step 2: Create product variant model**

```typescript
// src/app/models/product-variant.model.ts

export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  attributes: Record<string, string>;  // field name → value
  price: number | null;                // null = use base price
  stock: number;
  isActive: boolean;
  needsReview: boolean;
}

export type CreateVariantDto = Pick<ProductVariant, 'sku' | 'attributes' | 'price' | 'stock' | 'isActive'>;
export type UpdateVariantDto = Partial<CreateVariantDto & { needsReview: boolean }>;
```

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/models/
git commit -m "feat(categories): add Category, VariationField, and ProductVariant interfaces"
```

### Task A2: Category service with mock data

**Files:**
- Create: `src/app/services/category.service.ts`

- [ ] **Step 1: Create CategoryService**

The service must:
- Store categories as a flat `signal<Category[]>` and expose a computed `categoryTree` that builds the hierarchy
- Pre-seed with these categories (see PRD US-034 for full list):
  - Eletrônicos (> Celulares, Áudio > Fones, Cabos, Caixas de Som)
  - Informática (> Notebooks, Periféricos > Teclados, Mouses)
  - Moda (> Masculina, Feminina > Camisetas, Calças)
  - Casa e Decoração, Esportes, Beleza e Saúde
- Pre-seed variation fields:
  - "Voltagem" (select: 110V/220V/Bivolt) on Eletrônicos
  - "Cor" (select: Preto/Branco/Azul/Vermelho) on Moda
  - "Tamanho" (select: P/M/G/GG) on Camisetas
  - "Comprimento" (text) on Cabos
- Methods: `getAll()`, `getTree()`, `getById(id)`, `create(dto)`, `update(id, dto)`, `delete(id)`, `getVariationFields(categoryId)`, `getInheritedVariationFields(categoryId)` (walks ancestors), `addVariationField(categoryId, dto)`, `updateVariationField(fieldId, dto)`, `deleteVariationField(fieldId, categoryId)` (must also call `ProductVariantService.flagForReview()` for affected products and return count), `reorderCategories(parentId, orderedIds)`, `moveCategory(categoryId, newParentId)`
- Use `signal()` for reactive state, simulate 300ms delay with `setTimeout` wrapped in `Promise`
- `getInheritedVariationFields()` walks the parent chain collecting fields, tagging each with `inheritedFrom` / `inheritedFromId`
- `getBreadcrumb(categoryId)` returns array of ancestor names for display

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/services/category.service.ts
git commit -m "feat(categories): add CategoryService with mock data and tree operations"
```

### Task A3: Product variant service

**Files:**
- Create: `src/app/services/product-variant.service.ts`

- [ ] **Step 1: Create ProductVariantService**

The service must:
- Store variants as `signal<ProductVariant[]>`
- Pre-seed 2-3 products with variants (e.g., a Camiseta with Cor + Tamanho combos)
- Methods: `getByProductId(productId)`, `create(productId, dto)`, `update(variantId, dto)`, `delete(variantId)`, `generateCombinations(fields: {name: string, values: string[]}[])` (returns Cartesian product as `Record<string, string>[]`), `isSkuUnique(sku, excludeId?)`, `flagForReview(categoryId)` (flags all products in that category and descendants), `flagForReviewByProduct(productId)`, `clearReviewFlag(productId)`, `getAffectedProductCount(categoryId)` (returns count of products that would be flagged)
- `generateCombinations`: returns `{ combinations: Record<string, string>[], warning: boolean }` — `warning: true` if > 100 combos (UI should show inline warning banner: "Atenção: mais de 100 combinações serão geradas. Considere reduzir as opções.")

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/services/product-variant.service.ts
git commit -m "feat(categories): add ProductVariantService with combination generator"
```

### Task A4: Route and sidebar registration

**Files:**
- Modify: `src/app/app.routes.ts`
- Modify: `src/app/shared/components/sidebar/sidebar.component.ts`

- [ ] **Step 1: Add route**

In `app.routes.ts`, add inside the layout children array, between `produtos/:id/editar` and `vendas`:

```typescript
{
  path: 'categorias',
  loadComponent: () =>
    import('./pages/categorias/categorias.component').then(m => m.CategoriasComponent),
},
```

- [ ] **Step 2: Add sidebar nav item**

In `sidebar.component.ts`, add the import for `FolderTree` from `lucide-angular` (if not available in the installed version, use `Folders` as fallback), then insert after the "Produtos" nav item:

```typescript
{ label: 'Categorias', route: '/categorias', icon: FolderTree },
```

**Note:** Check `node_modules/lucide-angular` for `FolderTree` availability. If missing, use `Folders` icon instead.

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/app.routes.ts src/PeruShopHub.Web/src/app/shared/components/sidebar/sidebar.component.ts
git commit -m "feat(categories): add /categorias route and sidebar nav item"
```

### Task A5: Categories page shell (split panel)

**Files:**
- Create: `src/app/pages/categorias/categorias.component.ts`
- Create: `src/app/pages/categorias/categorias.component.html`
- Create: `src/app/pages/categorias/categorias.component.scss`

- [ ] **Step 1: Create the main categorias component**

Layout: Split panel — left tree (~300px on desktop), right detail panel (flex: 1). Mobile: show one at a time with back button. The component:
- Injects `CategoryService`
- Has `selectedCategoryId` signal
- Has `mobileView` signal: `'tree' | 'detail'`
- Computes `selectedCategory` from the service
- Page header: "Categorias" h1 with product count summary

Use the design system tokens: `--surface` background for panels, `--neutral-200` border between panels, `--radius-lg` for panel corners, `--space-6` for padding. Follow the same SCSS patterns as existing pages (using `@use '../../../styles/mixins' as m` for responsive breakpoints).

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/
git commit -m "feat(categories): add categories page shell with split panel layout"
```

---

## Workstream B: Category Tree & CRUD (Agent 2)

### Task B0: Bootstrap shared foundation

> Agent 2 works in an isolated worktree. It must create the same models and services that Agent 1 creates. The exact code is defined in Tasks A1-A3 above — copy those implementations verbatim.

**Files:**
- Create: `src/app/models/category.model.ts` (same as Task A1 Step 1)
- Create: `src/app/models/product-variant.model.ts` (same as Task A1 Step 2)
- Create: `src/app/services/category.service.ts` (same as Task A2)
- Create: `src/app/services/product-variant.service.ts` (same as Task A3)

- [ ] **Step 1:** Create all four files with the exact code specified in Tasks A1, A2, A3.
- [ ] **Step 2:** Commit: `git commit -m "feat(categories): bootstrap shared models and services"`

### Task B1: Category tree node (recursive)

**Files:**
- Create: `src/app/pages/categorias/category-tree-node.component.ts`
- Create: `src/app/pages/categorias/category-tree-node.component.html`
- Create: `src/app/pages/categorias/category-tree-node.component.scss`

- [ ] **Step 1: Create the recursive tree node component**

Inputs: `category: Category`, `selectedId: string | null`, `depth: number` (default 0).
Outputs: `select: EventEmitter<string>`, `toggleExpand: EventEmitter<string>`.

Each node renders:
- Indent based on depth (padding-left: `depth * 24px`)
- Expand/collapse chevron (`ChevronRight` / `ChevronDown`) if has children, otherwise spacer (24px)
- Category name
- Product count badge in `--neutral-500` (e.g., "24")
- Inactive indicator: `--neutral-400` opacity 0.5 if `!isActive`
- Selected state: `--primary-light` background, `--primary` left border (3px)
- Hover state: `--neutral-50` background
- Recursively renders `category-tree-node` for each child when expanded

State: `expanded` signal (default: false for depth > 0, true for depth 0).

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/category-tree-node.*
git commit -m "feat(categories): add recursive category tree node component"
```

### Task B2: Category tree (container with search)

**Files:**
- Create: `src/app/pages/categorias/category-tree.component.ts`
- Create: `src/app/pages/categorias/category-tree.component.html`
- Create: `src/app/pages/categorias/category-tree.component.scss`

- [ ] **Step 1: Create the tree container component**

This component wraps the tree nodes and adds:
- Search input at top (magnifying glass icon, placeholder "Buscar categoria...")
- `searchQuery` signal, `filteredTree` computed that filters nodes and auto-expands paths with matches
- "Adicionar Categoria" button (`FolderPlus` icon) at bottom
- Outputs: `selectCategory`, `addCategory`
- Empty state when no categories exist: use `app-empty-state` with title "Nenhuma categoria" and CTA "Criar primeira categoria"
- Scroll container with `overflow-y: auto`, `flex: 1`

Styling: `--surface` background, right border `1px solid var(--neutral-200)`, width 300px on desktop, 100% on mobile.

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/category-tree.*
git commit -m "feat(categories): add category tree container with search filter"
```

### Task B3: Category create dialog

**Files:**
- Create: `src/app/pages/categorias/category-form-dialog.component.ts`
- Create: `src/app/pages/categorias/category-form-dialog.component.html`
- Create: `src/app/pages/categorias/category-form-dialog.component.scss`

- [ ] **Step 1: Create the dialog component**

Modal overlay (480px width, centered). Angular Reactive Form with:
- Nome: required, max 100 chars
- Categoria Pai: tree-select showing hierarchy with indentation (reuse indent logic from tree-node). Excludes the category being edited (and descendants) to prevent circular refs. Shows "— Nenhuma (raiz)" as first option.
- Ativo: toggle switch (default true)

Footer: "Cancelar" ghost button + "Salvar" accent button.
Close on Escape key and backdrop click.
On submit: calls `CategoryService.create()` / `update()`, emits `saved` event.

Follow the existing modal pattern: `--overlay` backdrop, `--shadow-lg` box shadow, `--modal-animation-duration` transition.

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/category-form-dialog.*
git commit -m "feat(categories): add category create/edit dialog"
```

### Task B4: Category detail panel (edit + delete)

**Files:**
- Create: `src/app/pages/categorias/category-detail.component.ts`
- Create: `src/app/pages/categorias/category-detail.component.html`
- Create: `src/app/pages/categorias/category-detail.component.scss`

- [ ] **Step 1: Create the detail component**

Right panel showing selected category. Sections:
1. **Header**: Category name (h2) + breadcrumb path + Edit/Delete buttons
2. **Info form**: Editable inline form (same fields as dialog but inline — name input, parent display as breadcrumb, active toggle)
3. **Read-only fields**: ID (mono), Criado em, Atualizado em (formatted dates)
4. **Placeholder** for variation fields section (filled by Task B6)

Delete button: danger variant. If category has children → disabled with tooltip "Remova as subcategorias primeiro". Otherwise → confirmation dialog with count of affected products.

Empty state when nothing selected: centered message "Selecione uma categoria na árvore para ver os detalhes".

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/category-detail.*
git commit -m "feat(categories): add category detail panel with edit and delete"
```

### Task B5: Drag-and-drop reordering

**Files:**
- Modify: `src/app/pages/categorias/category-tree-node.component.ts`
- Modify: `src/app/pages/categorias/category-tree-node.component.html`
- Modify: `src/app/pages/categorias/category-tree-node.component.scss`
- Modify: `src/app/pages/categorias/category-tree.component.ts`

- [ ] **Step 1: Add Angular CDK DragDrop**

Install if needed: `@angular/cdk` (check `package.json` first).

Add to tree-node:
- `cdkDrag` directive on each node
- `GripVertical` icon as drag handle (visible on hover only)
- Drop indicators: line between items for reorder, highlight on category for reparenting
- `cdkDragDisabled` when dropping would create circular reference
- Animation: 200ms ease transition, `--shadow-md` on dragged item

Add to tree container:
- `cdkDropList` wrapping nodes
- `onDrop` handler that calls `CategoryService.reorderCategories()` or `moveCategory()`

Mobile: long-press (500ms) to initiate drag via `cdkDragDelay`.

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/
git commit -m "feat(categories): add drag-and-drop reordering to category tree"
```

### Task B6: Variation field management

**Files:**
- Create: `src/app/pages/categorias/variation-fields.component.ts`
- Create: `src/app/pages/categorias/variation-fields.component.html`
- Create: `src/app/pages/categorias/variation-fields.component.scss`

- [ ] **Step 1: Create variation fields component**

Input: `categoryId: string`. The component:
- Loads inherited fields via `CategoryService.getInheritedVariationFields()` — displayed first, read-only, with "Herdado de [name]" label in `--neutral-700`
- Loads own fields via `CategoryService.getVariationFields()` — fully editable
- Section header: "Campos de Variação" with "Adicionar Campo" button

Each field card:
- Field name, type badge ("Texto" / "Opções"), option chips (if select), required indicator
- Drag handle for reorder (`GripVertical`)
- Click to expand into edit mode
- Delete icon button → confirmation shows "X produtos serão marcados para revisão" (via `ProductVariantService.getAffectedProductCount(categoryId)`) → on confirm, deletes field + calls `ProductVariantService.flagForReview(categoryId)` → undo toast (5s) that can restore the field and clear the review flags
- When editing a field's type or options: same pattern — show affected count, flag products on save

Add field inline form (expandable card):
- Nome (required input)
- Tipo (radio: "Texto livre" / "Opções predefinidas")
- Opções (chip input — Enter to add, X to remove; only shown when type is "select"; min 2 options validated)
- Obrigatório (toggle)
- "Salvar" / "Cancelar" buttons

Chip input component (inline, no separate file):
- Input + Enter → adds chip
- Duplicate detection
- Each chip: text + X close button
- Chips wrap on multiple lines

- [ ] **Step 2: Wire into category-detail component**

Add `<app-variation-fields [categoryId]="selectedCategory().id">` below the info form section in `category-detail.component.html`.

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/variation-fields.* src/PeruShopHub.Web/src/app/pages/categorias/category-detail.*
git commit -m "feat(categories): add variation field management with inherited fields"
```

### Task B7: Wire up all category page components

**Files:**
- Modify: `src/app/pages/categorias/categorias.component.ts`
- Modify: `src/app/pages/categorias/categorias.component.html`
- Modify: `src/app/pages/categorias/categorias.component.scss`

- [ ] **Step 1: Wire tree + detail + dialog together**

The main `categorias.component`:
- Renders `<app-category-tree>` in left panel, `<app-category-detail>` in right panel
- Handles `selectCategory` → updates `selectedCategoryId`, switches mobile view to 'detail'
- Handles `addCategory` → opens `<app-category-form-dialog>`
- Handles dialog `saved` → refreshes tree, selects new/updated category
- Mobile back button → switches mobile view to 'tree'
- Toast notifications on CRUD success/error (use existing `ToastService`)

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/categorias/
git commit -m "feat(categories): wire up category page components and interactions"
```

---

## Workstream C: Product Integration (Agent 3)

### Task C0: Bootstrap shared foundation

> Agent 3 works in an isolated worktree. It must create the same models and services that Agent 1 creates. The exact code is defined in Tasks A1-A3 above — copy those implementations verbatim.

**Files:**
- Create: `src/app/models/category.model.ts` (same as Task A1 Step 1)
- Create: `src/app/models/product-variant.model.ts` (same as Task A1 Step 2)
- Create: `src/app/services/category.service.ts` (same as Task A2)
- Create: `src/app/services/product-variant.service.ts` (same as Task A3)

- [ ] **Step 1:** Create all four files with the exact code specified in Tasks A1, A2, A3.
- [ ] **Step 2:** Also register the route and sidebar nav item (same as Task A4) since product form changes depend on the categorias route existing.
- [ ] **Step 3:** Commit: `git commit -m "feat(categories): bootstrap shared models, services, and routes"`

### Task C1: Tree-select dropdown component

**Files:**
- Create: `src/app/pages/produtos/tree-select.component.ts`
- Create: `src/app/pages/produtos/tree-select.component.html`
- Create: `src/app/pages/produtos/tree-select.component.scss`

- [ ] **Step 1: Create the tree-select component**

A reusable form-compatible dropdown. Inputs: `categories: Category[]` (tree), `value: string | null` (selected category ID). Outputs: `valueChange: EventEmitter<string>`.

Features:
- Trigger shows selected category breadcrumb path (e.g., "Moda > Feminina > Camisetas") or placeholder "Selecione uma categoria..."
- Dropdown (max-height 320px, scroll) shows tree with expand/collapse
- Search input at top of dropdown filters and auto-expands
- Keyboard: arrow keys navigate, Enter selects, Escape closes
- Indent per depth level (padding-left: `depth * 20px`)
- Close on outside click (same pattern as existing `categoria-dropdown`)
- Selected item: `--primary-light` background

Styling: Follow the existing `.categoria-dropdown` / `.dropdown-list` patterns from `produto-form.component.scss`, extending with tree indentation.

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/produtos/tree-select.*
git commit -m "feat(products): add tree-select dropdown component for category selection"
```

### Task C2: Replace flat category dropdown in product form

**Files:**
- Modify: `src/app/pages/produtos/produto-form.component.ts`
- Modify: `src/app/pages/produtos/produto-form.component.html`

- [ ] **Step 1: Update product form to use tree-select**

In `produto-form.component.ts`:
- Import `CategoryService`, `TreeSelectComponent`, and `BrlCurrencyPipe`
- Inject `CategoryService`, get `categoryTree` from it
- Remove the `CATEGORIAS` constant and `categoriaDropdownOpen`, `categoriaFilter`, `filteredCategorias`, `selectCategoria`, `onCategoriaInput`, `toggleCategoriaDropdown` — all replaced by tree-select
- Add `productId = signal('')` — populated from `this.route.snapshot.paramMap.get('id')` in constructor
- Add `sku` field to the reactive form: `sku: ['', [Validators.required]]` — and add SKU input field in the "Informações Básicas" tab (after titulo, before descricao). In edit mode, populate from mock data (add `sku: 'FN-BT-001'` to `MOCK_PRODUCT`)
- Add `selectedCategoryId` signal derived from form value
- **IMPORTANT:** The existing form has no `sku` field — you must add it to both the FormGroup and the template

In template, replace the entire `.categoria-dropdown` div with:
```html
<app-tree-select
  [categories]="categoryService.categoryTree()"
  [value]="form.get('categoria')?.value"
  (valueChange)="onCategoryChange($event)"
></app-tree-select>
```

Add `onCategoryChange(categoryId: string)` method that patches form and updates variation fields.

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/produtos/produto-form.*
git commit -m "feat(products): replace flat category dropdown with tree-select"
```

### Task C3: Variant manager component

**Files:**
- Create: `src/app/pages/produtos/variant-manager.component.ts`
- Create: `src/app/pages/produtos/variant-manager.component.html`
- Create: `src/app/pages/produtos/variant-manager.component.scss`

- [ ] **Step 1: Create the variant manager component**

Inputs: `categoryId: string | null`, `productSku: string`, `productId: string`.
Imports: `BrlCurrencyPipe`, `BadgeComponent`, `EmptyStateComponent`, `CommonModule`, `FormsModule`, `LucideAngularModule`.
Injects: `CategoryService`, `ProductVariantService`.

The component:

1. **Field values section** — for each inherited variation field from the category chain:
   - Text fields: chip input to add multiple values
   - Select fields: checkbox group from predefined options
   - Shows field name, type, and "Herdado de [category]" label if inherited

2. **Generate button** — "Gerar Combinações" calls `ProductVariantService.generateCombinations()`. Warning if > 100 combos.

3. **Variant table** — columns: one per variation field + Preço (editable number, placeholder "Preço base" in `--neutral-500`) + Estoque (editable number) + SKU (auto-generated, editable) + Ativo (toggle) + delete button
   - SKU auto-gen: `{productSku}-{VALUE1}-{VALUE2}` uppercased
   - SKU uniqueness: on blur/save, call `ProductVariantService.isSkuUnique(sku, variantId)` — show inline error "SKU já está em uso" if not unique
   - Bulk actions row: "Definir preço para todos" + "Definir estoque para todos"
   - Variant count badge: "12 variantes ativas"
   - Mobile: card list (one card per variant)

4. **Empty state** — "Selecione uma categoria com campos de variação para criar variantes"

5. **Needs review** — rows with `needsReview: true` get `--warning-light` background and strikethrough on orphaned attributes

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/produtos/variant-manager.*
git commit -m "feat(products): add variant manager component with combination generator"
```

### Task C4: Add Variações tab to product form

**Files:**
- Modify: `src/app/pages/produtos/produto-form.component.ts`
- Modify: `src/app/pages/produtos/produto-form.component.html`
- Modify: `src/app/pages/produtos/produto-form.component.scss`

- [ ] **Step 1: Add 4th tab**

In `produto-form.component.ts`:
- Add `'variacoes'` to `TabId` type
- Add `{ id: 'variacoes', label: 'Variações' }` to `TABS`
- Import `VariantManagerComponent`

In template, add after the Envio tab panel:
```html
<div class="accordion-header" (click)="toggleAccordion('variacoes')">
  <span>Variações</span>
  <lucide-icon [img]="isAccordionOpen('variacoes') ? chevronUpIcon : chevronDownIcon" [size]="20"></lucide-icon>
</div>

<div class="tab-panel" [class.active]="activeTab() === 'variacoes'" [class.accordion-open]="isAccordionOpen('variacoes')">
  <app-variant-manager
    [categoryId]="form.get('categoria')?.value"
    [productSku]="form.get('sku')?.value || 'PROD'"
    [productId]="productId()"
  ></app-variant-manager>
</div>
```

- [ ] **Step 2: Category change confirmation**

Add `onCategoryChange(newCategoryId: string)` logic:
- If product has existing variants and new category has different fields → show confirm dialog: "Alterar a categoria irá redefinir os campos de variação. Variantes existentes serão removidas. Deseja continuar?"
- "Cancelar" → revert, "Continuar" → clear variants, update category
- If shared fields exist, preserve matching values

- [ ] **Step 3: Commit**

- [ ] **Step 4: Clear review flag on save**

In the `onSaveDraft()` and `onPublish()` methods, after successful save, call `ProductVariantService.clearReviewFlag(productId)` to clear the needsReview flag on all variants for this product.

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/produtos/produto-form.* src/PeruShopHub.Web/src/app/pages/produtos/variant-manager.*
git commit -m "feat(products): add Variações tab with category change confirmation"
```

### Task C4b: Default SKU for products without variants (FR-8)

**Files:**
- Modify: `src/app/services/product-variant.service.ts`

- [ ] **Step 1: Add default variant logic**

In `ProductVariantService`, add method `ensureDefaultVariant(productId, sku)`:
- If a product has no variants, create a single "default" variant with: `attributes: {}`, `price: null` (uses base price), `stock` = product stock, `sku` = product SKU, `isActive: true`, `needsReview: false`
- This represents the product itself as its single SKU entry
- The variant manager UI should show a note: "Produto sem variações — usando SKU único" when the default variant is active

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/services/product-variant.service.ts
git commit -m "feat(products): add default variant for products without variations (FR-8)"
```

### Task C5: Product list — variant indicator

**Files:**
- Modify: `src/app/pages/produtos/produtos-list.component.ts`
- Modify: `src/app/pages/produtos/produtos-list.component.html`
- Modify: `src/app/pages/produtos/produtos-list.component.scss`

- [ ] **Step 1: Add variant count column**

In `produtos-list.component.ts`:
- Import `ProductVariantService`
- Add `variantCount` to mock products (or compute from service)
- Add `needsReview` boolean to mock products
- Update `MOCK_PRODUCTS` to include variant counts (e.g., product 1 has 6 variants, product 4 has 12)

In template (desktop table), add column between Estoque and Status:
```html
<th class="produtos-table__th" style="text-align: center">Variantes</th>
```
```html
<td class="produtos-table__td" style="text-align: center">
  @if (product.variantCount > 0) {
    <span class="variant-badge" (click)="$event.stopPropagation(); router.navigate(['/produtos', product.id, 'editar'], { queryParams: { tab: 'variacoes' } })">
      {{ product.variantCount }} var.
    </span>
  } @else {
    <span class="produtos-table__td--mono">—</span>
  }
  @if (product.needsReview) {
    <span class="review-badge">Revisão</span>
  }
</td>
```

In mobile card, add variant chip below product name.

In SCSS:
```scss
.variant-badge {
  display: inline-block;
  padding: 2px var(--space-2);
  background: var(--primary-light);
  color: var(--primary);
  border-radius: var(--radius-sm);
  font-size: var(--text-xs);
  font-weight: 500;
  cursor: pointer;
  &:hover { filter: brightness(0.95); }
}

.review-badge {
  display: inline-block;
  padding: 2px var(--space-2);
  background: var(--warning-light);
  color: var(--warning);
  border-radius: var(--radius-sm);
  font-size: var(--text-xs);
  font-weight: 600;
  margin-left: var(--space-1);
}

/* Alternative: use <app-badge label="Revisão" variant="warning"> for consistency with detail page */
```

- [ ] **Step 2: Add "Precisa revisão" filter option**

Add to status filter dropdown: `<option value="Revisão">Precisa revisão</option>`
Update filter logic to filter by `needsReview` when "Revisão" is selected.

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/produtos/produtos-list.*
git commit -m "feat(products): add variant count column and needs-review badge to product list"
```

### Task C6: Product detail — variant display

**Files:**
- Modify: `src/app/pages/produtos/produto-detail.component.ts`
- Modify: `src/app/pages/produtos/produto-detail.component.html`
- Modify: `src/app/pages/produtos/produto-detail.component.scss`

- [ ] **Step 1: Add variant section**

In `produto-detail.component.ts`:
- Import `ProductVariantService`
- Add `variants` signal loaded from service
- Compute `totalVariantStock`, `priceRange`, `hasVariants`

In template, add after the two-column sections (before closing `</div>`):
```html
@if (hasVariants()) {
  <div class="detail-page__section detail-page__section--full">
    <div class="detail-page__section-header">
      <h2 class="detail-page__section-title">Variações</h2>
      <div class="detail-page__section-meta">
        <span>{{ variants().length }} variantes</span>
        <span class="mono">Estoque total: {{ totalVariantStock() }}</span>
        @if (priceRange()) {
          <span class="mono">{{ priceRange() }}</span>
        }
      </div>
    </div>
    <div class="detail-page__table-wrap">
      <table class="detail-table">
        <thead>
          <tr>
            <!-- Dynamic columns per variation field -->
            @for (field of variantFields(); track field) {
              <th>{{ field }}</th>
            }
            <th class="text-right">Preço</th>
            <th class="text-right">Estoque</th>
            <th>SKU</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          @for (variant of variants(); track variant.id) {
            <tr [class.variant-row--warning]="variant.stock > 0 && variant.stock <= 5"
                [class.variant-row--danger]="variant.stock === 0"
                [class.variant-row--review]="variant.needsReview">
              @for (field of variantFields(); track field) {
                <td>{{ variant.attributes[field] }}</td>
              }
              <td class="text-right mono">
                @if (variant.price !== null) {
                  {{ variant.price | brlCurrency }}
                } @else {
                  <span class="text-muted">Preço base</span>
                }
              </td>
              <td class="text-right">{{ variant.stock }}</td>
              <td class="mono">{{ variant.sku }}</td>
              <td>
                <app-badge [label]="variant.isActive ? 'Ativo' : 'Inativo'" [variant]="variant.isActive ? 'success' : 'neutral'"></app-badge>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  </div>
}
```

SCSS additions:
```scss
.detail-page__section--full {
  grid-column: 1 / -1;
}
.detail-page__section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
  flex-wrap: wrap;
  gap: var(--space-3);
}
.detail-page__section-meta {
  display: flex;
  gap: var(--space-4);
  font-size: var(--text-sm);
  color: var(--neutral-600);
}
.variant-row--warning { background: var(--warning-light); }
.variant-row--danger { background: var(--danger-light); }
.variant-row--review { background: var(--warning-light); border-left: 3px solid var(--warning); }
.text-muted { color: var(--neutral-500); }
```

- [ ] **Step 2: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/produtos/produto-detail.*
git commit -m "feat(products): add variant display section to product detail page"
```

---

## Integration (After all agents complete)

### Task I1: Merge worktrees and resolve conflicts

- [ ] **Step 1:** Merge Agent 1's branch (foundation)
- [ ] **Step 2:** Merge Agent 2's branch (category features) — resolve model/service conflicts by keeping the most complete version
- [ ] **Step 3:** Merge Agent 3's branch (product integration) — resolve model/service conflicts similarly
- [ ] **Step 4:** Verify all imports resolve correctly
- [ ] **Step 5:** Run `ng build` to confirm no compilation errors
- [ ] **Step 6:** Commit merged result

### Task I2: Final verification

- [ ] **Step 1:** Verify `/categorias` route loads
- [ ] **Step 2:** Verify sidebar shows "Categorias" between "Produtos" and "Vendas"
- [ ] **Step 3:** Verify category tree renders with pre-seeded data
- [ ] **Step 4:** Verify product form shows tree-select and Variações tab
- [ ] **Step 5:** Verify product list shows variant count column
- [ ] **Step 6:** Verify product detail shows variant section
- [ ] **Step 7:** Verify dark theme works on all new components
- [ ] **Step 8:** Verify mobile responsive behavior
