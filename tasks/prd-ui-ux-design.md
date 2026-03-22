# PRD: PeruShopHub UI/UX Design System & Application Layout

## Introduction

Define the complete visual design system and screen-by-screen layout for PeruShopHub — a multi-marketplace management hub focused on real per-sale profitability. This PRD covers the design tokens, component patterns, responsive strategy, every application screen, and the interaction model. The output guides all frontend implementation from Phase 0 through MVP and beyond.

The design must communicate **data density without clutter**, prioritize **financial clarity** (the product's differentiator), and feel **professional yet approachable** — a tool sellers trust with their money data.

## Goals

- Establish a complete design system (tokens, components, patterns) reusable across all screens
- Define layout, content, states, and behavior for every MVP screen
- Provide detailed specs for the 3 critical screens: Dashboard, Order Detail (cost breakdown), Financial Reports
- Support light + dark themes from day one
- Be fully responsive: desktop-first, functional on tablet and mobile
- Enable fast implementation by Angular developers without a dedicated designer

## Design System

### DS-001: Color Tokens

#### Light Theme

| Token | Value | Usage |
|-------|-------|-------|
| `--primary` | `#1A237E` | Primary actions, sidebar active, links |
| `--primary-hover` | `#283593` | Primary hover state |
| `--primary-light` | `#E8EAF6` | Primary backgrounds, selected rows |
| `--accent` | `#FF6F00` | CTAs, highlights, badges, attention |
| `--accent-hover` | `#E65100` | Accent hover state |
| `--accent-light` | `#FFF3E0` | Accent backgrounds |
| `--success` | `#2E7D32` | Positive values, profit, connected, delivered |
| `--success-light` | `#E8F5E9` | Success backgrounds |
| `--warning` | `#F57F17` | Caution, low stock, expiring |
| `--warning-light` | `#FFFDE7` | Warning backgrounds |
| `--danger` | `#C62828` | Negative values, losses, errors, disconnected |
| `--danger-light` | `#FFEBEE` | Danger backgrounds |
| `--neutral-900` | `#212121` | Primary text |
| `--neutral-700` | `#616161` | Secondary text |
| `--neutral-500` | `#9E9E9E` | Placeholder text, disabled |
| `--neutral-300` | `#E0E0E0` | Borders, dividers |
| `--neutral-100` | `#F5F5F5` | Page background, alt rows |
| `--neutral-50` | `#FAFAFA` | Card background |
| `--surface` | `#FFFFFF` | Cards, modals, dropdowns |
| `--page-bg` | `#F5F5F5` | Page background |

#### Dark Theme

| Token | Value | Usage |
|-------|-------|-------|
| `--primary` | `#7986CB` | Primary actions (lighter for contrast) |
| `--primary-hover` | `#9FA8DA` | Primary hover |
| `--primary-light` | `#1A237E` | Primary backgrounds (inverted) |
| `--accent` | `#FFB74D` | CTAs, highlights |
| `--accent-hover` | `#FFA726` | Accent hover |
| `--success` | `#66BB6A` | Positive values |
| `--warning` | `#FFF176` | Caution |
| `--danger` | `#EF5350` | Negative values |
| `--neutral-900` | `#FAFAFA` | Primary text (inverted) |
| `--neutral-700` | `#BDBDBD` | Secondary text |
| `--neutral-500` | `#757575` | Placeholder, disabled |
| `--neutral-300` | `#424242` | Borders |
| `--neutral-100` | `#303030` | Alt rows |
| `--surface` | `#1E1E1E` | Cards, modals |
| `--page-bg` | `#121212` | Page background |

### DS-002: Typography

| Token | Font | Size | Weight | Line Height | Usage |
|-------|------|------|--------|-------------|-------|
| `--heading-1` | Inter | 24px | 700 | 32px | Page titles |
| `--heading-2` | Inter | 20px | 600 | 28px | Section headings, card titles |
| `--heading-3` | Inter | 16px | 600 | 24px | Subsection headings |
| `--body` | Inter | 14px | 400 | 20px | Default body text |
| `--body-small` | Inter | 12px | 400 | 16px | Captions, table secondary |
| `--label` | Inter | 12px | 500 | 16px | Form labels, overlines |
| `--mono` | Roboto Mono | 13px | 400 | 18px | SKUs, order IDs, money values |
| `--metric-large` | Inter | 32px | 700 | 40px | Dashboard KPI numbers |
| `--metric-medium` | Inter | 20px | 600 | 28px | Card metric values |

### DS-003: Spacing Scale

Base unit: 4px. Scale: 0 (0px), 1 (4px), 2 (8px), 3 (12px), 4 (16px), 5 (20px), 6 (24px), 8 (32px), 10 (40px), 12 (48px), 16 (64px).

| Context | Value |
|---------|-------|
| Page padding (desktop) | 24px |
| Page padding (tablet) | 16px |
| Page padding (mobile) | 12px |
| Card padding | 16px |
| Card gap (grid) | 16px |
| Table cell padding | 12px horizontal, 8px vertical |
| Form field gap | 16px |
| Section gap | 24px |
| Sidebar width (expanded) | 256px |
| Sidebar width (collapsed) | 64px |
| Header height | 56px |

### DS-004: Elevation & Borders

| Token | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 1px 2px rgba(0,0,0,0.05)` | Cards, dropdowns |
| `--shadow-md` | `0 4px 6px rgba(0,0,0,0.07)` | Modals, popovers |
| `--shadow-lg` | `0 10px 15px rgba(0,0,0,0.1)` | Dialogs, notifications |
| `--radius-sm` | `4px` | Buttons, inputs, badges |
| `--radius-md` | `8px` | Cards, modals |
| `--radius-lg` | `12px` | Large containers |
| `--radius-full` | `9999px` | Avatars, pills |
| `--border` | `1px solid var(--neutral-300)` | Default border |

### DS-005: Responsive Breakpoints

| Name | Min Width | Target |
|------|-----------|--------|
| `mobile` | 0px | Phones (portrait) |
| `mobile-landscape` | 480px | Phones (landscape) |
| `tablet` | 768px | Tablets |
| `desktop` | 1024px | Small desktops |
| `desktop-lg` | 1280px | Standard desktops |
| `desktop-xl` | 1536px | Wide screens |

### DS-006: Component Library Decision

Component library to be decided during implementation. The design system defined here is framework-agnostic. Candidate libraries:
- **Angular Material** — Material Design, excellent a11y, needs more customization for data-dense layouts
- **PrimeNG** — Rich data table, calendar, chart integration out of box, more opinionated

Whichever is chosen must implement these design tokens via CSS custom properties and support theme switching.

---

## User Stories

### US-001: Design Token Infrastructure
**Description:** As a developer, I need a CSS/SCSS design token system so that all components use consistent colors, spacing, and typography, and theme switching works globally.

**Acceptance Criteria:**
- [ ] CSS custom properties file with all tokens from DS-001 through DS-005
- [ ] Light and dark theme token sets
- [ ] Theme toggle persists to `localStorage`
- [ ] `prefers-color-scheme` media query sets initial theme
- [ ] Typecheck/lint passes

### US-002: Application Shell — Sidebar
**Description:** As a user, I want a collapsible sidebar so I can navigate between sections while preserving screen space for data.

**Acceptance Criteria:**
- [ ] Sidebar with sections: Dashboard, Produtos, Vendas, Perguntas, Clientes, Financeiro, Estoque, Configuracoes
- [ ] Each item has icon + label text
- [ ] Collapse button toggles between 256px (expanded) and 64px (collapsed with icons only)
- [ ] Active route highlighted with `--primary` background
- [ ] Hover state on items
- [ ] Collapse state persists to `localStorage`
- [ ] On mobile (<768px): sidebar becomes an overlay drawer, triggered by hamburger in header
- [ ] On tablet (768-1023px): sidebar starts collapsed
- [ ] On desktop (1024px+): sidebar starts expanded
- [ ] PeruShopHub logo at top (Shih Tzu icon + text when expanded, icon only when collapsed)
- [ ] Verify in browser using dev-browser skill

### US-003: Application Shell — Header
**Description:** As a user, I want a top header bar with search, notifications, and user menu so I can access global actions from any screen.

**Acceptance Criteria:**
- [ ] Fixed header at top, 56px height
- [ ] Left: hamburger toggle (mobile/tablet) or sidebar collapse toggle (desktop)
- [ ] Center: global search input (placeholder: "Buscar pedidos, produtos, clientes...")
- [ ] Right: marketplace connection status indicator (green dot = connected, red = error)
- [ ] Right: notification bell icon with unread count badge (accent color)
- [ ] Right: theme toggle (sun/moon icon)
- [ ] Right: user avatar dropdown (nome, email, role, separator, "Configuracoes", "Sair")
- [ ] Notification bell click opens a slide-over panel with recent notifications
- [ ] Verify in browser using dev-browser skill

### US-004: Login Screen
**Description:** As a user, I want a login screen so I can authenticate into the system.

**Acceptance Criteria:**
- [ ] Centered card on page with PeruShopHub logo + tagline ("Gestao inteligente de marketplaces")
- [ ] Email and password fields with labels
- [ ] "Entrar" primary button (full width of card)
- [ ] "Esqueceu a senha?" link below button
- [ ] Form validation: required fields, email format
- [ ] Error state: invalid credentials message below form
- [ ] Loading state: button shows spinner, fields disabled
- [ ] Background: subtle gradient or pattern using primary/accent colors
- [ ] Responsive: card takes 100% width on mobile with padding, max 400px on desktop
- [ ] Supports dark theme
- [ ] Verify in browser using dev-browser skill

### US-005: Dashboard Screen
**Description:** As a user, I want a dashboard home screen showing KPIs, charts, and actionable summaries so I can understand my business health at a glance.

**Acceptance Criteria:**
- [ ] Period selector at top: "Hoje", "7 dias", "30 dias", "Custom" (date range picker)
- [ ] KPI cards row (4 cards, responsive grid):
  - Vendas (count + % change vs prior period)
  - Receita Bruta (BRL formatted + % change)
  - Lucro Liquido (BRL formatted + % change, green/red color)
  - Margem Media (% formatted + % change)
- [ ] KPI cards show upward/downward arrow icon with change percentage
- [ ] Chart: "Vendas e Lucro" — line chart with dual Y-axis (vendas count left, BRL right), lines for receita bruta and lucro liquido over time
- [ ] Chart: "Distribuicao de Custos" — donut chart showing cost category breakdown (comissao, frete, produto, embalagem, impostos, etc.)
- [ ] Table: "Top 5 Produtos Mais Lucrativos" — columns: Produto, Vendas, Receita, Lucro, Margem %. Rows clickable → navigate to product detail
- [ ] Table: "Top 5 Produtos Menos Lucrativos" — same columns, sorted ascending by margin. Rows with negative margin highlighted in danger-light
- [ ] Card: "Acoes Pendentes" — count badges for: Perguntas sem resposta, Pedidos pendentes de envio, Alertas ativos. Each clickable → navigate to respective screen
- [ ] All money values use `--mono` font and BRL formatting (R$ 1.234,56)
- [ ] Empty state: illustrated placeholder when no data yet ("Conecte seu Mercado Livre para comecar")
- [ ] Loading state: skeleton placeholders for all cards and charts
- [ ] Responsive: KPI cards 4 columns desktop, 2 columns tablet, 1 column mobile. Charts stack vertically on mobile
- [ ] Verify in browser using dev-browser skill

### US-006: Products List Screen
**Description:** As a user, I want to see all my products in a filterable, sortable table so I can manage my catalog.

**Acceptance Criteria:**
- [ ] Page title: "Produtos"
- [ ] Action bar: search input (by name/SKU), filters dropdown (status: Ativo/Pausado/Encerrado, marketplace, category), "Novo Produto" accent button
- [ ] Table columns: Foto (thumbnail 40x40), Nome, SKU (`--mono`), Preco (BRL), Estoque, Status (badge), Margem (% colored), Acoes (edit/view icons)
- [ ] Status badges: Ativo (success), Pausado (warning), Encerrado (neutral)
- [ ] Margin column: green if >= 20%, yellow if 10-19%, red if < 10%
- [ ] Server-side pagination with page size selector (10/25/50)
- [ ] Column sorting on: Nome, Preco, Estoque, Margem
- [ ] Row click → navigate to product detail
- [ ] Empty state: "Nenhum produto cadastrado" with "Novo Produto" CTA
- [ ] Loading state: table skeleton
- [ ] Responsive: on mobile, table becomes card list (photo + name + price + status per card)
- [ ] Verify in browser using dev-browser skill

### US-007: Product Create/Edit Screen
**Description:** As a user, I want to create and edit products with all required fields organized in clear sections.

**Acceptance Criteria:**
- [ ] Page title: "Novo Produto" or "Editar Produto: {name}"
- [ ] Tabs or accordion sections:
  - **Informacoes Basicas**: titulo (max 60 chars with counter), descricao (textarea), categoria (searchable dropdown with ML category tree), condicao (Novo/Usado)
  - **Preco e Custos**: preco de venda (BRL input), custo de aquisicao, custo embalagem, fornecedor, tipo de anuncio (Gratis/Classico/Premium). Shows real-time margin calculator below: "Margem estimada: X%" considering commission by category + listing type + fixed fee
  - **Fotos**: drag-and-drop upload zone, reorderable thumbnail grid, min 1 / max 10, shows ML specs (min 500x500px)
  - **Atributos**: dynamic form fields based on selected category (loaded from ML API). Required attributes marked with asterisk
  - **Variacoes**: add color/size variations with individual price, stock, and photo assignment per variation
  - **Envio**: modo de envio selector, dimensoes (peso, altura, largura, comprimento), frete gratis toggle
  - **Termos**: garantia tipo + tempo, max por compra
- [ ] Sticky bottom bar with: "Cancelar" (ghost button), "Salvar Rascunho" (outline), "Publicar" (accent)
- [ ] Form validation with inline error messages
- [ ] Unsaved changes warning on navigation
- [ ] Responsive: tabs become vertical accordion on mobile
- [ ] Verify in browser using dev-browser skill

### US-008: Product Detail Screen
**Description:** As a user, I want to see a product's complete information, performance metrics, and linked marketplace listings.

**Acceptance Criteria:**
- [ ] Header: product photo, name, SKU, status badge, ML item ID (link to ML listing), "Editar" button
- [ ] Metrics row: Vendas (30d), Receita (30d), Lucro (30d), Margem (30d), Estoque disponivel
- [ ] Section: "Anuncios Vinculados" — table of marketplace listings (marketplace icon, external ID, status, price, stock)
- [ ] Section: "Historico de Custos" — timeline showing cost changes with effective dates
- [ ] Section: "Vendas Recentes" — mini table of last 10 orders for this product
- [ ] Verify in browser using dev-browser skill

### US-009: Orders List Screen
**Description:** As a user, I want to see all orders across marketplaces with filters and status indicators.

**Acceptance Criteria:**
- [ ] Page title: "Vendas"
- [ ] Action bar: search (by order ID, buyer name), date range picker, status filter (Pago, Enviado, Entregue, Cancelado, Devolvido), marketplace filter
- [ ] Table columns: ID (`--mono`), Data, Comprador, Itens (count), Valor (BRL), Lucro (BRL, colored), Status (badge), Marketplace (icon)
- [ ] Status badges with distinct colors: Pago (primary), Enviado (warning), Entregue (success), Cancelado (danger), Devolvido (neutral)
- [ ] Lucro column: green text for positive, red for negative
- [ ] Server-side pagination
- [ ] Row click → order detail
- [ ] Empty state with illustration
- [ ] Responsive: card list on mobile (date + buyer + total + status + profit)
- [ ] Verify in browser using dev-browser skill

### US-010: Order Detail Screen (CRITICAL — detailed spec)
**Description:** As a user, I want to see complete order information including the full cost breakdown so I can understand exactly how much I earned on this sale.

**Acceptance Criteria:**
- [ ] Header: Order ID, date, status badge, marketplace icon, buyer name
- [ ] Section "Itens": product photo, name, SKU, variation, quantity, unit price, subtotal
- [ ] Section "Comprador": name, nickname, email, phone, total orders, total spent
- [ ] Section "Envio": tracking number (copyable), carrier, logistic type badge (Full/Coleta/Agencia), status timeline (criado → pago → enviado → entregue), estimated delivery, receiver address (collapsible)
- [ ] Section "Pagamento": method (icon + name), installments, transaction amount, status
- [ ] **Section "Decomposicao de Custos" (CRITICAL)**:
  - Visual stacked bar showing: Receita Bruta → each cost category as a colored segment → Lucro Liquido
  - Table below with columns: Categoria, Valor (BRL), % da Receita, Fonte (badge: API/Manual/Calculado)
  - Categories: Comissao ML, Taxa fixa, Frete vendedor, Taxa de pagamento, Custo do produto, Embalagem, Impostos, Armazenagem, Advertising, Outros
  - Summary row: Receita Bruta, Total Custos, **Lucro Liquido** (large, colored green/red)
  - Margin percentage displayed prominently
  - "Adicionar custo manual" button for ad-hoc costs
- [ ] Section "Timeline": chronological event log (criado, pago, etiqueta gerada, enviado, entregue)
- [ ] Raw API data toggle (collapsible JSON viewer for debugging)
- [ ] Verify in browser using dev-browser skill

### US-011: Questions Screen
**Description:** As a user, I want to manage pre-sale questions with quick response capabilities.

**Acceptance Criteria:**
- [ ] Page title: "Perguntas"
- [ ] Tab filters: "Sem Resposta" (count badge), "Respondidas", "Todas"
- [ ] Each question as a card: product thumbnail + title, buyer nickname, question text, timestamp, "Responder" button
- [ ] Inline response: clicking "Responder" expands textarea below the question within the same card
- [ ] Response templates: dropdown above textarea with saved templates, inserts template text
- [ ] After submitting response: card moves to "Respondidas" tab with success toast
- [ ] Unanswered questions sorted by oldest first (urgency)
- [ ] Time indicator: "ha 2h", "ha 1d" — turns red if > 24h unanswered
- [ ] Responsive: full-width cards on mobile
- [ ] Verify in browser using dev-browser skill

### US-012: Customers Screen
**Description:** As a user, I want to see my unique buyers and their purchase history.

**Acceptance Criteria:**
- [ ] Page title: "Clientes"
- [ ] Table columns: Nome, Nickname, Email, Total Pedidos, Total Gasto (BRL), Ultima Compra (relative date)
- [ ] Search by name, nickname, email
- [ ] Sort by: Total Gasto (desc), Total Pedidos (desc), Ultima Compra (desc)
- [ ] Row click → customer profile slide-over panel: buyer info + order history table
- [ ] Verify in browser using dev-browser skill

### US-013: Financial Summary Screen (CRITICAL — detailed spec)
**Description:** As a user, I want a financial overview showing revenue, costs, and profit trends so I can track business health.

**Acceptance Criteria:**
- [ ] Page title: "Financeiro"
- [ ] Sub-navigation tabs: "Resumo", "Lucratividade por SKU", "Conciliacao", "Curva ABC"
- [ ] **Resumo tab:**
  - Period selector (same as dashboard)
  - KPI row: Receita Bruta, Total Custos, Lucro Liquido, Margem Media, Ticket Medio
  - Chart: "Receita vs Lucro" — grouped bar chart per day/week/month (receita bar + lucro bar side by side)
  - Chart: "Evolucao da Margem" — line chart of margin % over time with threshold line at configured minimum margin
  - Chart: "Custos por Categoria" — horizontal stacked bar (one bar per period unit) with each cost category color-coded
  - Export buttons: "Exportar PDF", "Exportar Excel" (outline buttons, top right)
- [ ] **Lucratividade por SKU tab:**
  - Table: SKU, Produto, Vendas, Receita Bruta, CMV, Comissoes, Frete, Impostos, Armazenagem, Ads, Lucro Liquido, Margem %
  - Margem column: color gradient (green → yellow → red)
  - Sortable by any column
  - Expandable rows: clicking a row shows monthly breakdown sparkline
  - Export buttons
- [ ] **Conciliacao tab:**
  - Table: Periodo, Valor Esperado (calculated), Valor Depositado (from ML), Diferenca (BRL + %), Status (badge: OK/Divergencia)
  - Divergencia rows highlighted in warning-light
  - Click to expand: line-by-line comparison per order
- [ ] **Curva ABC tab:**
  - Horizontal bar chart: products sorted by profit contribution, cumulative line overlay
  - Table below: Rank, Produto, SKU, Lucro, % do Lucro Total, Classificacao (A/B/C badge)
  - A = top 80%, B = next 15%, C = bottom 5%
  - Badge colors: A=success, B=warning, C=danger
- [ ] Loading skeletons for all tabs
- [ ] Empty states per tab
- [ ] Verify in browser using dev-browser skill

### US-014: Inventory Screen
**Description:** As a user, I want to manage stock levels, see sync status, and track movements.

**Acceptance Criteria:**
- [ ] Page title: "Estoque"
- [ ] Sub-navigation tabs: "Visao Geral", "Movimentacoes", "Estoque Full" (future, disabled with "Em breve" tooltip)
- [ ] **Visao Geral tab:**
  - KPI row: Total SKUs, Unidades em Estoque, Itens Criticos (< min configured), Valor em Estoque (BRL)
  - Table: SKU, Produto, Estoque Total, Reservado, Disponivel, Alocacao ML (synced/pending/error badge), Ultima Sync
  - Low stock rows highlighted in warning-light
  - Zero stock rows highlighted in danger-light
  - "Registrar Entrada" accent button
- [ ] **Movimentacoes tab:**
  - Table: Data, SKU, Produto, Tipo (Entrada/Saida/Ajuste badge), Quantidade (+/-), Motivo, Usuario
  - Date range filter
  - Type filter
- [ ] Verify in browser using dev-browser skill

### US-015: Settings Screen
**Description:** As a user, I want to configure system settings, manage users, and control marketplace connections.

**Acceptance Criteria:**
- [ ] Page title: "Configuracoes"
- [ ] Left sub-navigation (vertical tabs on desktop, horizontal on mobile):
  - **Empresa**: nome, CNPJ, endereco, logo upload
  - **Usuarios**: table (nome, email, role badge, status, acoes). "Novo Usuario" button. Edit via modal (nome, email, role dropdown, ativo toggle)
  - **Integracoes**: marketplace connection cards. Each card: marketplace logo, seller nickname, connection status (connected/disconnected/error with colored indicator), last sync time, "Conectar"/"Desconectar"/"Reconectar" button. OAuth flow launched in popup
  - **Custos Fixos**: configurable defaults — embalagem padrao (BRL), aliquota Simples Nacional (%), outros custos fixos (table with name + value, add/remove)
  - **Alertas**: toggle cards for each alert type with threshold configuration. Types: margem minima (%), estoque minimo (units), pergunta sem resposta (hours), divergencia financeira (%). Enable/disable toggle + threshold input per alert
  - **Aparencia**: theme selector (Claro/Escuro/Sistema), language (future, disabled)
- [ ] Verify in browser using dev-browser skill

### US-016: Notification System
**Description:** As a user, I want real-time notifications for important events so I don't miss sales, questions, or alerts.

**Acceptance Criteria:**
- [ ] Bell icon in header shows unread count (accent badge, max "99+")
- [ ] Click opens slide-over panel from right (320px wide)
- [ ] Notification list: icon (type-specific), title, description, relative time, read/unread indicator (dot)
- [ ] Notification types with icons: Nova venda (cart), Nova pergunta (message), Alerta de estoque (warning), Alerta de margem (trending down), Erro de conexao (alert triangle)
- [ ] Click notification → navigate to relevant screen, mark as read
- [ ] "Marcar todas como lidas" link at top
- [ ] Real-time updates via SignalR (new notifications appear without refresh)
- [ ] Toast notification appears briefly (5s) at top-right for high-priority events (new sale, connection error)
- [ ] Verify in browser using dev-browser skill

### US-017: Global Search
**Description:** As a user, I want to search across orders, products, and customers from the header.

**Acceptance Criteria:**
- [ ] Search input in header, expands on focus
- [ ] Keyboard shortcut: `Ctrl+K` or `Cmd+K` opens search as modal overlay (command palette style)
- [ ] Debounced input (300ms)
- [ ] Results grouped by type: Pedidos, Produtos, Clientes — max 3 results per group
- [ ] Each result: icon (type), primary text (name/ID), secondary text (SKU, date)
- [ ] Arrow keys to navigate, Enter to select, Esc to close
- [ ] Click/Enter → navigate to respective detail screen
- [ ] Verify in browser using dev-browser skill

### US-018: Empty & Error States
**Description:** As a developer, I need consistent empty and error state components for reuse across all screens.

**Acceptance Criteria:**
- [ ] Empty state component: illustration area (optional), title, description, optional CTA button
- [ ] Error state component: error icon, title ("Algo deu errado"), description, "Tentar novamente" button
- [ ] Loading skeleton component: rectangle, circle, and text line variants with shimmer animation
- [ ] 404 page: illustration, "Pagina nao encontrada", link back to dashboard
- [ ] Typecheck/lint passes

### US-019: Responsive Table/Card Pattern
**Description:** As a user, I want tables that gracefully adapt to mobile screens.

**Acceptance Criteria:**
- [ ] On desktop (>=1024px): standard table with columns
- [ ] On tablet (768-1023px): table with horizontal scroll, sticky first column
- [ ] On mobile (<768px): table transforms to card list. Each row becomes a card with key fields displayed as label:value pairs
- [ ] Sort and filter controls remain accessible on all breakpoints
- [ ] Verify in browser using dev-browser skill

---

## Functional Requirements

- FR-1: Application shell must render sidebar, header, and content area as the root layout. Content area scrolls independently
- FR-2: Sidebar navigation items: Dashboard, Produtos, Vendas, Perguntas, Clientes, Financeiro, Estoque, Configuracoes
- FR-3: All monetary values must use monospace font, BRL locale formatting (R$ 1.234,56), and semantic coloring (green positive, red negative)
- FR-4: All percentage changes must show directional arrow (up/down) and be colored (green = good, red = bad — contextual: for costs, up is red)
- FR-5: All tables must support server-side pagination, column sorting, and at minimum a search filter
- FR-6: All forms must show inline validation errors, support keyboard navigation, and warn on unsaved changes
- FR-7: Theme toggle must switch all tokens instantly without page reload
- FR-8: Global search must query orders (by ID), products (by name/SKU), and customers (by name/nickname) simultaneously
- FR-9: Notification panel must update in real-time via SignalR without page refresh
- FR-10: All date/time displays use relative format for recent ("ha 2h") and absolute for older ("15 mar 2026, 14:30")
- FR-11: All export actions (PDF/Excel) must show loading state and download file on completion
- FR-12: Dashboard period selector must persist to session and propagate to all dashboard components
- FR-13: Order detail cost breakdown must show both absolute values and percentage of revenue
- FR-14: Financial charts must support period granularity toggle (dia/semana/mes)
- FR-15: Sidebar must animate collapse/expand (200ms ease transition)
- FR-16: All modals must be closable via Esc key and clicking outside
- FR-17: Toast notifications must stack and auto-dismiss after 5 seconds with manual dismiss option

---

## Non-Goals (Out of Scope)

- Wireframe mockup images or Figma files — this PRD provides textual layout specs, implementation uses code directly
- Animations beyond basic transitions (no complex motion design)
- Offline/PWA support
- Multi-language i18n (Portuguese only for MVP, but use Angular i18n pipes for future readiness)
- Custom illustration creation — use placeholder SVGs or a library like unDraw
- Print-optimized stylesheets
- Accessibility audit (follow baseline WCAG 2.1 AA via component library, but no formal audit)
- Mobile native app design
- Admin super-user screens (multi-tenant management)

---

## Design Considerations

### Layout Structure (Desktop)

```
┌──────────────────────────────────────────────────────────┐
│ [≡]  PeruShopHub        [🔍 Buscar...]  [●] [🔔3] [☀️] [👤] │  ← Header (56px, fixed)
├────────┬─────────────────────────────────────────────────┤
│ Logo   │                                                 │
│        │  Page Title                         [Actions]   │
│ 📊 Dash │                                                 │
│ 📦 Prod │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────┐  │
│ 🛒 Vend │  │  KPI 1  │ │  KPI 2  │ │  KPI 3  │ │KPI 4│  │
│ ❓ Perg │  └─────────┘ └─────────┘ └─────────┘ └─────┘  │
│ 👥 Clie │                                                 │
│ 💰 Fina │  ┌──────────────────┐ ┌──────────────────────┐ │
│ 📋 Esto │  │                  │ │                      │ │
│        │  │   Chart Area     │ │   Chart Area         │ │
│ ⚙️ Conf │  │                  │ │                      │ │
│        │  └──────────────────┘ └──────────────────────┘ │
│        │                                                 │
│ [<]    │  ┌──────────────────────────────────────────┐  │
│        │  │          Table / Content Area             │  │
│        │  └──────────────────────────────────────────┘  │
├────────┴─────────────────────────────────────────────────┤
│ (no footer — infinite scroll or pagination within content) │
└──────────────────────────────────────────────────────────┘
   256px                    remaining width
```

### Layout Structure (Mobile)

```
┌────────────────────────┐
│ [≡]  Logo    [🔔] [👤] │  ← Header (56px)
├────────────────────────┤
│  Page Title            │
│                        │
│  ┌──────────────────┐  │
│  │     KPI Card     │  │
│  └──────────────────┘  │
│  ┌──────────────────┐  │
│  │     KPI Card     │  │
│  └──────────────────┘  │
│  ┌──────────────────┐  │
│  │   Chart (full)   │  │
│  └──────────────────┘  │
│  ┌──────────────────┐  │
│  │   Card List      │  │
│  │   (replaces      │  │
│  │    table)        │  │
│  └──────────────────┘  │
└────────────────────────┘
  Sidebar = overlay drawer
```

### Order Detail Cost Breakdown Visual

```
Receita Bruta: R$ 159,90
┌─────────────────────────────────────────────────────┐
│█████████ Comissao ██████ Frete ████ Produto █ Lucro │
│  16%        25,58    12%  19,19  45%  72,00   22,83 │
└─────────────────────────────────────────────────────┘

┌──────────────────────┬──────────┬────────┬─────────┐
│ Categoria            │ Valor    │ % Rec. │ Fonte   │
├──────────────────────┼──────────┼────────┼─────────┤
│ Comissao ML          │ R$ 25,58 │ 16,0%  │ 🔵 API  │
│ Taxa fixa            │ R$  0,00 │  0,0%  │ 🔵 API  │
│ Frete (vendedor)     │ R$ 19,19 │ 12,0%  │ 🔵 API  │
│ Taxa de pagamento    │ R$  6,30 │  3,9%  │ 🔵 API  │
│ Custo do produto     │ R$ 72,00 │ 45,0%  │ 🟡 Man  │
│ Embalagem            │ R$  2,00 │  1,3%  │ 🟢 Calc │
│ Impostos             │ R$  8,00 │  5,0%  │ 🟢 Calc │
│ Armazenagem          │ R$  0,38 │  0,2%  │ 🔵 API  │
│ Advertising          │ R$  3,62 │  2,3%  │ 🔵 API  │
├──────────────────────┼──────────┼────────┼─────────┤
│ TOTAL CUSTOS         │ R$137,07 │ 85,7%  │         │
│ ══════════════════   │══════════│════════│═════════│
│ LUCRO LIQUIDO        │ R$ 22,83 │ 14,3%  │         │
└──────────────────────┴──────────┴────────┴─────────┘
```

---

## Technical Considerations

- CSS custom properties for theming — avoid Sass variables for tokens that need runtime switching
- Use Angular's `@media (prefers-color-scheme: dark)` for initial theme, then user override
- Chart.js with ng2-charts for all visualizations — configure global defaults (font family, colors, tooltips)
- BRL formatting: Angular `currency` pipe with `'BRL'` and `'symbol'` display, or custom pipe for `R$ 1.234,56`
- Sidebar state managed via Angular service + `localStorage` persistence
- SignalR hub connection established on login, reconnects automatically
- Tables: use virtual scrolling for lists > 100 rows to maintain performance
- Images: lazy load product thumbnails, use ML image CDN URLs with size parameter
- Forms: use Angular Reactive Forms for all forms (not template-driven)
- Route guards: `AuthGuard` for all routes, `RoleGuard` for admin-only settings
- Preload strategy: preload module for the next likely route (dashboard → orders)

---

## Success Metrics

- All screens render correctly on desktop (1280px), tablet (768px), and mobile (375px)
- Theme switch takes < 100ms with no layout shift
- Dashboard loads in < 2 seconds with data (perceived — skeleton shown immediately)
- Order detail cost breakdown is understandable without explanation (validated by user review)
- Global search returns results in < 500ms
- No horizontal scroll on any screen at any breakpoint (except data tables on tablet)
- All interactive elements have visible focus indicators for keyboard navigation

---

## Open Questions

1. Should we support browser push notifications (in addition to in-app SignalR notifications)?
2. Should the dashboard be customizable (drag/rearrange widgets) or fixed layout?
3. For the financial export PDF — should it match the screen layout or use a formal report template?
4. Should there be a "getting started" wizard/checklist for first-time users (connect ML, add first product, configure costs)?
5. PrimeNG vs Angular Material — to be decided at implementation start. PrimeNG offers richer table features (inline editing, column reordering, export) which align well with this data-heavy app.
