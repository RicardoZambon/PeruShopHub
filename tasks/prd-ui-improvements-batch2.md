# PRD: UI Improvements Batch 2

## Introduction

A collection of UI fixes, new screens, and feature additions covering: sidebar menu reorganization with groups, categories page layout fixes, sales detail improvements (manual costs CRUD, state locking), customer detail as full page, inventory entry functionality, a new internal assets/supplies management system, and the Anúncios (Listings) placeholder screen.

**Scope:** Frontend only (Angular). Mock data. Current branch `ralph/ui-ux-design-system`.

---

## Goals

- Reorganize sidebar navigation with logical groupings and labels
- Fix layout inconsistencies across pages (margins, spacing)
- Add manual cost management (add/edit/remove) to sales detail
- Lock sales after "Enviado" status (with admin override)
- Convert customer profile from modal to full-page view
- Fix non-functional buttons (Estoque entry, manual cost)
- Introduce internal assets/supplies management for packaging materials
- Add Anúncios (Listings) placeholder screen, splitting from Produtos

---

## User Stories

### US-052: Sidebar menu reorganization with group labels

**Description:** As a user, I want the sidebar menu organized into logical groups so I can find features faster.

**Acceptance Criteria:**
- [ ] Menu items in this order with group labels:
  ```
  [No label - standalone]
  Dashboard

  COMERCIAL
  Vendas
  Perguntas
  Anúncios
  Clientes

  CATÁLOGO
  Produtos
  Categorias
  Estoque
  Financeiro

  [No label - standalone]
  Configurações
  ```
- [ ] Group labels: small uppercase text in `--neutral-500`, `--label-size` (12px), `--label-weight` (500), with `letter-spacing: 0.05em`
- [ ] Group labels have `margin-top: var(--space-4)` for vertical separation (except first group)
- [ ] When sidebar is collapsed (icons only), group labels are hidden but a thin horizontal divider line appears between groups
- [ ] "Anúncios" uses `Megaphone` icon from lucide-angular
- [ ] NavItem interface extended with optional `group` property
- [ ] Mobile drawer shows same grouping

### US-053: Anúncios placeholder screen

**Description:** As a user, I want to see an Anúncios page in the menu that will eventually manage marketplace listings separately from products.

**Acceptance Criteria:**
- [ ] New route `/anuncios` with lazy-loaded component
- [ ] Empty state page with `Megaphone` icon, title "Anúncios", description "Gerencie seus anúncios nos marketplaces. Em breve."
- [ ] Follows existing page patterns (standalone component, design tokens)

### US-054: Categories page layout fixes

**Description:** As a user, I want the categories page to have consistent margins and a reorganized header matching other pages.

**Acceptance Criteria:**
- [ ] Page margins match other pages (use `--page-padding-desktop` / `--page-padding-tablet` / `--page-padding-mobile` — check the layout component for how other pages get their padding)
- [ ] Page header: "Categorias" title on the left, "Adicionar Categoria" button on the right (same pattern as Products list header with "Novo Produto")
- [ ] Search input moved below the title bar, full-width above the tree panel (not inside the tree)
- [ ] Tree panel starts below the search input
- [ ] Remove search and add button from inside the tree component — they're now in the page header

### US-055: Vendas detail — vertical spacing for groupings

**Description:** As a user, I want consistent vertical spacing between grouped sections on the sales detail page.

**Acceptance Criteria:**
- [ ] Add the same spacing used between columns (`--space-6` / `--section-gap-desktop`) as vertical separator between grouped sections
- [ ] Applies to: order info section, cost breakdown section, shipping section, payment section

### US-056: Vendas detail — manual cost CRUD

**Description:** As a user, I want to add, edit, and remove manual costs on a sale so I can track extra expenses like insurance or additional fees.

**Acceptance Criteria:**
- [ ] "Adicionar custo manual" button opens an inline form (expandable card, not modal) with fields:
  - **Categoria** (dropdown): options from the existing cost categories defined in the architecture: `marketplace_commission`, `fixed_fee`, `shipping_seller`, `payment_fee`, `tax_icms`, `tax_pis_cofins`, `storage_daily`, `fulfillment_fee`, `packaging`, `advertising`, `other` — with Portuguese labels
  - **Descrição** (text input, required): free-text description
  - **Valor** (R$ prefix, number, required): cost amount
- [ ] On save, the cost is added to the cost breakdown table with `source: 'manual'` badge
- [ ] Manual costs show an edit icon (pencil) and delete icon (trash) on hover
- [ ] Edit: re-opens the inline form pre-filled with the cost's values
- [ ] Delete: confirmation dialog "Remover custo manual?" → removes from list
- [ ] Cost breakdown table recalculates totals when manual costs are added/edited/removed
- [ ] API/automatic costs (source: 'api' or 'calculated') are not editable/deletable

### US-057: Vendas — lock after "Enviado" status

**Description:** As a user, once a sale reaches "Enviado" status, I want the sale details to be locked to prevent accidental changes, with admin override.

**Acceptance Criteria:**
- [ ] When sale status is "Enviado" or "Entregue":
  - All editable fields become read-only (disabled inputs)
  - "Adicionar custo manual" button is disabled
  - A lock icon + "Venda bloqueada" banner appears at the top of the detail page
  - Banner text: "Esta venda foi enviada e está bloqueada para edição."
- [ ] Admin override: a "Desbloquear" button (outline, small) in the banner
  - Clicking shows confirmation: "Desbloquear esta venda permitirá edições. Continuar?"
  - On confirm: `isLocked` flag is set to false, fields become editable again
  - The unlock is temporary (per session) — reloading the page re-locks it
- [ ] Visual indicator: `--neutral-100` background on locked fields, lock icon (`Lock` from lucide) next to status badge

### US-058: Customer profile as full page

**Description:** As a user, I want customer details to open as a full page at `/clientes/:id` instead of a side modal.

**Acceptance Criteria:**
- [ ] New route `/clientes/:id` with lazy-loaded `CustomerDetailComponent`
- [ ] Page layout matches product detail page pattern: back link "Voltar para Clientes", header with customer name + info, KPI cards, order history table
- [ ] Customer info section: name, email, phone, CPF/CNPJ, address, total orders, total spent, first/last purchase dates
- [ ] Recent orders table: order ID, date, value, status — clicking navigates to `/vendas/:id`
- [ ] Remove the existing side modal/panel from the customers list
- [ ] Clicking a customer row in the list navigates to `/clientes/:id`

### US-059: Estoque — fix "Registrar Entrada" button

**Description:** As a user, I want the "Registrar Entrada" button to work so I can record stock entries.

**Acceptance Criteria:**
- [ ] Clicking "Registrar Entrada" opens a modal dialog (480px) with fields:
  - **Produto** (searchable dropdown from product list)
  - **Quantidade** (number, required, min 1)
  - **Custo unitário** (R$ prefix, optional)
  - **Nota fiscal** (text, optional)
  - **Observação** (textarea, optional)
- [ ] On save: adds mock entry to a stock movements list, updates product stock count
- [ ] Stock movements section below the main stock table showing recent entries/exits
- [ ] Each movement row: date, product, type (Entrada/Saída), quantity, unit cost, note

### US-060: Internal assets/supplies management

**Description:** As a user, I want to manage internal supplies (packaging materials, boxes, labels, wrapping) separately from sellable products, with stock and cost tracking.

**Acceptance Criteria:**
- [ ] New menu item "Suprimentos" under the CATÁLOGO group (between Estoque and Financeiro), icon: `PackageOpen` from lucide
- [ ] New route `/suprimentos` with lazy-loaded component
- [ ] List view matching products list pattern: table with columns: Nome, SKU, Categoria (Embalagem/Etiqueta/Outros), Custo unitário (R$), Estoque, Status
- [ ] CRUD: "Novo Suprimento" button → form with fields:
  - **Nome** (required): e.g., "Plástico bolha rolo 50m"
  - **SKU** (required): e.g., "SUP-PB-50M"
  - **Categoria** (dropdown): Embalagem, Etiqueta, Caixa, Fita, Proteção, Outros
  - **Custo unitário** (R$, required)
  - **Estoque atual** (number)
  - **Estoque mínimo** (number — for alerts)
  - **Fornecedor** (text, optional)
  - **Observação** (textarea, optional)
- [ ] Low stock alert: rows with stock ≤ minimum show `--warning` color
- [ ] Out of stock: `--danger` color

### US-061: Vendas — associate internal assets to a sale

**Description:** As a user, when processing a sale, I want to indicate which internal supplies were used for shipping.

**Acceptance Criteria:**
- [ ] New "Suprimentos utilizados" section in the sales detail page (below cost breakdown, above shipping)
- [ ] "Adicionar suprimento" button opens inline form:
  - **Suprimento** (searchable dropdown from supplies list)
  - **Quantidade** (number, min 1)
- [ ] Each added supply shows: name, quantity used, unit cost, total cost (qty × unit cost)
- [ ] Can remove an added supply (X button)
- [ ] Total supplies cost is added to the cost breakdown automatically as category `packaging`
- [ ] Section is read-only when sale is locked (US-057)
- [ ] If sale has no supplies: show "Nenhum suprimento utilizado" muted text

---

## Functional Requirements

- **FR-1:** Sidebar supports grouped navigation items with section labels
- **FR-2:** Group labels hidden when sidebar is collapsed; thin divider shown instead
- **FR-3:** Manual costs on sales support full CRUD with `source: 'manual'` tracking
- **FR-4:** API/calculated costs are read-only — only manual costs are editable
- **FR-5:** Sales are locked (read-only) when status is "Enviado" or "Entregue"
- **FR-6:** Admin can temporarily unlock a locked sale (per session only)
- **FR-7:** Internal supplies are tracked separately from sellable products with their own stock and cost
- **FR-8:** Supplies used per sale are tracked and their cost contributes to the packaging cost category
- **FR-9:** Customer detail is a full page route, not a side modal
- **FR-10:** Stock entry modal adds mock movements to a movement history list
- **FR-11:** Anúncios page is a placeholder for future marketplace listing management

---

## Non-Goals

- Backend API implementation
- Actual marketplace listing sync (Anúncios)
- Supply reorder automation
- Supply purchase order management
- Multi-warehouse supply tracking
- Cost approval workflows

---

## Design Considerations

### Sidebar Groups
```
┌──────────────────────┐
│ 🏠 Dashboard         │
│                      │
│ COMERCIAL            │
│ 🛒 Vendas            │
│ 💬 Perguntas          │
│ 📢 Anúncios          │
│ 👥 Clientes           │
│                      │
│ CATÁLOGO             │
│ 📦 Produtos           │
│ 📂 Categorias         │
│ 🏭 Estoque            │
│ 🏷️ Suprimentos        │
│ 💰 Financeiro         │
│                      │
│ ⚙️ Configurações      │
└──────────────────────┘
```

### Collapsed sidebar (icons only)
```
┌────┐
│ 🏠 │
│────│  ← thin divider
│ 🛒 │
│ 💬 │
│ 📢 │
│ 👥 │
│────│  ← thin divider
│ 📦 │
│ 📂 │
│ 🏭 │
│ 🏷️ │
│ 💰 │
│────│  ← thin divider
│ ⚙️ │
└────┘
```

### Manual Cost Form (inline expandable)
```
┌──────────────────────────────────────┐
│ + Adicionar custo manual             │
├──────────────────────────────────────┤
│ Categoria: [▼ Outros              ]  │
│ Descrição: [Seguro adicional      ]  │
│ Valor:     [R$ 15,00              ]  │
│            [Cancelar]    [Salvar]     │
└──────────────────────────────────────┘
```

### Locked Sale Banner
```
┌──────────────────────────────────────┐
│ 🔒 Esta venda foi enviada e está     │
│    bloqueada para edição.            │
│                        [Desbloquear] │
└──────────────────────────────────────┘
```

---

## Technical Considerations

- Sidebar `NavItem` interface needs a `group?: string` property
- Sidebar component groups items by `group` and renders labels between groups
- Supply entity reuses product list patterns (table, search, CRUD)
- Sale lock state managed via a signal in the sale detail component
- Manual costs stored in a signal array alongside API costs
- Customer detail follows `produto-detail` component pattern exactly

---

## Docs Updates Required

- Update `Docs/PeruShopHub-Design-PreProjeto.md` to include:
  - Internal supplies entity definition
  - Supply-to-sale association table
  - Updated navigation map with new menu structure
- Update `Docs/PeruShopHub-Roadmap.md` to reference Anúncios as separate from Produtos

---

## Open Questions

None — all resolved in conversation.
