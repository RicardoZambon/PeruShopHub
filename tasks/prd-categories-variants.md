# PRD: Product Categories & Variants System (Frontend)

## Introduction

PeruShopHub currently has a hardcoded list of 12 product categories embedded directly in the product form component. There is no way to manage categories, create hierarchies, or define variation fields (like size, color, configuration) per category. This PRD defines a **Categories Management** screen with tree navigation, CRUD operations, custom variation field configuration per category, and the integration of category-driven variant fields into the product form — enabling products to have variants with independent stock and pricing.

**Scope:** Frontend only (Angular). All data is mocked via interfaces and service stubs, ready for future backend integration.

---

## Goals

- Replace the hardcoded category list with a full category management system
- Enable unlimited-depth category hierarchies with tree navigation
- Allow users to define custom variation fields (text or predefined options) per category
- Products inherit variation fields from their assigned category and all ancestor categories
- Each product variant combination can have its own stock quantity and price
- Pre-seed a basic set of categories so users have a starting point
- Maintain full design system compliance (light/dark theme, responsive, pt-BR)

---

## User Stories

### US-034: Category data models and mock service

**Description:** As a developer, I need TypeScript interfaces and a mock service for categories, variation fields, and product variants so that all UI components have data to work with.

**Acceptance Criteria:**
- [ ] `Category` interface: `id`, `name`, `slug`, `parentId` (nullable), `children` (recursive), `icon` (optional), `isActive`, `productCount` (number — products directly in this category), `createdAt`, `updatedAt`
- [ ] `VariationField` interface: `id`, `categoryId`, `name`, `type` (`'text' | 'select'`), `options` (string array, used when type is `'select'`), `required` (boolean), `order` (number)
- [ ] `ProductVariant` interface: `id`, `productId`, `sku` (globally unique — validated across all products), `attributes` (key-value map of field name to value), `price` (nullable — falls back to product base price), `stock` (number), `isActive`, `needsReview` (boolean — flagged when category fields change)
- [ ] `CategoryService` with methods: `getAll()`, `getTree()`, `getById(id)`, `create(data)`, `update(id, data)`, `delete(id)`, `getVariationFields(categoryId)`, `getInheritedVariationFields(categoryId)` (walks up ancestor chain)
- [ ] `ProductVariantService` with methods: `getByProductId(productId)`, `create(data)`, `update(id, data)`, `delete(id)`, `generateCombinations(fields, values)`
- [ ] Pre-seeded mock data with at least 3 levels of depth and variation fields at different levels
- [ ] Pre-seeded categories include: Eletrônicos (> Celulares, Áudio > Fones, Cabos, Caixas de Som), Informática (> Notebooks, Periféricos > Teclados, Mouses), Moda (> Masculina, Feminina > Camisetas, Calças), Casa e Decoração, Esportes, Beleza e Saúde
- [ ] Variation field examples: "Cor" (select) on Moda, "Tamanho" (select: P/M/G/GG) on Camisetas, "Voltagem" (select: 110V/220V/Bivolt) on Eletrônicos, "Comprimento" (text) on Cabos

### US-035: Categories page — tree navigation and list view

**Description:** As a user, I want to see all my categories organized in an expandable tree so that I can navigate the full hierarchy at a glance.

**Acceptance Criteria:**
- [ ] New route `/categorias` with lazy-loaded component
- [ ] Sidebar navigation updated: add "Categorias" item with `FolderTree` (lucide) icon, positioned between "Produtos" and "Vendas"
- [ ] Page layout: left panel (tree navigation, ~300px on desktop) + right panel (selected category detail/edit)
- [ ] Tree displays categories with expand/collapse toggles (chevron icons)
- [ ] Tree nodes show: category name, product count badge (e.g., "24 produtos"), child count if has children, active/inactive indicator
- [ ] Root-level "Adicionar Categoria" button above the tree
- [ ] Clicking a tree node selects it (highlighted with `--primary-light` background) and loads its details in the right panel
- [ ] Search/filter input at the top of the tree panel — filters nodes and auto-expands matching paths
- [ ] Empty state when no categories exist, with CTA to create the first one
- [ ] Mobile (<768px): tree and detail as separate views with back navigation (no split panel)
- [ ] Tablet (768-1023px): tree as collapsible drawer overlay, detail takes full width
- [ ] Dark theme support via CSS custom properties
- [ ] Verify in browser using dev-browser skill

### US-036: Category CRUD — create, edit, delete

**Description:** As a user, I want to create, edit, and delete categories so that I can organize my product catalog.

**Acceptance Criteria:**
- [ ] **Create:** Modal dialog (480px) with fields: Nome (required, max 100 chars), Categoria Pai (optional, tree-select dropdown showing hierarchy with indentation), Ativo (toggle, default true)
- [ ] **Edit:** Right panel shows editable form when a category is selected — same fields as create, plus read-only fields: ID, Criado em, Atualizado em
- [ ] **Delete:** Confirmation dialog warns if category has children or assigned products. Message: "Esta categoria possui X subcategorias e Y produtos. Deseja realmente excluir?" Options: "Cancelar" / "Excluir" (danger button)
- [ ] Delete is blocked (disabled button + tooltip) if category has children — user must delete or move children first
- [ ] Parent category selector shows tree with indentation (e.g., "— — Camisetas" for depth 2), excludes self and descendants to prevent circular references
- [ ] Form validation: name required, name unique among siblings (same parent)
- [ ] Success/error toast notifications on all operations
- [ ] After create: new category appears in tree, auto-selected
- [ ] After delete: tree updates, parent (or root) auto-selected
- [ ] Verify in browser using dev-browser skill

### US-037: Category drag-and-drop reordering

**Description:** As a user, I want to drag categories in the tree to reorder them or move them under a different parent so that I can reorganize my catalog structure.

**Acceptance Criteria:**
- [ ] Tree nodes are draggable (Angular CDK DragDrop)
- [ ] Visual drop indicators: line between items (reorder) or highlight on category (nest under)
- [ ] Drag handle icon visible on hover (grip dots icon)
- [ ] Moving a category under a new parent updates the hierarchy
- [ ] Cannot drop a parent into its own descendants (circular reference prevention)
- [ ] Drop animation: 200ms ease with `--shadow-md` on dragged item
- [ ] Mobile: long-press (500ms) to initiate drag
- [ ] Verify in browser using dev-browser skill

### US-038: Variation field management per category

**Description:** As a user, I want to configure custom variation fields for each category so that products in that category can have the right variant options (like size, color, voltage).

**Acceptance Criteria:**
- [ ] "Campos de Variação" section in the category detail panel (below category info)
- [ ] Section header: "Campos de Variação" with "Adicionar Campo" button
- [ ] Inherited fields (from ancestor categories) shown first with a label "Herdado de [Categoria Pai]" in `--neutral-700` text, non-editable but visible
- [ ] Own fields shown below inherited fields, fully editable
- [ ] Each field card displays: field name, type badge ("Texto" or "Opções"), option chips (if select type), required indicator, drag handle for reordering
- [ ] **Add field** inline form (expandable card): Nome (required), Tipo (radio: "Texto livre" / "Opções predefinidas"), Opções (chip input — type and press Enter to add, click X to remove; shown only when type is "Opções predefinidas"), Obrigatório (toggle)
- [ ] **Edit field:** Click on field card expands it into edit mode (same as add form, pre-filled)
- [ ] **Delete field:** Icon button with confirmation tooltip "Remover campo?" — removes immediately with undo toast (5s)
- [ ] Fields are reorderable via drag-and-drop within the list
- [ ] Chip input for options: validates no duplicates, min 2 options required for select type
- [ ] Verify in browser using dev-browser skill

### US-039: Product form — category selection with tree dropdown

**Description:** As a user, when creating/editing a product, I want to select a category from a hierarchical dropdown so that I pick the most specific category for my product.

**Acceptance Criteria:**
- [ ] Replace current flat category dropdown in `produto-form.component` with a tree-select dropdown
- [ ] Dropdown shows categories as indented tree (expand/collapse within dropdown)
- [ ] Search/filter input at top of dropdown
- [ ] Selected category shows full breadcrumb path (e.g., "Moda > Feminina > Camisetas")
- [ ] Only leaf categories are selectable (or any category — configurable, default: any)
- [ ] Dropdown max-height: 320px with scroll
- [ ] Keyboard navigation: arrow keys to move, Enter to select, Escape to close
- [ ] Verify in browser using dev-browser skill

### US-040: Product form — variant management section

**Description:** As a user, I want to define product variants based on category variation fields so that each variant can have its own price and stock.

**Acceptance Criteria:**
- [ ] New "Variações" tab in the product form (4th tab, after "Envio")
- [ ] Tab shows inherited variation fields from the selected category chain
- [ ] If no category selected or category has no variation fields: empty state "Selecione uma categoria com campos de variação para criar variantes"
- [ ] For each variation field, show a value input section:
  - Text fields: chip input to add multiple values (e.g., "1m", "2m", "3m" for Comprimento)
  - Select fields: multi-select from predefined options (e.g., check "P", "M", "G" from Tamanho)
- [ ] "Gerar Combinações" button: creates all combinations of selected values as a variant table
- [ ] Variant table columns: one column per variation field + Preço (editable, placeholder: "Preço base") + Estoque (editable number input) + SKU (auto-generated but editable) + Ativo (toggle) + Ações (delete)
- [ ] SKU auto-generation: `{productSKU}-{value1}-{value2}` (e.g., "CAB-001-1M-PRETO")
- [ ] Bulk actions row above table: "Definir preço para todos" (applies a price to all variants), "Definir estoque para todos" (applies stock to all)
- [ ] Adding/removing values from variation fields shows warning if existing combinations will be affected
- [ ] Variant count summary: "12 variantes ativas" badge
- [ ] Mobile: variant table transforms to card list (one card per variant, stacked fields)
- [ ] Verify in browser using dev-browser skill

### US-041: Product form — category change handling

**Description:** As a user, when I change a product's category, the system should handle the transition of variation fields gracefully.

**Acceptance Criteria:**
- [ ] When category changes and new category has different variation fields: show confirmation dialog "Alterar a categoria irá redefinir os campos de variação. Variantes existentes serão removidas. Deseja continuar?"
- [ ] Options: "Cancelar" (revert category) / "Continuar" (clear variants, load new fields)
- [ ] If new category shares some fields with old category, preserve matching values
- [ ] Variation tab updates immediately on category change (if confirmed)
- [ ] Verify in browser using dev-browser skill

### US-042: Product list — variant indicator and count

**Description:** As a user, I want to see at a glance which products have variants in the product list.

**Acceptance Criteria:**
- [ ] New column "Variantes" in the product list table (between Estoque and Status)
- [ ] Shows variant count badge (e.g., "12 var.") or "—" if no variants
- [ ] Badge uses `--primary-light` background with `--primary` text
- [ ] Clicking the badge navigates to the product edit form, Variações tab
- [ ] Mobile card view: variant count shown as a chip below the product name
- [ ] Verify in browser using dev-browser skill

### US-043: Product detail — variant display

**Description:** As a user, when viewing a product detail page, I want to see all its variants with their stock and pricing.

**Acceptance Criteria:**
- [ ] New "Variações" card section in the product detail page
- [ ] Table showing all variants: variation field values, price (or "Preço base" if null), stock, SKU, status
- [ ] Total stock is sum of all variant stocks (shown in header)
- [ ] Price range shown in header if variants have different prices (e.g., "R$ 29,90 — R$ 49,90")
- [ ] Low-stock variants highlighted with `--warning-light` background
- [ ] Out-of-stock variants highlighted with `--danger-light` background
- [ ] Section hidden if product has no variants
- [ ] Verify in browser using dev-browser skill

### US-044: Variant "needs review" flag when category fields change

**Description:** As a user, when I modify or remove variation fields from a category, I want affected products to be flagged so I can review and fix their variants.

**Acceptance Criteria:**
- [ ] When a variation field is removed or its type/options change, all products in that category (and descendant categories) with variants using that field get `needsReview: true`
- [ ] Product list: "Precisa revisão" badge (orange/warning) shown next to variant count for flagged products
- [ ] Product detail: warning banner at top of Variações section: "Os campos de variação da categoria foram alterados. Revise as variantes deste produto."
- [ ] Product form (Variações tab): affected variant rows highlighted with `--warning-light` background, orphaned attribute values shown with strikethrough
- [ ] Filter in product list: "Precisa revisão" option in status filter dropdown
- [ ] After user saves the product form (even without changes to variants), the `needsReview` flag is cleared
- [ ] Category detail panel: when deleting/editing a variation field, show count of affected products: "X produtos serão marcados para revisão"
- [ ] Verify in browser using dev-browser skill

---

## Functional Requirements

- **FR-1:** The system must support unlimited-depth category hierarchies (parent-child relationships)
- **FR-2:** Each category can have zero or more custom variation fields of type `text` or `select`
- **FR-3:** Select-type variation fields must have a list of predefined option values (minimum 2)
- **FR-4:** Products inherit variation fields from their assigned category and ALL ancestor categories (walking up the tree)
- **FR-5:** Each product variant stores: attribute values (key-value map), optional override price, stock quantity, SKU, and active status
- **FR-6:** If a variant has no price override, it uses the product's base price
- **FR-7:** Variant SKUs are globally unique across all products; auto-generated from `{productSKU}-{value1}-{value2}` but manually editable; validated for uniqueness on save
- **FR-8:** Products without variants still have a default SKU entry (the product-level SKU acts as the single "variant")
- **FR-9:** Category deletion is blocked if the category has children (must delete/move children first)
- **FR-10:** Changing a product's category clears existing variants if variation fields differ (with user confirmation)
- **FR-11:** Shared variation fields between old and new categories preserve their values during category change
- **FR-12:** Category tree supports drag-and-drop for reordering and reparenting
- **FR-13:** All variation field and category CRUD operations show success/error toast notifications
- **FR-14:** The categories page tree supports search/filter with auto-expansion of matching paths
- **FR-15:** Pre-seeded categories provide a useful starting structure that users can modify freely
- **FR-16:** Each category tree node displays the count of products directly assigned to it
- **FR-17:** Variation fields are always per-category (no global fields); common fields must be added to each category or a shared ancestor
- **FR-18:** When a category's variation fields are modified or removed, all products with variants using those fields are flagged `needsReview`
- **FR-19:** Flagged products show a "Precisa revisão" warning badge in the product list and a banner in the product detail/form
- **FR-20:** The `needsReview` flag is cleared when a user saves the product form (acknowledging the changes)
- **FR-21:** Variants are flat combinations (Cartesian product) — no hierarchical grouping by field

---

## Non-Goals (Out of Scope)

- **Backend API implementation** — all data is mocked in Angular services
- **Marketplace-specific category mapping** (e.g., mapping local categories to Mercado Livre categories)
- **Variant-level images** (each variant having its own photo gallery)
- **Variant-level cost tracking** (cost of goods per variant — uses product-level cost)
- **Automatic variant price calculation** (e.g., +R$5 for size GG)
- **Category import/export** (CSV, bulk operations)
- **Category permissions / visibility** per user role
- **Variant barcode/EAN** generation
- **Inventory alerts per variant** (low-stock notifications are existing feature scope, not this PRD)

---

## Design Considerations

### Layout — Categories Page
```
┌─────────────────────────────────────────────────────────┐
│  Header (56px)                                          │
├──────────┬──────────────────────────────────────────────┤
│ Sidebar  │  ┌─────────────┬───────────────────────────┐ │
│ (256/64) │  │ Tree Panel  │  Detail Panel              │ │
│          │  │ (~300px)    │                             │ │
│          │  │             │  [Category Info Form]       │ │
│          │  │ 🔍 Buscar   │                             │ │
│          │  │             │  ─────────────────────────  │ │
│          │  │ ▶ Eletrôn.45│                             │ │
│          │  │   ▶ Áudio 12│  [Campos de Variação]      │ │
│          │  │     Fones  8│                             │ │
│          │  │     Cabos  3│  Herdado de "Eletrônicos": │ │
│          │  │   ▶ Celul.18│  ☑ Voltagem (Opções)       │ │
│          │  │ ▶ Moda    32│                             │ │
│          │  │ ▶ Casa    15│  Próprios:                  │ │
│          │  │             │  ☑ Comprimento (Texto)      │ │
│          │  │ [+Adicionar]│  [+ Adicionar Campo]        │ │
│          │  └─────────────┴───────────────────────────┘ │
├──────────┴──────────────────────────────────────────────┤
```

### Layout — Product Form Variants Tab
```
┌─────────────────────────────────────────────────────┐
│  Básico │ Preço │ Envio │ Variações                 │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Campos de variação (de Moda > Feminina > Camisetas)│
│                                                     │
│  Cor: [Preto ✕] [Branco ✕] [Azul ✕] [+ Adicionar]  │
│  Tamanho: ☑P ☑M ☑G ☐GG                             │
│                                                     │
│  [Gerar Combinações]          12 variantes ativas   │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │ Definir preço p/ todos │ Definir estoque │    │   │
│  ├────────┬─────────┬────────┬─────────┬────────┤   │
│  │ Cor    │ Tamanho │ Preço  │ Estoque │ SKU    │   │
│  ├────────┼─────────┼────────┼─────────┼────────┤   │
│  │ Preto  │ P       │ R$49,90│ 15      │ CAM-1P │   │
│  │ Preto  │ M       │ R$49,90│ 23      │ CAM-1M │   │
│  │ Preto  │ G       │ R$54,90│ 8       │ CAM-1G │   │
│  │ Branco │ P       │ —      │ 12      │ CAM-2P │   │
│  │ ...    │ ...     │ ...    │ ...     │ ...    │   │
│  └────────┴─────────┴────────┴─────────┴────────┘   │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Existing Components to Reuse
- **Data table** (`shared/components/data-table/`) — for variant table
- **Empty state** (`shared/components/empty-state/`) — for no-categories and no-variants states
- **Error state** (`shared/components/error-state/`) — for service errors
- **Toast notifications** — existing pattern in the app
- **Form patterns** — reactive forms, validation, tab/accordion layout from `produto-form.component`
- **Sidebar nav items** — extend existing `navItems` array in `sidebar.component.ts`

### Icons (Lucide)
- Categories page: `FolderTree`
- Add category: `FolderPlus`
- Expand/collapse: `ChevronRight` / `ChevronDown`
- Variation field (text): `Type`
- Variation field (select): `ListChecks`
- Drag handle: `GripVertical`
- Inherited field indicator: `Link`

---

## Technical Considerations

- **Angular CDK** — use `@angular/cdk/drag-drop` for tree reordering and field reordering
- **Recursive component** — the category tree requires a recursive Angular component (`category-tree-node` that renders itself for children)
- **Mock data strategy** — services use `signal()` for reactive state, `delay()` via RxJS to simulate API latency (300-500ms)
- **Category path resolution** — `getInheritedVariationFields(categoryId)` must walk up the parent chain and collect fields from each ancestor, ordered from root to leaf
- **Variant combination generation** — Cartesian product of all selected field values; warn if > 100 combinations
- **File structure:**
  ```
  src/app/
  ├── models/
  │   ├── category.model.ts
  │   └── product-variant.model.ts
  ├── services/
  │   ├── category.service.ts
  │   └── product-variant.service.ts
  ├── pages/
  │   └── categorias/
  │       ├── categorias.component.ts/html/scss
  │       ├── category-tree/
  │       │   ├── category-tree.component.ts/html/scss
  │       │   └── category-tree-node.component.ts/html/scss
  │       ├── category-detail/
  │       │   └── category-detail.component.ts/html/scss
  │       ├── category-form-dialog/
  │       │   └── category-form-dialog.component.ts/html/scss
  │       └── variation-fields/
  │           └── variation-fields.component.ts/html/scss
  └── pages/produtos/
      └── variant-manager/
          └── variant-manager.component.ts/html/scss
  ```
- **Lazy loading** — categories module loaded via `loadComponent` in routes (consistent with existing pattern)
- **State management** — Angular signals for local component state, services with signals for shared state (consistent with existing codebase pattern)

---

## Success Metrics

- User can create a 3-level category hierarchy in under 2 minutes
- User can add a variation field to a category in under 30 seconds
- Product form loads correct variation fields within 500ms of category selection
- Variant table correctly generates all flat combinations (Cartesian product)
- All UI works on mobile (tree as separate view, variant table as card list)
- Zero hardcoded category references remain in the product form
- SKU uniqueness is validated globally — no duplicate SKUs across any products/variants
- Category tree displays accurate product counts per node
- Products with stale variants are clearly flagged and filterable in the product list

---

## Resolved Questions

1. **Global variation fields?** — No. Variation fields are always scoped to a specific category. If "Cor" is needed in multiple categories, it must be added to each one (or to a common ancestor).
2. **Variant SKU uniqueness?** — Globally unique. Each variant SKU is treated as a distinct product identifier. Products without variants still have a default SKU entry.
3. **Category field changes affecting existing products?** — Yes. Products with variants tied to modified/removed fields are flagged as "Precisa revisão" (needs review). A visual indicator appears in the product list and detail pages.
4. **Product counts in category tree?** — Yes. Each tree node shows the count of products assigned to that category.
5. **Variant groups?** — No. Each combination is a flat row (e.g., "Cor: Azul / Tamanho: M" is one variant, "Cor: Azul / Tamanho: G" is another). No hierarchical grouping by field.
