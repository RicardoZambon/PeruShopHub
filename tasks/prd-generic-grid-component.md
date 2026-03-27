# PRD: Generic Grid Component with Infinite Scroll

## Introduction

Extract and generalize the table pattern used across Products, Sales, Customers, Purchase Orders, and Inventory into a single reusable `DataGridComponent`. Every list page in the app duplicates the same structure: server-side sorting, pagination controls, loading skeletons, mobile card layout, and responsive breakpoints. The new component replaces all of this with a declarative column-based API using `ng-template` for custom cell rendering, and replaces traditional pagination with infinite scroll (append-on-scroll).

This eliminates ~60% of duplicated template/SCSS across 5+ pages and establishes a single source of truth for grid behavior, accessibility, and responsive design.

## Goals

- Single reusable grid component that handles all table rendering across the app
- Infinite scroll replaces traditional pagination — rows append as the user scrolls down
- Declarative column definitions with full custom cell rendering via `ng-template`
- Zero layout shift between loading and loaded states (skeleton placeholders match real layout)
- Mobile-responsive: table on desktop/tablet, card list on mobile
- Server-side sorting built into the grid; filtering stays with the parent page
- Migrate all 5 existing table pages to use the new grid component
- Remove the old `DataTableComponent` once migration is complete

## User Stories

### US-001: Core Grid Component with Column Definitions
**Description:** As a developer, I want a grid component that accepts column definitions and data so I can render any tabular data without building custom table HTML.

**Acceptance Criteria:**
- [ ] `DataGridComponent` created in `shared/components/data-grid/`
- [ ] Accepts `columns` input — array of column config objects with: `key`, `label`, `align` (left/center/right), `width` (optional), `sortable` (boolean), `sticky` (boolean)
- [ ] Accepts `data` input — array of `Record<string, any>`
- [ ] Renders a `<table>` with `<thead>` and `<tbody>` based on column definitions
- [ ] Default cell rendering: displays `row[column.key]` as text
- [ ] Columns with `align: 'right'` right-align both header and cells
- [ ] Columns with `width` set apply that width (e.g., `'56px'`, `'20%'`)
- [ ] Typecheck passes (`ng build` succeeds)

### US-002: Custom Cell Rendering via ng-template
**Description:** As a developer, I want to provide custom templates for specific columns so I can render images, badges, buttons, and complex content inside grid cells.

**Acceptance Criteria:**
- [ ] Grid supports `<ng-template appGridCell="columnKey" let-row let-value="value">` directive
- [ ] When a template is registered for a column key, the grid uses it instead of default text rendering
- [ ] Template receives the full row object and the cell value as template context
- [ ] Works for any number of columns — some can use default rendering, others custom templates
- [ ] Example: product image column renders `<img>` tag; status column renders `<app-badge>`
- [ ] Typecheck passes

### US-003: Custom Header Rendering via ng-template
**Description:** As a developer, I want to optionally provide custom header templates for columns that need non-text headers (e.g., icon-only headers, tooltips).

**Acceptance Criteria:**
- [ ] Grid supports `<ng-template appGridHeader="columnKey">` directive
- [ ] When a header template is registered, it replaces the default label text for that column
- [ ] Columns without a custom header template render the `label` string as before
- [ ] Typecheck passes

### US-004: Server-Side Sorting
**Description:** As a user, I want to click column headers to sort the grid so I can find what I need quickly.

**Acceptance Criteria:**
- [ ] Columns marked `sortable: true` show a sort indicator (neutral ⇅, ascending ▲, descending ▼)
- [ ] Clicking a sortable header cycles: ascending → descending → neutral (clear sort)
- [ ] Grid emits `sortChange` event with `{ column: string, direction: 'asc' | 'desc' | null }`
- [ ] Only one column can be sorted at a time — clicking a new column resets the previous
- [ ] Grid does NOT sort data internally — it delegates to the parent (server-side)
- [ ] Active sort column header is visually highlighted
- [ ] Typecheck passes

### US-005: Infinite Scroll
**Description:** As a user, I want the grid to load more rows automatically as I scroll down so I don't have to click pagination buttons.

**Acceptance Criteria:**
- [ ] Grid detects when the user scrolls near the bottom of the table container (configurable threshold, default 200px)
- [ ] Emits `loadMore` event when threshold is reached and `hasMore` input is `true`
- [ ] Does NOT emit `loadMore` while a load is in progress (`loading` input is `true`)
- [ ] Shows a loading indicator (spinner row) at the bottom while loading more data
- [ ] Parent component is responsible for appending new items to the `data` array
- [ ] Accepts `hasMore` input (boolean) — when `false`, no more `loadMore` events are emitted
- [ ] Accepts `pageSize` input (default 20) for the parent to know batch size
- [ ] Grid scroll container uses `overflow-y: auto` with proper height (flex: 1 in parent layout)
- [ ] Scroll position is preserved when new rows are appended
- [ ] Typecheck passes

### US-006: Initial Loading State (Skeleton Placeholders)
**Description:** As a user, I want to see skeleton placeholders while the grid loads for the first time so the page doesn't appear broken.

**Acceptance Criteria:**
- [ ] When `loading` is `true` AND `data` is empty, grid renders skeleton rows (default 8 rows)
- [ ] Skeleton rows mirror the column layout: same number of cells, same alignment, same widths
- [ ] Skeleton cells use `<app-skeleton>` component with appropriate width/height per column
- [ ] When `loading` is `true` AND `data` is NOT empty, skeleton rows are NOT shown (infinite scroll spinner is shown instead at the bottom)
- [ ] Typecheck passes

### US-007: Empty State
**Description:** As a user, I want to see a clear message when there's no data so I know it's not a loading error.

**Acceptance Criteria:**
- [ ] When `loading` is `false` AND `data` is empty, grid shows an empty state
- [ ] Grid supports `<ng-template appGridEmpty>` for custom empty state content
- [ ] If no custom template is provided, renders a default empty state with configurable `emptyTitle` and `emptyDescription` inputs
- [ ] Empty state is vertically centered within the grid container
- [ ] Typecheck passes

### US-008: Mobile Card Layout
**Description:** As a user on mobile, I want data displayed as cards instead of a table so it's readable on small screens.

**Acceptance Criteria:**
- [ ] Below 768px breakpoint, the table is hidden and a card list is displayed
- [ ] Grid supports `<ng-template appGridCard let-row>` for custom mobile card rendering
- [ ] If no card template is provided, grid renders a default card with all column label-value pairs stacked vertically
- [ ] Card layout also supports infinite scroll (same `loadMore` behavior)
- [ ] Skeleton cards shown during initial loading on mobile
- [ ] Typecheck passes

### US-009: Row Click Events
**Description:** As a developer, I want the grid to emit row click events so I can navigate to detail pages.

**Acceptance Criteria:**
- [ ] Grid emits `rowClick` event with the full row object when a row is clicked
- [ ] Rows have `cursor: pointer` and hover highlight styling
- [ ] Click events do NOT fire when the user clicks an interactive element inside a cell (button, link, checkbox)
- [ ] On mobile, card click emits the same `rowClick` event
- [ ] Typecheck passes

### US-010: Sticky Header and First Column
**Description:** As a user scrolling through a long table, I want the header row to stay visible and optionally the first column to stay pinned on tablet.

**Acceptance Criteria:**
- [ ] Table header (`<thead>`) is sticky at the top of the scroll container
- [ ] Columns marked with `sticky: true` remain pinned when scrolling horizontally (tablet breakpoint)
- [ ] Sticky column has a right border shadow to indicate scroll underneath
- [ ] Custom scrollbar styling matches existing app scrollbar design
- [ ] Typecheck passes

### US-011: Scroll-to-Top Utility
**Description:** As a developer, I want to programmatically scroll the grid back to the top when filters or sort change so the user sees results from the beginning.

**Acceptance Criteria:**
- [ ] Grid exposes a public `scrollToTop()` method
- [ ] Parent can call it via `@ViewChild` reference when filters/sort change
- [ ] Smooth scroll animation (CSS `scroll-behavior: smooth`)
- [ ] Typecheck passes

### US-012: Migrate Products List
**Description:** As a developer, I want to migrate the Products list page to use `DataGridComponent` so we validate the component with the most complex use case.

**Acceptance Criteria:**
- [ ] Products list uses `<app-data-grid>` instead of custom `<table>`
- [ ] All 9 columns render identically to current implementation (image, name, SKU, price, stock, variants, status, margin, actions)
- [ ] Custom cell templates for: image, stock (conditional coloring), variants (badge + review badge), status (badge), margin (margin-badge), actions (edit button)
- [ ] Infinite scroll replaces pagination — loads 20 items per batch
- [ ] Search and status filter still work (parent-owned, trigger grid reset + scrollToTop)
- [ ] Server-side sorting still works for all sortable columns
- [ ] Mobile card layout matches current design
- [ ] Old custom table HTML and SCSS removed from products-list component
- [ ] Verify in browser that the products page renders and scrolls correctly

### US-013: Migrate Sales List
**Description:** As a developer, I want to migrate the Sales list page to use `DataGridComponent`.

**Acceptance Criteria:**
- [ ] Sales list uses `<app-data-grid>` with columns: ID, Date, Buyer, Items, Value, Profit, Status
- [ ] Custom cell templates for: ID (mono, clickable), profit (conditional green/red), status (badge)
- [ ] Infinite scroll replaces pagination
- [ ] Search, date range, and status filters still work (parent-owned)
- [ ] Mobile card layout matches current design
- [ ] Old custom table HTML and SCSS removed
- [ ] Verify in browser that the sales page renders and scrolls correctly

### US-014: Migrate Customers List
**Description:** As a developer, I want to migrate the Customers list page to use `DataGridComponent`.

**Acceptance Criteria:**
- [ ] Customers list uses `<app-data-grid>` with columns: Name, Nickname, Email, Total Orders, Total Spent, Last Purchase
- [ ] Custom cell templates for: nickname (mono), total spent (currency + visual indicator), last purchase (relative date)
- [ ] Infinite scroll replaces current load-all pattern
- [ ] Search and sorting still work
- [ ] Mobile card layout matches current design
- [ ] Old custom table HTML and SCSS removed
- [ ] Verify in browser that the customers page renders and scrolls correctly

### US-015: Migrate Purchase Orders List
**Description:** As a developer, I want to migrate the Purchase Orders list page to use `DataGridComponent`.

**Acceptance Criteria:**
- [ ] Purchase Orders list uses `<app-data-grid>` with columns: Supplier, Status, Items, Total, Created Date, Received Date
- [ ] Custom cell templates for: status (badge), total (currency mono)
- [ ] Infinite scroll replaces pagination
- [ ] Search and status filters still work
- [ ] Mobile card layout matches current design
- [ ] Old custom table HTML and SCSS removed
- [ ] Verify in browser that the purchase orders page renders and scrolls correctly

### US-016: Migrate Inventory Movements Table
**Description:** As a developer, I want to migrate the Inventory movements table to use `DataGridComponent`.

**Acceptance Criteria:**
- [ ] Movements table uses `<app-data-grid>` with columns: Date, Product, Type, Quantity, Unit Cost, Notes
- [ ] Custom cell templates for: type (colored badge), quantity (signed +/- with color)
- [ ] Infinite scroll replaces pagination
- [ ] Type filter still works
- [ ] Mobile card layout matches current design
- [ ] Old custom table HTML and SCSS removed
- [ ] Verify in browser that the inventory movements tab renders and scrolls correctly

### US-017: Scroll-to-Row for Deep Linking
**Description:** As a developer, I want to programmatically scroll to a specific row so that after creating or editing an item, the user sees it highlighted in the grid.

**Acceptance Criteria:**
- [ ] Grid exposes a public `scrollToRow(predicate: (row: any) => boolean)` method
- [ ] Method scrolls the matching row into view and applies a brief highlight animation (e.g., 1.5s fade background pulse)
- [ ] If the row is not yet loaded (beyond current data), the method returns `false` so the parent can decide to load more
- [ ] Works on both desktop table and mobile card layout
- [ ] Typecheck passes

### US-018: ARIA Accessibility Attributes
**Description:** As a user with assistive technology, I want the grid to have proper ARIA attributes so screen readers can navigate the table correctly.

**Acceptance Criteria:**
- [ ] Table element has `role="grid"` and `aria-label` (configurable input, defaults to `"Data grid"`)
- [ ] Header row has `role="row"` and header cells have `role="columnheader"`
- [ ] Sortable headers include `aria-sort` attribute (`ascending`, `descending`, or `none`)
- [ ] Body rows have `role="row"` and cells have `role="gridcell"`
- [ ] Loading state announces via `aria-live="polite"` region (e.g., "Carregando dados")
- [ ] Empty state is announced via `aria-live="polite"`
- [ ] Infinite scroll "loading more" spinner has `aria-label="Carregando mais itens"`
- [ ] Typecheck passes

### US-019: Migrate Inventory Overview Table
**Description:** As a developer, I want to migrate the Inventory overview table to use `DataGridComponent` with infinite scroll so it's consistent with the rest of the app and ready for dataset growth.

**Acceptance Criteria:**
- [ ] Inventory overview uses `<app-data-grid>` with columns: SKU, Product, Total Stock, Reserved, Available, Unit Cost, Stock Value
- [ ] Custom cell templates for: available (conditional warning/danger coloring for low/zero stock), costs (mono currency)
- [ ] Infinite scroll replaces current load-all pattern
- [ ] Backend `GET /api/inventory` updated to support pagination via `PagedResult<InventoryItemDto>` (with `page`, `pageSize` query params)
- [ ] Row highlighting for low stock (≤5) and zero stock preserved
- [ ] Mobile card layout
- [ ] Old custom table HTML and SCSS removed
- [ ] Verify in browser that the inventory overview tab renders and scrolls correctly

### US-020: Remove Old DataTableComponent
**Description:** As a developer, I want to remove the old `DataTableComponent` once all pages are migrated so we don't maintain two grid implementations.

**Acceptance Criteria:**
- [ ] Verify no component imports or uses `DataTableComponent`
- [ ] Delete `shared/components/data-table/` directory
- [ ] Remove from shared module exports/declarations
- [ ] App builds without errors
- [ ] Typecheck passes

## Functional Requirements

- FR-1: `DataGridComponent` must accept column definitions as an input array with properties: `key`, `label`, `align`, `width`, `sortable`, `sticky`
- FR-2: Custom cell rendering must use Angular's `ng-template` with a structural directive (`appGridCell`) keyed by column name, receiving the row object and cell value as context
- FR-3: Custom header rendering must use `ng-template` with an `appGridHeader` directive keyed by column name
- FR-4: Sorting must be server-side only — the grid emits sort events but never reorders data internally
- FR-5: Infinite scroll must detect proximity to scroll bottom (configurable threshold) and emit `loadMore` events
- FR-6: The grid must not emit `loadMore` while `loading` is `true` or `hasMore` is `false`
- FR-7: Initial loading state (loading + empty data) must show skeleton rows matching the column layout
- FR-8: Incremental loading state (loading + existing data) must show a spinner row appended at the bottom
- FR-9: Empty state (not loading + empty data) must display a customizable empty state via `ng-template` or default text inputs
- FR-10: Below 768px, the grid must switch from table to card layout using a customizable card template
- FR-11: Row clicks must emit the full row object; clicks on interactive child elements (buttons, links) must not bubble to the row handler
- FR-12: Table header must be sticky; columns marked `sticky: true` must pin horizontally on tablet+
- FR-13: Grid must expose a `scrollToTop()` public method for programmatic scroll reset
- FR-14: When parent changes filters (data is reset to empty + loading), grid must show initial skeletons again
- FR-15: Grid must support `trackBy` function input for efficient DOM updates when data array changes
- FR-16: All monetary values displayed in the grid must use monospace font and BRL formatting per the design system
- FR-17: Grid must expose a `scrollToRow(predicate)` method that scrolls a matching row into view and highlights it briefly
- FR-18: Grid must include ARIA attributes: `role="grid"`, `role="columnheader"` with `aria-sort`, `role="gridcell"`, and `aria-live` regions for loading/empty state announcements
- FR-19: Inventory overview endpoint (`GET /api/inventory`) must be updated to support pagination via `PagedResult<InventoryItemDto>`

## Non-Goals

- **No client-side sorting or filtering** — the grid is a pure presentation component; all data logic stays in the parent
- **No row selection** (single or multi) — rows are click-to-navigate only
- **No inline editing** — cells are read-only
- **No column reordering or resizing by the user** — columns are fixed by the developer
- **No column visibility toggles** — all defined columns are always shown
- **No export functionality** (CSV, Excel) — handled elsewhere
- **No virtual scrolling (windowing)** — infinite scroll appends real DOM rows; virtual scrolling is a future optimization if performance becomes an issue with 1000+ rows
- **No cursor-based pagination on the backend** — infinite scroll uses offset/limit with the existing `PagedResult<T>` API

## Design Considerations

### Component API (Developer Experience)

```html
<!-- Parent template example -->
<app-data-grid
  [columns]="columns"
  [data]="products()"
  [loading]="loading()"
  [hasMore]="hasMore()"
  [pageSize]="20"
  [emptyTitle]="'Nenhum produto encontrado'"
  [emptyDescription]="'Tente ajustar os filtros'"
  [trackBy]="trackByProductId"
  (sortChange)="onSort($event)"
  (loadMore)="onLoadMore()"
  (rowClick)="onRowClick($event)">

  <!-- Custom cell: image column -->
  <ng-template appGridCell="image" let-row let-value="value">
    <img [src]="value" [alt]="row.name" class="product-thumb" />
  </ng-template>

  <!-- Custom cell: status column -->
  <ng-template appGridCell="status" let-value="value">
    <app-badge [variant]="getStatusVariant(value)">{{ value }}</app-badge>
  </ng-template>

  <!-- Custom mobile card -->
  <ng-template appGridCard let-row>
    <div class="product-card">...</div>
  </ng-template>

  <!-- Custom empty state -->
  <ng-template appGridEmpty>
    <app-empty-state title="Sem produtos" />
  </ng-template>
</app-data-grid>
```

### Column Definition Interface

```typescript
interface GridColumn {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';  // default: 'left'
  width?: string;                         // e.g., '56px', '20%'
  sortable?: boolean;                     // default: false
  sticky?: boolean;                       // default: false
  headerClass?: string;                   // additional CSS class for <th>
  cellClass?: string;                     // additional CSS class for <td>
}
```

### Styling

- Reuse existing CSS custom properties (`--neutral-*`, `--space-*`, `--text-*`, `--radius-*`, `--shadow-*`)
- Table rows: `border-bottom: 1px solid var(--neutral-100)`
- Hover: `background: var(--neutral-50)`
- Sticky header: `background: var(--bg-primary); z-index: 2`
- Custom scrollbar styling matching existing app pattern
- Infinite scroll spinner: centered below last row, uses existing spinner component

## Technical Considerations

- **Scroll detection:** Use `IntersectionObserver` on a sentinel element placed after the last row — more performant than scroll event listeners
- **Change detection:** Component should use `OnPush` change detection strategy
- **TrackBy:** Required for efficient list updates when appending data — parent provides a `trackBy` function
- **Template collection:** Use `@ContentChildren(GridCellDirective)` and `@ContentChildren(GridHeaderDirective)` to collect custom templates
- **Cleanup:** All subscriptions and observers must be cleaned up in `ngOnDestroy`
- **Backend compatibility:** Infinite scroll uses the existing `PagedResult<T>` API — the parent increments `page` and appends `items` to the data signal. `hasMore` is computed as `data.length < totalCount`
- **Filter reset pattern:** When filters change, the parent resets data to `[]`, sets `loading = true`, resets page to 1, and calls `scrollToTop()`. The grid shows skeletons until the first batch arrives.

## Success Metrics

- All 6 list pages (Products, Sales, Customers, Purchase Orders, Inventory Overview, Inventory Movements) use `DataGridComponent`
- Old `DataTableComponent` is deleted with zero references
- Each migrated page's template is reduced by ~50% in line count (table HTML + SCSS removed)
- Grid infinite scroll loads batches within 300ms on typical connection
- No visual regression on desktop, tablet, or mobile breakpoints
- `ng build --configuration production` succeeds with no warnings

## Resolved Questions

- **Scroll to row:** Yes — US-017 adds `scrollToRow(predicate)` with highlight animation
- **ARIA accessibility:** Yes, included now — US-018 adds full ARIA attributes
- **Inventory overview table:** Yes, migrate to grid with infinite scroll — US-019 includes backend pagination update
