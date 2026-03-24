# Shared UI Components — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract all repeated UI patterns into shared components and replace every hardcoded instance across all pages, ensuring visual consistency and proper loading states.

**Architecture:** Create standalone Angular components in `src/app/shared/components/` with inputs/outputs matching the most common usage patterns found across pages. Each component uses CSS custom properties from the existing design system (`styles/tokens.css`). Pages import and use shared components, removing duplicated SCSS. Every page with backend data gets proper skeleton loading states.

**Tech Stack:** Angular 17+ (standalone components, signals), Lucide Angular (icons), CSS custom properties, SCSS with BEM naming

---

## File Structure

### New Shared Components (create)

| Component | Path | Purpose |
|-----------|------|---------|
| `PageHeaderComponent` | `shared/components/page-header/` | Page title + subtitle + primary action button |
| `SearchInputComponent` | `shared/components/search-input/` | Icon-prefixed search box with debounce |
| `SelectDropdownComponent` | `shared/components/select-dropdown/` | Styled native `<select>` wrapper |
| `ButtonComponent` | `shared/components/button/` | All button variants: accent, ghost, outline, danger, icon-only |
| `FormFieldComponent` | `shared/components/form-field/` | Label + input + hint + error wrapper |
| `TextInputComponent` | `shared/components/text-input/` | Styled text input with prefix/suffix support |
| `ToggleSwitchComponent` | `shared/components/toggle-switch/` | Checkbox toggle with label |
| `RadioGroupComponent` | `shared/components/radio-group/` | Radio button group with custom styling |
| `TabBarComponent` | `shared/components/tab-bar/` | Horizontal tab navigation with active indicator |
| `DialogComponent` | `shared/components/dialog/` | Modal overlay + container + header/body/footer |
| `FormActionsComponent` | `shared/components/form-actions/` | Cancel + Save button pair for form footers |
| `PageSkeletonComponent` | `shared/components/page-skeleton/` | Configurable skeleton loading for list/detail pages |
| `MarginBadgeComponent` | `shared/components/margin-badge/` | Color-coded margin percentage display |

### Existing Shared Components (already exist, keep as-is)

- `BadgeComponent` — status labels with color variants
- `KpiCardComponent` — metric cards with change indicators
- `EmptyStateComponent` — empty state with CTA
- `ErrorStateComponent` — error state display
- `SkeletonComponent` — individual skeleton blocks
- `DataTableComponent` — data table with sorting/pagination
- `IconPickerComponent` — icon selection grid
- `ToastContainerComponent` — notification toasts
- `HeaderComponent`, `SidebarComponent`, `LayoutComponent` — app shell

### Pages to Update (modify)

Every page under `src/app/pages/` will be modified to import and use shared components:

- `dashboard/dashboard.component.*`
- `categories/categories.component.*`, `category-detail.component.*`, `category-form-dialog.component.*`, `category-tree.component.*`, `variation-fields.component.*`
- `customers/customers.component.*`, `customer-detail.component.*`
- `finance/finance.component.*`
- `inventory/inventory.component.*`
- `products/products-list.component.*`, `product-detail.component.*`, `product-form.component.*`, `variant-manager.component.*`
- `purchase-orders/purchase-orders-list.component.*`, `purchase-order-detail.component.*`, `purchase-order-form.component.*`
- `questions/questions.component.*`
- `sales/sales-list.component.*`, `sale-detail.component.*`
- `settings/settings.component.*`
- `supplies/supplies.component.*`

---

## Shared Component Specifications

### PageHeaderComponent

```typescript
// Inputs
@Input({ required: true }) title: string;
@Input() subtitle?: string;
@Input() actionLabel?: string;      // e.g. "Novo Produto"
@Input() actionIcon?: LucideIconData; // e.g. Plus
// Outputs
@Output() action = new EventEmitter<void>();
```

```html
<!-- Usage -->
<app-page-header
  title="Produtos"
  [actionLabel]="'Novo Produto'"
  [actionIcon]="plusIcon"
  (action)="onNewProduct()">
</app-page-header>
```

Replaces: `.dashboard__title`, `.categorias-page__title + __subtitle + __add-btn`, `.clientes__title`, `.estoque__title`, `.financeiro__title`, `.produtos__title + __new-btn`, `.po-list__title + __new-btn`, `.page-title`, `.vendas__title`, `.suprimentos__title`

---

### SearchInputComponent

```typescript
@Input() placeholder = 'Buscar...';
@Input() value = '';
@Output() valueChange = new EventEmitter<string>();
```

Replaces: `.categorias-page__search`, `.clientes__search`, `.produtos__search`, `.po-list__search`, `.vendas__search-input`, `.estoque__action-bar input`, `.suprimentos__search`

---

### SelectDropdownComponent

```typescript
@Input({ required: true }) options: { value: string; label: string }[];
@Input() value = '';
@Input() placeholder?: string;
@Output() valueChange = new EventEmitter<string>();
```

Replaces: `.produtos__status-filter`, `.po-list__status-filter`, `.mov-filters__select`, `.vendas select`, `.suprimentos select`

---

### ButtonComponent

```typescript
@Input() variant: 'accent' | 'ghost' | 'outline' | 'danger' | 'icon' = 'accent';
@Input() size: 'sm' | 'md' = 'md';
@Input() disabled = false;
@Input() loading = false;
@Input() icon?: LucideIconData;
@Input() iconOnly = false;
@Input() type: 'button' | 'submit' = 'button';
```

```html
<!-- Usage examples -->
<app-button variant="accent" [icon]="plusIcon">Novo Produto</app-button>
<app-button variant="ghost">Cancelar</app-button>
<app-button variant="icon" [icon]="pencilIcon" [iconOnly]="true" ariaLabel="Editar"></app-button>
<app-button variant="icon" [icon]="trashIcon" [iconOnly]="true" variant="danger" ariaLabel="Excluir"></app-button>
```

Replaces: All `.btn`, `.btn-accent`, `.btn-ghost`, `.btn-outline`, `.btn-danger`, `.btn-icon`, `.btn-icon--danger`, `.btn-sm`, `.modal__btn--save`, `.modal__btn--cancel`, `.btn-reply`, `.btn-send`, `.btn-cancel`, field-card action buttons, etc.

---

### FormFieldComponent

```typescript
@Input() label?: string;
@Input() hint?: string;
@Input() error?: string;
@Input() required = false;
```

```html
<!-- Wraps any form control via content projection -->
<app-form-field label="Nome" [error]="nameError()" required>
  <input type="text" formControlName="name" appTextInput />
</app-form-field>
```

Replaces: All `.field`, `.form-field`, `.field-form__field` wrappers with their label/hint/error patterns

---

### TextInputComponent (directive approach)

```typescript
// Directive that applies consistent styling
@Directive({ selector: 'input[appTextInput], textarea[appTextInput]' })
@Input() prefix?: string;  // e.g. "R$"
@Input() suffix?: string;  // e.g. "%"
```

Replaces: All `.form-field__input`, `.form-field__textarea`, `.form-field__prefix-wrap` patterns

---

### ToggleSwitchComponent

```typescript
@Input() label?: string;
@Input() checked = false;
@Output() checkedChange = new EventEmitter<boolean>();
// Or use with formControlName via ControlValueAccessor
```

Replaces: All `.toggle-label`, `.toggle-switch`, `.toggle-slider` patterns across categories, variation-fields, settings, inventory

---

### RadioGroupComponent

```typescript
@Input({ required: true }) options: { value: string; label: string }[];
@Input() value = '';
@Output() valueChange = new EventEmitter<string>();
// Or use with formControlName via ControlValueAccessor
```

Replaces: All `.radio-group`, `.radio-label`, `.radio-custom` patterns in variation-fields

---

### TabBarComponent

```typescript
@Input({ required: true }) tabs: { key: string; label: string; count?: number; disabled?: boolean }[];
@Input() activeTab = '';
@Output() tabChange = new EventEmitter<string>();
```

Replaces: `.financeiro__tabs`, `.estoque__tabs`, `.tab-filters` (questions), `.settings tabs`

---

### DialogComponent

```typescript
@Input({ required: true }) title: string;
@Input() open = false;
@Output() closed = new EventEmitter<void>();
```

```html
<app-dialog title="Nova Categoria" [open]="showDialog()" (closed)="onClose()">
  <ng-container body><!-- form content --></ng-container>
  <ng-container footer>
    <app-form-actions (cancel)="onClose()" (save)="onSave()" [saving]="saving()" />
  </ng-container>
</app-dialog>
```

Replaces: All `.dialog-overlay + .dialog`, `.modal-backdrop + .modal` patterns

---

### FormActionsComponent

```typescript
@Input() saveLabel = 'Salvar';
@Input() cancelLabel = 'Cancelar';
@Input() saving = false;
@Input() disabled = false;
@Output() save = new EventEmitter<void>();
@Output() cancel = new EventEmitter<void>();
```

Replaces: All `.dialog__footer`, `.detail-panel__form-actions`, `.field-form__actions`, `.modal__actions`, `.form-actions`, `.reply-actions` patterns

---

### MarginBadgeComponent

```typescript
@Input() margin: number | null = null;
```

Replaces: All `.margin-badge` with `getMarginClass()` logic across products-list, dashboard, finance

---

### PageSkeletonComponent

```typescript
@Input() type: 'list' | 'detail' | 'kpi-grid' | 'custom' = 'list';
@Input() rows = 6;
@Input() columns = 4;
```

Replaces: All inline skeleton HTML across pages (`.table-skeleton`, `.skeleton-card`, `.po-table__skeleton`, `.po-detail-skeleton`, custom detail skeletons)

---

## Task Breakdown

### Task 1: Create ButtonComponent

**Files:**
- Create: `src/app/shared/components/button/button.component.ts`
- Create: `src/app/shared/components/button/button.component.html`
- Create: `src/app/shared/components/button/button.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create `ButtonComponent` with variants: `accent`, `ghost`, `outline`, `danger`, `icon`. Support `size` (sm/md), `loading` (spinner), `disabled`, `icon` (lucide), `iconOnly`, `type` (button/submit). Use `<ng-content>` for label text. Ghost icon buttons: 32×32, no border, hover bg `neutral-100`.
- [ ] **Step 2:** Add SCSS using design tokens: `--accent`, `--accent-hover`, `--neutral-*`, `--radius-md`, `--radius-sm`. All transitions 150ms ease. Spinner animation for loading state.
- [ ] **Step 3:** Export from `shared/components/index.ts`.
- [ ] **Step 4:** Verify build: `cd src/PeruShopHub.Web && npx ng build`
- [ ] **Step 5:** Commit: `feat: add shared ButtonComponent with all variants`

---

### Task 2: Create PageHeaderComponent

**Files:**
- Create: `src/app/shared/components/page-header/page-header.component.ts`
- Create: `src/app/shared/components/page-header/page-header.component.html`
- Create: `src/app/shared/components/page-header/page-header.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create component with inputs: `title` (required), `subtitle` (optional), `actionLabel` (optional), `actionIcon` (optional lucide icon). Output: `action`. Layout: flex row, title+subtitle left, action button right. Use `<app-button variant="accent">` for the action.
- [ ] **Step 2:** SCSS: h1 with `--heading-1-size`/`--heading-1-weight`, subtitle with `--body-small-size`/`--neutral-500`. Responsive: stack on mobile.
- [ ] **Step 3:** Export from index.
- [ ] **Step 4:** Build check.
- [ ] **Step 5:** Commit: `feat: add shared PageHeaderComponent`

---

### Task 3: Create SearchInputComponent

**Files:**
- Create: `src/app/shared/components/search-input/search-input.component.ts`
- Create: `src/app/shared/components/search-input/search-input.component.html`
- Create: `src/app/shared/components/search-input/search-input.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create component with inputs: `placeholder`, `value`. Output: `valueChange`. Uses lucide `Search` icon positioned absolute left. Input has padding-left for icon space.
- [ ] **Step 2:** SCSS: border `--neutral-300`, focus `--primary` + `--focus-ring`, placeholder `--neutral-500`. Full width.
- [ ] **Step 3:** Export, build, commit: `feat: add shared SearchInputComponent`

---

### Task 4: Create SelectDropdownComponent

**Files:**
- Create: `src/app/shared/components/select-dropdown/select-dropdown.component.ts`
- Create: `src/app/shared/components/select-dropdown/select-dropdown.component.html`
- Create: `src/app/shared/components/select-dropdown/select-dropdown.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create component wrapping native `<select>`. Inputs: `options` (array of `{value, label}`), `value`, `placeholder`. Output: `valueChange`. Implement `ControlValueAccessor` for `formControlName` support.
- [ ] **Step 2:** SCSS: consistent with text inputs — same height, border, focus, radius.
- [ ] **Step 3:** Export, build, commit: `feat: add shared SelectDropdownComponent`

---

### Task 5: Create FormFieldComponent, TextInput directive, ToggleSwitchComponent, RadioGroupComponent

**Files:**
- Create: `src/app/shared/components/form-field/form-field.component.ts`
- Create: `src/app/shared/components/form-field/form-field.component.html`
- Create: `src/app/shared/components/form-field/form-field.component.scss`
- Create: `src/app/shared/directives/text-input.directive.ts`
- Create: `src/app/shared/components/toggle-switch/toggle-switch.component.ts`
- Create: `src/app/shared/components/toggle-switch/toggle-switch.component.html`
- Create: `src/app/shared/components/toggle-switch/toggle-switch.component.scss`
- Create: `src/app/shared/components/radio-group/radio-group.component.ts`
- Create: `src/app/shared/components/radio-group/radio-group.component.html`
- Create: `src/app/shared/components/radio-group/radio-group.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create `FormFieldComponent` — content projection wrapper with label, hint, error slots. Uses `<ng-content>` for the actual form control.
- [ ] **Step 2:** Create `TextInputDirective` — applies consistent styling host classes, handles prefix/suffix wrapper if `prefix`/`suffix` inputs provided.
- [ ] **Step 3:** Create `ToggleSwitchComponent` — implements `ControlValueAccessor`. 44×24px toggle, animated slider, primary color when checked.
- [ ] **Step 4:** Create `RadioGroupComponent` — implements `ControlValueAccessor`. Takes `options` array, renders custom radio circles. Fix: radio dot positioned correctly within circle (not overlapping label).
- [ ] **Step 5:** Export all, build, commit: `feat: add shared form components — FormField, TextInput, ToggleSwitch, RadioGroup`

---

### Task 6: Create TabBarComponent

**Files:**
- Create: `src/app/shared/components/tab-bar/tab-bar.component.ts`
- Create: `src/app/shared/components/tab-bar/tab-bar.component.html`
- Create: `src/app/shared/components/tab-bar/tab-bar.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create component. Inputs: `tabs` (array of `{key, label, count?, disabled?}`), `activeTab`. Output: `tabChange`. Renders horizontal button row with active indicator (bottom border).
- [ ] **Step 2:** SCSS: flex row, `overflow-x: auto`, scrollbar hidden. Active: primary color + bottom border. Disabled: muted color. Count badge: accent pill.
- [ ] **Step 3:** Export, build, commit: `feat: add shared TabBarComponent`

---

### Task 7: Create DialogComponent and FormActionsComponent

**Files:**
- Create: `src/app/shared/components/dialog/dialog.component.ts`
- Create: `src/app/shared/components/dialog/dialog.component.html`
- Create: `src/app/shared/components/dialog/dialog.component.scss`
- Create: `src/app/shared/components/form-actions/form-actions.component.ts`
- Create: `src/app/shared/components/form-actions/form-actions.component.html`
- Create: `src/app/shared/components/form-actions/form-actions.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create `DialogComponent` — overlay backdrop, centered dialog, header with title + close X, `<ng-content select="[body]">`, `<ng-content select="[footer]">`. Closes on Escape and backdrop click. Animations: fadeIn overlay, slideUp dialog.
- [ ] **Step 2:** Create `FormActionsComponent` — flex row with cancel (ghost) + save (accent) buttons. Inputs: `saveLabel`, `cancelLabel`, `saving`, `disabled`. Outputs: `save`, `cancel`. Uses `<app-button>`.
- [ ] **Step 3:** Export, build, commit: `feat: add shared DialogComponent and FormActionsComponent`

---

### Task 8: Create MarginBadgeComponent and PageSkeletonComponent

**Files:**
- Create: `src/app/shared/components/margin-badge/margin-badge.component.ts`
- Create: `src/app/shared/components/margin-badge/margin-badge.component.scss`
- Create: `src/app/shared/components/page-skeleton/page-skeleton.component.ts`
- Create: `src/app/shared/components/page-skeleton/page-skeleton.component.html`
- Create: `src/app/shared/components/page-skeleton/page-skeleton.component.scss`
- Modify: `src/app/shared/components/index.ts`

- [ ] **Step 1:** Create `MarginBadgeComponent` — inline component, input: `margin: number | null`. Displays `XX.X%` with color: green ≥20%, yellow 10-19%, red <10%. Monospace font. Null → `0.0%` red.
- [ ] **Step 2:** Create `PageSkeletonComponent` — configurable skeleton layouts. Input: `type` ('list' | 'detail' | 'kpi-grid'). List: 6 rows of varying-width blocks. Detail: header + info rows + meta section. KPI-grid: 4 card placeholders. Pulse animation.
- [ ] **Step 3:** Export, build, commit: `feat: add shared MarginBadge and PageSkeleton components`

---

### Task 9: Replace shared components in Dashboard

**Files:**
- Modify: `src/app/pages/dashboard/dashboard.component.html`
- Modify: `src/app/pages/dashboard/dashboard.component.ts`
- Modify: `src/app/pages/dashboard/dashboard.component.scss`

- [ ] **Step 1:** Replace title with `<app-page-header title="Dashboard">`.
- [ ] **Step 2:** Replace period selector buttons with `<app-tab-bar>`.
- [ ] **Step 3:** Replace margin badges in top products table with `<app-margin-badge>`.
- [ ] **Step 4:** Replace KPI skeleton loading with `<app-page-skeleton type="kpi-grid">`.
- [ ] **Step 5:** Remove orphaned SCSS for replaced patterns.
- [ ] **Step 6:** Build check, commit: `refactor: use shared components in Dashboard`

---

### Task 10: Replace shared components in Categories

**Files:**
- Modify: `src/app/pages/categories/categories.component.html`
- Modify: `src/app/pages/categories/categories.component.ts`
- Modify: `src/app/pages/categories/categories.component.scss`
- Modify: `src/app/pages/categories/category-detail.component.html`
- Modify: `src/app/pages/categories/category-detail.component.ts`
- Modify: `src/app/pages/categories/category-detail.component.scss`
- Modify: `src/app/pages/categories/category-form-dialog.component.html`
- Modify: `src/app/pages/categories/category-form-dialog.component.ts`
- Modify: `src/app/pages/categories/category-form-dialog.component.scss`
- Modify: `src/app/pages/categories/variation-fields.component.html`
- Modify: `src/app/pages/categories/variation-fields.component.ts`
- Modify: `src/app/pages/categories/variation-fields.component.scss`

- [ ] **Step 1:** categories.component: Replace header with `<app-page-header>`, search with `<app-search-input>`.
- [ ] **Step 2:** category-form-dialog: Replace with `<app-dialog>`, form fields with `<app-form-field>`, toggle with `<app-toggle-switch>`, footer with `<app-form-actions>`.
- [ ] **Step 3:** category-detail: Replace edit/delete icon buttons with `<app-button variant="icon">`, form fields and actions with shared components.
- [ ] **Step 4:** variation-fields: Replace radio groups with `<app-radio-group>`, toggle with `<app-toggle-switch>`, add button with `<app-button variant="icon">`, form actions with `<app-form-actions>`.
- [ ] **Step 5:** Remove orphaned SCSS (button styles, toggle styles, radio styles, field styles duplicated across all 4 component SCSSes).
- [ ] **Step 6:** Build check, commit: `refactor: use shared components in Categories`

---

### Task 11: Replace shared components in Products

**Files:**
- Modify: `src/app/pages/products/products-list.component.*`
- Modify: `src/app/pages/products/product-detail.component.*`
- Modify: `src/app/pages/products/product-form.component.*`
- Modify: `src/app/pages/products/variant-manager.component.*`

- [ ] **Step 1:** products-list: Replace header with `<app-page-header>`, search with `<app-search-input>`, status filter with `<app-select-dropdown>`, margin badges with `<app-margin-badge>`.
- [ ] **Step 2:** product-detail: Replace edit button with `<app-button>`, ensure proper loading skeleton.
- [ ] **Step 3:** product-form: Replace tab navigation with `<app-tab-bar>`, form fields with `<app-form-field>`, bottom action bar buttons with `<app-button>` variants, currency inputs with `<input appTextInput prefix="R$">`.
- [ ] **Step 4:** variant-manager: Replace toggle switches with `<app-toggle-switch>`, buttons with `<app-button>`.
- [ ] **Step 5:** Remove orphaned SCSS, build check, commit: `refactor: use shared components in Products`

---

### Task 12: Replace shared components in Customers, Sales, Finance

**Files:**
- Modify: `src/app/pages/customers/customers.component.*`
- Modify: `src/app/pages/customers/customer-detail.component.*`
- Modify: `src/app/pages/sales/sales-list.component.*`
- Modify: `src/app/pages/sales/sale-detail.component.*`
- Modify: `src/app/pages/finance/finance.component.*`

- [ ] **Step 1:** customers: Replace header with `<app-page-header>`, search with `<app-search-input>`.
- [ ] **Step 2:** sales-list: Replace header with `<app-page-header>`, search with `<app-search-input>`, status filter with `<app-select-dropdown>`, date inputs with consistent styling.
- [ ] **Step 3:** sale-detail: Replace action buttons with `<app-button>`, form fields in cost editor with shared form components, lock banner buttons with `<app-button>`.
- [ ] **Step 4:** finance: Replace tabs with `<app-tab-bar>`, export buttons with `<app-button variant="outline">`, margin displays with `<app-margin-badge>`.
- [ ] **Step 5:** Remove orphaned SCSS, build check, commit: `refactor: use shared components in Customers, Sales, Finance`

---

### Task 13: Replace shared components in Inventory, Purchase Orders, Supplies

**Files:**
- Modify: `src/app/pages/inventory/inventory.component.*`
- Modify: `src/app/pages/purchase-orders/purchase-orders-list.component.*`
- Modify: `src/app/pages/purchase-orders/purchase-order-detail.component.*`
- Modify: `src/app/pages/purchase-orders/purchase-order-form.component.*`
- Modify: `src/app/pages/supplies/supplies.component.*`

- [ ] **Step 1:** inventory: Replace header with `<app-page-header>`, tabs with `<app-tab-bar>`, modal with `<app-dialog>`, form fields with shared components, filter dropdowns with `<app-select-dropdown>`.
- [ ] **Step 2:** purchase-orders-list: Replace header with `<app-page-header>`, search with `<app-search-input>`, status filter with `<app-select-dropdown>`.
- [ ] **Step 3:** purchase-order-detail: Replace action buttons with `<app-button>`, ensure skeleton loading.
- [ ] **Step 4:** purchase-order-form: Replace form fields with shared components, currency inputs with prefix directive, action buttons with `<app-button>`.
- [ ] **Step 5:** supplies: Replace header with `<app-page-header>`, search with `<app-search-input>`, filter with `<app-select-dropdown>`, modal with `<app-dialog>`, form fields with shared components.
- [ ] **Step 6:** Remove orphaned SCSS, build check, commit: `refactor: use shared components in Inventory, Purchase Orders, Supplies`

---

### Task 14: Replace shared components in Questions and Settings

**Files:**
- Modify: `src/app/pages/questions/questions.component.*`
- Modify: `src/app/pages/settings/settings.component.*`

- [ ] **Step 1:** questions: Replace header with `<app-page-header>`, tabs with `<app-tab-bar>`, reply buttons with `<app-button>`, empty state skeleton with `<app-page-skeleton>`.
- [ ] **Step 2:** settings: Replace tab navigation with `<app-tab-bar>`, all form fields with shared components, toggle switches with `<app-toggle-switch>`, modals with `<app-dialog>`, form actions with `<app-form-actions>`, all buttons with `<app-button>`.
- [ ] **Step 3:** Remove orphaned SCSS, build check, commit: `refactor: use shared components in Questions and Settings`

---

### Task 15: Add loading states to all pages missing them

**Files:**
- Audit and modify all page components that fetch backend data

- [ ] **Step 1:** Audit each page for loading state: Dashboard (has it), Categories (has it), Customers (missing on list), Finance (has it), Inventory (has it), Products list (check), Product detail (has it), Purchase orders list (has it), Purchase order detail (has it), Questions (has it), Sales list (has it), Sale detail (has it), Settings (missing), Supplies (has it).
- [ ] **Step 2:** Add `loading` signal + skeleton to Customers list, Settings page, and any others found missing.
- [ ] **Step 3:** Verify all list pages show skeleton while loading, not empty states.
- [ ] **Step 4:** Build check, commit: `fix: add missing loading states across all pages`

---

### Task 16: Final build verification and cleanup

- [ ] **Step 1:** Run full build: `cd src/PeruShopHub.Web && npx ng build`. Fix any errors.
- [ ] **Step 2:** Search for orphaned SCSS patterns that should no longer exist: grep for `.btn-accent`, `.btn-ghost`, `.btn-icon`, `.toggle-label`, `.toggle-switch`, `.radio-group`, `.radio-label`, `.field-error`, `.dialog-overlay` in page-level SCSS files. Remove any that are now handled by shared components.
- [ ] **Step 3:** Verify no duplicate component definitions across page SCSS files.
- [ ] **Step 4:** Final build check, commit: `chore: cleanup orphaned SCSS after shared component migration`
