# PRD: PeruShopHub MVP — Phases 0.5 through 5

## Introduction

This PRD covers all remaining work to take PeruShopHub from its current foundation (Phase 0 complete) to a closed-beta MVP serving 10-20 Mercado Livre sellers. The product's core differentiator is **real per-sale profitability tracking** — no existing ERP/hub decomposes every cost (commission, fees, real shipping, fulfillment, advertising, taxes, product cost, packaging, coupon absorption) to show true net profit per sale.

**Current state:** 21 entities, 16 controllers, 60+ endpoints, multi-tenancy with EF Core query filters, JWT auth with refresh tokens, Angular 17+ frontend with 16 pages, Redis caching, SignalR real-time notifications, Docker infrastructure.

**Target state:** Production-deployed system connected to Mercado Livre, importing listings, receiving orders via webhook, syncing stock automatically, calculating real profitability per sale with ML Billing API data, managing post-sale communications, with full LGPD compliance and automated backups.

**Target users:** Small sellers on Mercado Livre earning R$10k-50k/month.

---

## Goals

- Establish CI/CD pipeline and test infrastructure to prevent regressions as features grow
- Build a robust inventory system with cost history, channel allocation, and min/max rules
- Implement the financial engine that decomposes all costs per sale and calculates real profit
- Fully integrate with Mercado Livre (OAuth, products, orders, stock, billing, fulfillment)
- Provide post-sale management (questions, messages, claims) in a unified inbox
- Achieve LGPD compliance for Brazilian data protection requirements
- Deploy to production with automated backups, monitoring, and security hardening
- Onboard 10-20 beta sellers with a guided setup experience

---

## Phase 0.5 — DevOps & Quality Foundation

### US-001: GitHub Actions CI Pipeline
**Description:** As a developer, I want automated build and test checks on every PR so regressions are caught before merge.

**Acceptance Criteria:**
- [ ] `.github/workflows/ci.yml` created
- [ ] Backend job: `dotnet restore` → `dotnet build` → `dotnet test` (all test projects)
- [ ] Frontend job: `npm ci` → `ng build --configuration=production` → `ng test --watch=false --browsers=ChromeHeadless`
- [ ] Both jobs run in parallel on `push` to `main` and on all PRs
- [ ] Pipeline fails if any step fails
- [ ] Badge in README shows CI status
- [ ] Branch protection rule on `main`: require CI pass before merge

### US-002: Docker Image Build & Push Pipeline
**Description:** As a developer, I want Docker images built and pushed automatically so deployments are reproducible.

**Acceptance Criteria:**
- [ ] `.github/workflows/docker.yml` created
- [ ] Triggers on push to `main` (not on PRs)
- [ ] Builds `Dockerfile.api`, `Dockerfile.worker`, `Dockerfile.web` images
- [ ] Pushes to GitHub Container Registry (ghcr.io)
- [ ] Tags images with commit SHA and `latest`
- [ ] Uses Docker layer caching for faster builds

### US-003: Test Infrastructure Setup
**Description:** As a developer, I want a test project configured with TestContainers so integration tests run against real PostgreSQL and Redis.

**Acceptance Criteria:**
- [ ] `tests/PeruShopHub.UnitTests/` project created with xUnit + FluentAssertions + Moq
- [ ] `tests/PeruShopHub.IntegrationTests/` project created with xUnit + TestContainers
- [ ] `IntegrationTestBase` class that spins up PostgreSQL 16 + Redis 7 containers
- [ ] `CustomWebApplicationFactory<Program>` that uses TestContainers connection strings
- [ ] Database is migrated automatically in test setup
- [ ] Both test projects added to CI pipeline
- [ ] `dotnet test` runs successfully from repo root

### US-004: Unit Tests — Financial Calculations
**Description:** As a developer, I want unit tests covering all financial calculation logic so cost decomposition is provably correct.

**Acceptance Criteria:**
- [ ] Tests for `CostCalculationService.CalculateOrderCostsAsync` — verifies product_cost, packaging, commission, fixed_fee, tax categories are created correctly
- [ ] Tests for commission resolution: specific match → category fallback → default fallback → hardcoded 13%
- [ ] Tests for fixed fee brackets: ≤R$12.50 (50%), R$12.51-29 (R$6.25), R$29.01-50 (R$6.50), R$50.01-79 (R$6.75), >R$79 (R$0)
- [ ] Tests for weighted average cost calculation on PO receive
- [ ] Tests for cost distribution methods: `by_value` and `by_quantity`
- [ ] Tests for `RecalculateOrderCostsAsync` — preserves manual/API costs, recalculates only "Calculated"
- [ ] Tests for profit calculation: `TotalAmount - Sum(Costs)`
- [ ] Edge cases: zero quantity, zero price, missing variant, missing commission rule
- [ ] All tests pass, minimum 15 test cases

### US-005: Unit Tests — Critical Services
**Description:** As a developer, I want unit tests for ProductService, OrderService, and InventoryService so core business logic is verified.

**Acceptance Criteria:**
- [ ] ProductService tests: create (SKU auto-gen), update, delete, category filtering, search
- [ ] OrderService tests: list with pagination, detail, fulfillment flow, timeline building
- [ ] InventoryService tests: overview calculation, movement creation, stock adjustment
- [ ] CategoryService tests: hierarchy traversal, circular reference detection, slug uniqueness
- [ ] PurchaseOrderService tests: create, receive, cost allocation, status gating
- [ ] Mock dependencies (DbContext, ICacheService) using Moq
- [ ] All tests pass, minimum 30 test cases across services

### US-006: Integration Tests — Controllers
**Description:** As a developer, I want integration tests that hit real endpoints with a real database so API contracts are verified end-to-end.

**Acceptance Criteria:**
- [ ] Auth flow test: register → login → get token → access protected endpoint
- [ ] Product CRUD test: create → read → update → delete → verify 404
- [ ] Order flow test: create order → add costs → fulfill → verify stock decreased
- [ ] Category test: create parent → create child → verify hierarchy
- [ ] Tenant isolation test: create data as Tenant A → verify Tenant B cannot see it
- [ ] Concurrency test: two simultaneous updates → one succeeds, one gets 409
- [ ] Tests use `CustomWebApplicationFactory` with TestContainers
- [ ] All tests pass

### US-007: Angular Test Setup
**Description:** As a developer, I want Angular unit tests for shared components and critical services so frontend regressions are caught.

**Acceptance Criteria:**
- [ ] Karma/Jasmine configured (or migrate to Jest if preferred)
- [ ] Tests for `AuthService`: login, register, token storage, refresh, logout
- [ ] Tests for `DataGridComponent`: pagination, sorting, search, empty state
- [ ] Tests for `FormFieldComponent`: validation display, error messages
- [ ] Tests for currency formatting (BRL, monospace)
- [ ] Tests run in CI with `ng test --watch=false --browsers=ChromeHeadless`
- [ ] Minimum 15 test cases

### US-008: API Rate Limiting per Tenant
**Description:** As a platform operator, I want per-tenant rate limiting on the API so one tenant cannot overload the system for others.

**Acceptance Criteria:**
- [ ] `AspNetCoreRateLimit` NuGet package (or .NET 7+ built-in rate limiter) configured
- [ ] Rate limit policy: 100 requests/minute per tenant (configurable via appsettings)
- [ ] Rate limit headers in response: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- [ ] 429 Too Many Requests response when limit exceeded, with `Retry-After` header
- [ ] Super-admin requests exempt from rate limiting
- [ ] Rate limit counters stored in Redis (shared across instances)
- [ ] Integration test verifying rate limiting works

### US-009: Structured Logging with Serilog
**Description:** As a platform operator, I want structured JSON logging with request correlation so I can debug production issues efficiently.

**Acceptance Criteria:**
- [ ] Serilog NuGet packages added: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- [ ] JSON format for all log output (structured, not plain text)
- [ ] Every request gets a `CorrelationId` (from `X-Correlation-Id` header or auto-generated)
- [ ] Log entries include: `CorrelationId`, `TenantId`, `UserId`, `Endpoint`, `StatusCode`, `ElapsedMs`
- [ ] Request/response logging middleware (configurable: headers only, or include body for debug)
- [ ] Log levels configurable per namespace via appsettings
- [ ] Sensitive data excluded from logs (passwords, tokens, credit cards)
- [ ] Log files rotated daily, max 30 days retention

### US-010: Error Tracking with Sentry
**Description:** As a platform operator, I want unhandled exceptions reported to Sentry so I'm alerted to production errors immediately.

**Acceptance Criteria:**
- [ ] `Sentry.AspNetCore` NuGet package added
- [ ] Sentry DSN configurable via environment variable `SENTRY_DSN`
- [ ] Unhandled exceptions automatically reported with full stack trace
- [ ] Request context included: URL, method, headers (sanitized), user ID, tenant ID
- [ ] `GlobalExceptionFilter` sends exceptions to Sentry before returning error response
- [ ] Source maps uploaded for Angular (frontend errors traceable to TypeScript)
- [ ] Environment tag set (development/staging/production)
- [ ] Sentry disabled when DSN not configured (local dev)

### US-011: Health Check Dashboard
**Description:** As a platform operator, I want a health check endpoint that verifies all dependencies so I can monitor system status.

**Acceptance Criteria:**
- [ ] `/health` endpoint returns aggregated status (Healthy/Degraded/Unhealthy)
- [ ] `/health/ready` checks: PostgreSQL connection, Redis connection, disk space
- [ ] `/health/live` returns 200 if process is running (for k8s liveness probe)
- [ ] Each dependency check has a timeout (5s default)
- [ ] Response includes individual check results with duration
- [ ] Health check UI page at `/health-ui` (using `AspNetCore.HealthChecks.UI` or simple custom page)
- [ ] Health checks run every 30 seconds (configurable)

---

## Phase 1 — Stock & Fulfillment

### US-012: Purchase Order Receive → Stock Adjustment Flow
**Description:** As a seller, I want receiving a purchase order to automatically update my stock and product costs so inventory is always accurate.

**Acceptance Criteria:**
- [ ] `POST /api/purchase-orders/{id}/receive` endpoint triggers full receive flow
- [ ] For each PO item: increment `ProductVariant.Stock` by received quantity
- [ ] Weighted average cost recalculated: `(currentStock * currentCost + newQty * newUnitCost) / totalQty`
- [ ] `StockMovement` created for each item with type "Entrada", linked to PO
- [ ] `ProductCostHistory` record created for each cost change
- [ ] Additional PO costs distributed proportionally before cost calculation
- [ ] `Product.PurchaseCost` updated as weighted average across all active variants
- [ ] PO status changes to "Recebido" — cannot be received twice
- [ ] SignalR notification sent to tenant users
- [ ] Frontend PO detail page shows "Receber" button (only when status is "Rascunho")
- [ ] After receive, stock values update in Inventory page without refresh

### US-013: Stock Allocation by Channel
**Description:** As a seller, I want to allocate my master stock across marketplaces so each channel has a defined quantity available for sale.

**Acceptance Criteria:**
- [ ] New entity `StockAllocation`: ProductVariantId, MarketplaceId (string), AllocatedQuantity, ReservedQuantity
- [ ] Migration adds `stock_allocations` table with unique constraint on (VariantId, MarketplaceId, TenantId)
- [ ] `GET /api/inventory/{productId}/allocations` returns allocations per variant per channel
- [ ] `PUT /api/inventory/{variantId}/allocations` updates allocation for a specific marketplace
- [ ] Validation: sum of allocations across channels ≤ variant total stock
- [ ] "Unallocated" quantity shown = total stock - sum(allocations)
- [ ] Frontend: Inventory detail view shows allocation table per marketplace with editable quantities
- [ ] When stock changes (PO receive, sale, adjustment), allocations are proportionally adjusted if they exceed new total

### US-014: Min/Max Stock Rules and Alerts
**Description:** As a seller, I want configurable minimum and maximum stock levels per product so I'm alerted before running out of stock.

**Acceptance Criteria:**
- [ ] New fields on `Product`: `MinStock` (int?, nullable), `MaxStock` (int?, nullable)
- [ ] Migration adds columns
- [ ] Product create/edit form includes min/max stock fields
- [ ] `StockAlertWorker` (existing, runs every 15min) checks: if any variant stock ≤ product MinStock → create Notification
- [ ] Notification type: "low_stock", message includes product name, current stock, min threshold
- [ ] `GET /api/inventory/alerts` returns products below minimum stock
- [ ] Dashboard "Pending Actions" section shows count of low-stock alerts
- [ ] Frontend: Inventory overview highlights rows where stock ≤ MinStock in red
- [ ] Alert is not duplicated if already exists unread for same product

### US-015: Stock Movement History with Full Traceability
**Description:** As a seller, I want a complete audit trail of every stock change so I can trace exactly who changed what, when, and why.

**Acceptance Criteria:**
- [ ] `StockMovement` entity includes: `CreatedBy` (user name/email), `Reason` (required for adjustments)
- [ ] Every stock change creates a movement: PO receive (Entrada), sale fulfillment (Saida), manual adjustment (Ajuste), reconciliation (Reconciliacao)
- [ ] `GET /api/inventory/movements` supports filters: productId, variantId, type, dateFrom, dateTo, createdBy
- [ ] Response includes pagination and is sorted by date descending
- [ ] Each movement links to source: `PurchaseOrderId` or `OrderId` (clickable in UI)
- [ ] Frontend: Inventory page "Movimentacoes" tab with filters and data grid
- [ ] Export movements to Excel (ClosedXML) — button on movements page
- [ ] Movement reason is required for manual adjustments (frontend validation + backend validation)

### US-016: Internal Stock Reconciliation
**Description:** As a seller, I want to perform physical stock counts and reconcile differences so my system always reflects reality.

**Acceptance Criteria:**
- [ ] `POST /api/inventory/reconciliation` accepts list of `{ variantId, countedQuantity }`
- [ ] For each item: compares `countedQuantity` vs `variant.Stock`
- [ ] If different: creates `StockMovement` type "Ajuste" with reason "Reconciliacao fisica" and difference quantity
- [ ] Updates `variant.Stock` to counted quantity
- [ ] Returns reconciliation report: items checked, items with discrepancies, total difference
- [ ] Frontend: "Reconciliacao" page/dialog with product list, input fields for counted qty, current qty shown
- [ ] Differences highlighted (red if counted < system, yellow if counted > system)
- [ ] Reconciliation creates a single batch record linking all movements

### US-017: Product Cost History with Effective Dates
**Description:** As a seller, I want to see how my product costs have changed over time so I can track supplier price trends and audit profitability.

**Acceptance Criteria:**
- [ ] `ProductCostHistory` already exists — ensure `CreatedAt` serves as effective date
- [ ] `GET /api/products/{id}/cost-history` returns history sorted by date descending
- [ ] Response includes: date, previousCost, newCost, quantity, unitCostPaid, purchaseOrderId, reason
- [ ] Chart on Product Detail page: cost over time (line chart, x=date, y=cost)
- [ ] When recalculating old order costs, use the cost that was effective at the time of the order (not current cost)
- [ ] Frontend: Product detail "Historico de Custos" section with table and line chart

### US-018: Packaging and Storage Cost per Product
**Description:** As a seller, I want to set packaging cost per product and estimated daily storage cost so these are included in profitability calculations.

**Acceptance Criteria:**
- [ ] `Product.PackagingCost` already exists — verify it's used in cost calculations
- [ ] New field: `Product.StorageCostDaily` (decimal?, nullable) — estimated daily storage cost
- [ ] Migration adds `StorageCostDaily` column
- [ ] Product form includes both fields with labels "Custo de embalagem (R$)" and "Custo de armazenagem diario (R$)"
- [ ] `CostCalculationService` includes packaging cost in order cost decomposition (already does — verify)
- [ ] Storage cost calculation: `StorageCostDaily * averageDaysInStorage` (configurable default: 30 days)
- [ ] Storage cost added as `OrderCost` category `storage_daily` if product has `StorageCostDaily` set

---

## Phase 2 — Financial Engine

### US-019: Commission Engine — Category, Reputation, Listing Type
**Description:** As a seller, I want commission rates to vary by ML category, listing type, and seller reputation so profitability calculations match what ML actually charges.

**Acceptance Criteria:**
- [ ] `CommissionRule` entity (exists) supports: MarketplaceId, CategoryPattern, ListingType, Rate, IsDefault
- [ ] Seed data: ML default commission rules for major categories (Eletronicos 16%/19%, Roupas 14%/17%, Casa 13%/16%, etc.)
- [ ] Resolution algorithm: exact match (category+listing) → category only → marketplace default → hardcoded 13%
- [ ] `GET /api/settings/commission-rules` lists all rules (already exists)
- [ ] `POST/PUT/DELETE /api/settings/commission-rules` for CRUD (already exists)
- [ ] Frontend Settings > Comissoes page shows rules table with add/edit/delete
- [ ] When ML Billing API provides actual commission, it overrides calculated value (Source = "API" > "Calculated")
- [ ] Unit test: resolution algorithm with all fallback levels

### US-020: Tax Calculation Engine
**Description:** As a seller, I want taxes calculated automatically based on my tax regime so I see the real tax impact per sale.

**Acceptance Criteria:**
- [ ] New entity `TaxProfile`: TenantId, TaxRegime (enum: SimplesNacional, LucroPresumido, MEI), AliquotPercentage, State
- [ ] Migration adds `tax_profiles` table
- [ ] `GET/PUT /api/settings/tax-profile` to read/update tenant's tax profile
- [ ] Default: Simples Nacional, 6% (DAS approximate)
- [ ] `CostCalculationService` uses tenant's tax profile rate instead of hardcoded `CostSettings:TaxRate`
- [ ] For Simples Nacional: tax = totalAmount * aliquot (simplified; full Simples table is Phase 7)
- [ ] Frontend Settings > Fiscal page with tax regime selector and aliquot input
- [ ] Tax cost appears as `OrderCost` category `tax` with Source "Calculated"

### US-021: Payment Fee Calculation
**Description:** As a seller, I want payment processing fees calculated per order so I see the real payment cost (which varies by installment count).

**Acceptance Criteria:**
- [ ] New field on `Order`: `InstallmentCount` (int, default 1)
- [ ] Payment fee rules configurable: `PaymentFeeRule` entity with InstallmentRange (min/max) and FeePercentage
- [ ] Seed data: ML default payment fees (1x: 4.99%, 2-3x: absorbed by buyer typically, 4-6x: extra %, etc.)
- [ ] `CostCalculationService` calculates payment fee based on installment count and total amount
- [ ] Payment fee added as `OrderCost` category `payment_fee` with Source "Calculated"
- [ ] When ML Billing API provides actual payment fee, it replaces calculated value
- [ ] Frontend Settings: payment fee rules table (configurable)
- [ ] Order detail shows installment count and payment fee in cost breakdown

### US-022: Complete Cost Composition Service
**Description:** As a seller, I want every sale automatically decomposed into all cost categories so I see exactly where my money goes.

**Acceptance Criteria:**
- [ ] `CostCalculationService.CalculateOrderCostsAsync` produces all categories:
  - `product_cost` — weighted average cost × quantity (per item)
  - `packaging` — product packaging cost × quantity
  - `marketplace_commission` — resolved via CommissionRule
  - `fixed_fee` — ML fixed fee table (already implemented)
  - `tax` — via TaxProfile
  - `payment_fee` — via PaymentFeeRule
  - `shipping_seller` — from ML API or manual
  - `fulfillment_fee` — from ML API or manual
  - `storage_daily` — from product StorageCostDaily
  - `advertising` — manual attribution
- [ ] Each cost has correct Source: "Calculated", "API", or "Manual"
- [ ] `Order.Profit = TotalAmount - Sum(all costs)`
- [ ] Frontend Order Detail: cost breakdown table shows all categories with values and percentages
- [ ] Cost breakdown pie chart on order detail
- [ ] Costs with value 0 are not shown in UI (but stored)

### US-023: Materialized View — SKU Profitability
**Description:** As a seller, I want a pre-computed profitability summary per SKU so the finance dashboard loads instantly even with thousands of orders.

**Acceptance Criteria:**
- [ ] SQL materialized view `sku_profitability`: SKU, total_orders, total_units, total_revenue, total_costs (by category), total_profit, avg_margin
- [ ] EF Core migration creates the materialized view
- [ ] `REFRESH MATERIALIZED VIEW CONCURRENTLY` scheduled in background worker (every 1 hour)
- [ ] `GET /api/finance/sku-profitability` reads from materialized view (fast query)
- [ ] Supports filters: dateFrom, dateTo, minMargin, maxMargin, search (SKU/name)
- [ ] Supports sorting by any column
- [ ] Frontend Finance page "Lucratividade por SKU" tab uses this endpoint
- [ ] Manual refresh button for admins

### US-024: PDF Export with QuestPDF
**Description:** As a seller, I want to export financial reports as professional PDF documents so I can share them with my accountant or partners.

**Acceptance Criteria:**
- [ ] QuestPDF NuGet package added
- [ ] `IReportService` interface with methods: `GenerateProfitabilityReport`, `GenerateOrderReport`, `GenerateInventoryReport`
- [ ] `ReportService` implementation generates styled PDFs with:
  - Company header (tenant name, logo if available)
  - Date range
  - Summary KPIs (revenue, costs, profit, margin)
  - Detail table (per SKU or per order depending on report type)
  - Footer with generation timestamp
- [ ] `GET /api/reports/profitability/pdf?dateFrom=&dateTo=` returns PDF file
- [ ] `GET /api/reports/orders/pdf?dateFrom=&dateTo=` returns PDF file
- [ ] Frontend: "Exportar PDF" button on Finance page and Sales page
- [ ] PDF uses BRL formatting (R$ 1.234,56), Inter font, company colors

### US-025: Excel Export with ClosedXML
**Description:** As a seller, I want to export data as Excel spreadsheets so I can manipulate data in my own tools.

**Acceptance Criteria:**
- [ ] ClosedXML NuGet package added
- [ ] `IExportService` interface with methods: `ExportOrdersToExcel`, `ExportInventoryToExcel`, `ExportProfitabilityToExcel`
- [ ] `ExportService` implementation generates .xlsx files with:
  - Formatted headers with filters enabled
  - Number formatting (currency in BRL, percentages)
  - Column auto-width
  - Sheet name matching report type
- [ ] `GET /api/reports/profitability/excel?dateFrom=&dateTo=` returns .xlsx
- [ ] `GET /api/reports/orders/excel?dateFrom=&dateTo=` returns .xlsx
- [ ] `GET /api/reports/inventory/excel` returns .xlsx
- [ ] Frontend: "Exportar Excel" button alongside PDF button
- [ ] Files downloaded with descriptive filename: `profitabilidade_2026-03-01_2026-03-31.xlsx`

### US-026: ABC Curve Analysis with Real Data
**Description:** As a seller, I want products classified into A/B/C tiers based on real revenue contribution so I know which SKUs to prioritize.

**Acceptance Criteria:**
- [ ] `GET /api/finance/abc-curve?dateFrom=&dateTo=` calculates ABC from real order data
- [ ] Classification: A = top SKUs contributing 80% of revenue, B = next 15%, C = remaining 5%
- [ ] Response: list of SKUs with revenue, percentage of total, cumulative percentage, class (A/B/C), profit margin
- [ ] Recalculated on-demand (not cached — uses materialized view data)
- [ ] Frontend Finance page "Curva ABC" tab: table + Pareto chart (bar = revenue per SKU, line = cumulative %)
- [ ] Color coding: A = green, B = yellow, C = red
- [ ] Products page shows ABC class badge on each product card/row

### US-027: Financial Dashboard with Real Data
**Description:** As a seller, I want the dashboard showing real financial KPIs calculated from actual orders so I can monitor my business health at a glance.

**Acceptance Criteria:**
- [ ] Dashboard KPIs calculated from real data (not seed data):
  - Total revenue (sum of order amounts in period)
  - Total costs (sum of all OrderCosts in period)
  - Total profit (revenue - costs)
  - Average margin (profit / revenue × 100)
  - Order count
  - Average ticket (revenue / orders)
- [ ] Period selector: today, last 7 days, last 30 days, this month, custom range
- [ ] Revenue vs Profit line chart (daily/weekly data points)
- [ ] Cost breakdown pie chart (by category: commission, shipping, tax, product cost, etc.)
- [ ] Top 5 most profitable products (by total profit in period)
- [ ] Top 5 least profitable products (lowest margin in period)
- [ ] Comparison with previous period (e.g., "↑ 12% vs last month")
- [ ] Frontend: dashboard page fully wired to real endpoints

### US-028: Automated Reports by Email
**Description:** As a seller, I want weekly and monthly profitability digest emails so I stay informed without logging in.

**Acceptance Criteria:**
- [ ] New entity `ReportSchedule`: TenantId, Frequency (weekly/monthly), Recipients (email list), IsActive, LastSentAt
- [ ] Migration adds `report_schedules` table
- [ ] `ReportEmailWorker` background service: runs daily, checks if any scheduled reports are due
- [ ] Weekly report: sent Monday morning, covers previous Mon-Sun
- [ ] Monthly report: sent 1st of month, covers previous month
- [ ] Email content: summary KPIs (revenue, profit, margin, top/bottom products), link to full dashboard
- [ ] Uses email provider from US-055 (SendGrid/Resend)
- [ ] Frontend Settings > Relatorios: configure frequency, recipients, enable/disable
- [ ] Unsubscribe link in email footer

### US-029: Audit Trail / Activity Log
**Description:** As a seller, I want to see who changed prices, stock, and costs so I have accountability for all financial data changes.

**Acceptance Criteria:**
- [ ] New entity `AuditLog`: TenantId, UserId, UserName, Action (string), EntityType, EntityId, OldValue (JSON), NewValue (JSON), CreatedAt
- [ ] Migration adds `audit_logs` table with index on (TenantId, EntityType, CreatedAt)
- [ ] Audit events captured for: product price/cost changes, stock adjustments, commission rule changes, order cost manual edits, PO receives
- [ ] `IAuditService.LogAsync(action, entityType, entityId, oldValue, newValue)` called from services
- [ ] `GET /api/audit-log?entityType=&entityId=&dateFrom=&dateTo=&userId=` with pagination
- [ ] Frontend: Settings > Log de Atividades page with filters and data grid
- [ ] Each entry shows: timestamp, user, action description, old→new values
- [ ] Accessible only to Owner and Admin roles

### US-030: Target-Margin Pricing Rules
**Description:** As a seller, I want to set a desired profit margin and have the system calculate the required selling price considering all marketplace fees so I price products correctly.

**Acceptance Criteria:**
- [ ] New endpoint: `POST /api/pricing/calculate` accepts: productId, targetMarginPercent, marketplaceId, listingType
- [ ] Calculation: works backwards from target margin to determine required price
  - `Price = ProductCost / (1 - targetMargin - commissionRate - taxRate - paymentFeeRate - fixedFeePercent)`
  - Accounts for: commission, tax, payment fee, fixed fee, packaging, estimated shipping
- [ ] Returns: suggestedPrice, breakdown of each cost at that price, actual resulting margin
- [ ] `GET /api/pricing/rules` returns saved pricing rules per product per marketplace
- [ ] `POST /api/pricing/rules` saves a rule: productId, marketplaceId, targetMarginPercent
- [ ] Frontend: Product detail > "Precificacao" tab with margin input, marketplace selector, calculated price display
- [ ] Visual breakdown: bar chart showing price composition (product cost, commission, tax, fees, profit)

### US-031: Price Calculator & Scenario Simulator
**Description:** As a seller, I want to simulate "what if" scenarios (commission changes, cost increases, shipping changes) so I can plan ahead.

**Acceptance Criteria:**
- [ ] `POST /api/pricing/simulate` accepts: productId, overrides (commissionRate, taxRate, productCost, shippingCost, etc.)
- [ ] Returns: profitability at current price with overridden costs, comparison with current values
- [ ] Supports batch simulation: multiple products at once
- [ ] Frontend: "Simulador" page/dialog accessible from Products and Finance pages
- [ ] Input fields for each cost variable with current value pre-filled and editable
- [ ] Real-time recalculation as user changes inputs (debounced 300ms)
- [ ] Results show: current margin vs simulated margin, difference highlighted (green if better, red if worse)
- [ ] "What if commission increases 2%?" — slider for quick adjustments

### US-032: Configurable Margin Alerts
**Description:** As a seller, I want to be notified when a product's margin drops below my threshold so I can take action before losing money.

**Acceptance Criteria:**
- [ ] New entity `AlertRule`: TenantId, Type (enum: MarginBelow, CostIncrease, StockLow), Threshold (decimal), IsActive, ProductId (nullable — null means all products)
- [ ] Migration adds `alert_rules` table
- [ ] `AlertWorker` background service: runs every hour, evaluates rules against current data
- [ ] MarginBelow: checks average margin per product over last 30 days, creates notification if below threshold
- [ ] CostIncrease: compares current product cost vs cost 30 days ago, alerts if increase > threshold%
- [ ] `GET/POST/PUT/DELETE /api/settings/alert-rules` CRUD
- [ ] Frontend Settings > Alertas: rule list with type, threshold, product filter, active toggle
- [ ] Notifications appear in notification bell + optional email (if email configured)

---

## Phase 3 — Mercado Livre Integration

### US-033: MercadoLivreAdapter — Core Implementation
**Description:** As a developer, I want the MercadoLivreAdapter implementing IMarketplaceAdapter registered via DI keyed services so the system can communicate with ML's API.

**Acceptance Criteria:**
- [ ] `MercadoLivreAdapter` class implementing `IMarketplaceAdapter` in Infrastructure project
- [ ] Registered as `services.AddKeyedScoped<IMarketplaceAdapter, MercadoLivreAdapter>("mercadolivre")`
- [ ] Uses `HttpClientFactory` with named client "MercadoLivre"
- [ ] Base URL: `https://api.mercadolibre.com`
- [ ] All methods use `CancellationToken`
- [ ] JSON serialization with `System.Text.Json` and camelCase naming policy
- [ ] Error handling: deserializes ML error responses (`{ message, error, status, cause }`)
- [ ] Logging: all API calls logged with correlation ID, endpoint, status code, elapsed time

### US-034: OAuth 2.0 Flow with PKCE
**Description:** As a seller, I want to connect my Mercado Livre account via OAuth so the system can access my listings and orders securely.

**Acceptance Criteria:**
- [ ] `GET /api/integrations/mercadolivre/auth-url` generates authorization URL with PKCE (code_challenge, state)
- [ ] State parameter stored in Redis (5 min TTL) to prevent CSRF
- [ ] `GET /api/integrations/mercadolivre/callback?code=&state=` handles redirect
- [ ] Callback exchanges code for tokens via `POST https://api.mercadolibre.com/oauth/token`
- [ ] Tokens stored in `MarketplaceConnection` entity (already exists)
- [ ] `MarketplaceConnection.AccessToken` and `RefreshToken` encrypted at rest using `IDataProtectionProvider` (AES-256)
- [ ] Connection status set to "Active" with `TokenExpiresAt`
- [ ] ML user_id stored for API calls
- [ ] Frontend Settings > Integracoes: "Conectar Mercado Livre" button triggers OAuth flow
- [ ] After successful connection, page shows: connected status, ML username, expiration

### US-035: Token Encryption at Rest
**Description:** As a platform operator, I want OAuth tokens encrypted in the database so a database breach doesn't expose seller credentials.

**Acceptance Criteria:**
- [ ] `ITokenEncryptionService` interface: `Encrypt(plainText)`, `Decrypt(cipherText)`
- [ ] Implementation uses `IDataProtectionProvider` with purpose string "MarketplaceTokens"
- [ ] Data protection keys stored in a dedicated directory (configurable, not in-memory)
- [ ] `MarketplaceConnection` stores encrypted tokens, decrypts on read
- [ ] Encryption key rotation supported (old keys can still decrypt)
- [ ] Unit test: encrypt → decrypt roundtrip works
- [ ] Integration test: save connection with token → reload → token is correct

### US-036: Token Renewal Background Worker
**Description:** As a seller, I want my ML tokens renewed automatically before they expire so the integration never breaks silently.

**Acceptance Criteria:**
- [ ] `TokenRenewalWorker` background service runs every 15 minutes
- [ ] Queries all `MarketplaceConnection` where `TokenExpiresAt < now + 30 minutes` and status is "Active"
- [ ] For each: calls `POST /oauth/token` with `grant_type=refresh_token`
- [ ] On success: updates tokens, resets `RefreshErrorCount` to 0
- [ ] On failure: increments `RefreshErrorCount`
- [ ] Circuit breaker: after 3 consecutive failures, sets status to "Error", creates notification for user
- [ ] Notification message: "Sua conexao com o Mercado Livre precisa ser reconectada"
- [ ] Logs all renewal attempts (success and failure) with tenant context

### US-037: HTTP Client — Circuit Breaker & Rate Limiter
**Description:** As a developer, I want Polly circuit breaker and rate limiting on ML API calls so the system handles ML outages gracefully and respects rate limits.

**Acceptance Criteria:**
- [ ] Polly NuGet packages added: `Microsoft.Extensions.Http.Polly` or `Polly.Extensions.Http`
- [ ] HttpClient policy chain: Rate Limiter → Retry → Circuit Breaker
- [ ] Rate limiter: max 300 requests/minute (18,000/hour ÷ 60), uses sliding window
- [ ] Retry policy: 3 retries with exponential backoff (1s, 2s, 4s), only for transient errors (5xx, timeout)
- [ ] Circuit breaker: opens after 5 consecutive failures, stays open 30 seconds, then half-open
- [ ] When circuit is open: immediately returns failure (no API call), logs warning
- [ ] Rate limit state stored in-memory (per instance — acceptable for single-instance MVP)
- [ ] Integration test: verify circuit breaker opens after failures

### US-038: ML Integration Settings Page
**Description:** As a seller, I want a settings page showing my ML connection status and allowing me to connect/disconnect so I manage my integration easily.

**Acceptance Criteria:**
- [ ] Frontend page: Settings > Integracoes > Mercado Livre
- [ ] Shows connection status: Ativo (green badge), Erro (red badge), Desconectado (gray badge)
- [ ] When connected: ML seller name, user ID, token expires at, last sync time
- [ ] "Conectar" button (when disconnected) → redirects to OAuth flow
- [ ] "Reconectar" button (when error) → redirects to OAuth flow
- [ ] "Desconectar" button (when connected) → confirmation dialog → revokes tokens, sets status to "Disconnected"
- [ ] "Sincronizar Agora" button → triggers manual sync of products/orders
- [ ] Connection health indicator: last API call status, error count

### US-039: Import ML Listings
**Description:** As a seller, I want to import my existing Mercado Livre listings so the system knows about all my active products.

**Acceptance Criteria:**
- [ ] `GET /users/{userId}/items/search` with scroll pagination to fetch all seller items
- [ ] For each ML item: fetch full details via `GET /items/{itemId}`
- [ ] Map ML item → internal representation: title, price, available_quantity, category_id, pictures, variations
- [ ] `POST /api/integrations/mercadolivre/import` triggers import job (enqueued, not synchronous)
- [ ] Import worker processes items in batches of 50 (respecting rate limit)
- [ ] For each item: check if already linked (by `ExternalId`) → update or create
- [ ] New items create `Product` + `ProductVariant`(s) with `ExternalId` = ML item ID
- [ ] Existing items update: price, stock, title (if changed on ML)
- [ ] Import progress tracked: total items, processed, errors
- [ ] Frontend: Import button on Products page, progress indicator, import log/results
- [ ] After import, products page shows ML-linked products with ML icon badge

### US-040: Product-Listing Mapping (Link/Unlink)
**Description:** As a seller, I want to manually link internal products to ML listings so I can map products that weren't auto-matched during import.

**Acceptance Criteria:**
- [ ] Products have `ExternalId` field (already on `Order`, add to `Product` if not present)
- [ ] `PUT /api/products/{id}/link-marketplace` body: `{ marketplaceId: "mercadolivre", externalId: "MLB1234" }`
- [ ] `DELETE /api/products/{id}/link-marketplace/{marketplaceId}` unlinks
- [ ] `GET /api/integrations/mercadolivre/unlinked-items` returns ML items not linked to any internal product
- [ ] Frontend: Products page shows link status icon (linked/unlinked) per product
- [ ] "Vincular" action on product: dropdown showing unlinked ML items with search
- [ ] "Desvincular" action: confirmation dialog → removes link
- [ ] Linked products show ML item ID and direct link to ML listing

### US-041: Variation Sync ML → Internal
**Description:** As a seller, I want ML product variations (size, color) mapped to internal variants so stock and costs track per variation.

**Acceptance Criteria:**
- [ ] ML item variations parsed from `GET /items/{id}` response `variations` array
- [ ] Each ML variation maps to a `ProductVariant`: ML variation ID → `ExternalId`, attribute combinations → variant name/SKU
- [ ] ML variation `available_quantity` → variant `Stock`
- [ ] ML variation `picture_ids` → linked to internal photos
- [ ] Sync creates new variants for new ML variations, updates existing ones
- [ ] Deleted ML variations marked as inactive (not deleted — may have order history)
- [ ] Frontend: Product detail shows ML variation mapping in variants table

### US-042: Photo Sync ML → Internal
**Description:** As a seller, I want ML product photos imported and stored locally so they display in the system even if ML is down.

**Acceptance Criteria:**
- [ ] ML photo URLs extracted from item `pictures` array
- [ ] Photos downloaded and stored via `IFileStorageService` (local disk for MVP)
- [ ] `FileUpload` record created for each photo, linked to Product
- [ ] Original ML URL preserved in `FileUpload.ExternalUrl` (new field if needed)
- [ ] Photos displayed in Product detail gallery
- [ ] Sync updates: new photos added, removed photos marked inactive
- [ ] Respects rate limit — photos downloaded in background, not blocking import

### US-043: Periodic Product Sync Worker
**Description:** As a seller, I want products synced periodically with ML so changes made on ML (price, stock, new items) are reflected in the system.

**Acceptance Criteria:**
- [ ] `ProductSyncWorker` background service: runs every 2 hours (configurable)
- [ ] Uses `GET /users/{userId}/items/search` with `orders=last_updated_desc` to find recently changed items
- [ ] Fetches details for items updated since last sync
- [ ] Updates: price, stock, title, status, variations
- [ ] Detects new items not yet imported → creates them
- [ ] Detects items paused/closed on ML → updates internal status
- [ ] Last sync timestamp stored in `MarketplaceConnection.LastSyncAt`
- [ ] Sync results logged: items checked, updated, created, errors
- [ ] Notification on errors: "Sync com ML encontrou X erros"

### US-044: Listings Page (Frontend)
**Description:** As a seller, I want a dedicated "Anuncios" page showing all my ML listings with sync status so I manage my marketplace presence from one place.

**Acceptance Criteria:**
- [ ] New page `/anuncios` with route guard (auth + tenant)
- [ ] Data grid showing: ML item photo, title, ML ID, price, stock, status (active/paused/closed), sync status, internal product link
- [ ] Sync status: "Sincronizado" (green), "Desatualizado" (yellow), "Erro" (red), "Nao vinculado" (gray)
- [ ] Filters: status, sync status, search (title/ML ID)
- [ ] Actions per row: "Ver no ML" (external link), "Vincular" (if unlinked), "Sincronizar" (manual single-item sync)
- [ ] Bulk actions: sync selected, sync all
- [ ] Sidebar navigation updated with "Anuncios" item (with ML icon)

### US-045: Webhook Receiver — orders_v2
**Description:** As a seller, I want ML order webhooks received and processed so new sales appear automatically in the system.

**Acceptance Criteria:**
- [ ] `POST /api/webhooks/mercadolivre` endpoint — no authentication (ML webhooks are unauthenticated)
- [ ] Validates webhook structure: `topic`, `resource`, `user_id`, `application_id`
- [ ] IP validation: only accept from ML IPs (54.88.218.97, 18.215.140.160, etc.) — configurable list
- [ ] Response time < 500ms — validate and enqueue only, no processing
- [ ] Webhook payload enqueued to Redis list `ml:webhooks:{topic}`
- [ ] Returns 200 OK immediately after enqueue
- [ ] For `orders_v2` topic: enqueues order resource URL
- [ ] Duplicate detection: check if webhook ID already processed (Redis SET with 24h TTL)
- [ ] Logging: webhook received, topic, resource, processing time

### US-046: Webhook Queue Worker
**Description:** As a developer, I want a background worker processing ML webhook queue reliably so orders are processed even during high volume.

**Acceptance Criteria:**
- [ ] `WebhookProcessingWorker` background service polls Redis `ml:webhooks:*` lists
- [ ] Processes one webhook at a time per topic (ordered processing)
- [ ] For `orders_v2`: fetches order details via `GET /orders/{orderId}`
- [ ] Maps ML order → internal `Order`: buyer info, items, amounts, shipping, payment
- [ ] Creates or updates `Order` entity (idempotent — same external ID doesn't duplicate)
- [ ] Creates `OrderItem`s linked to internal products (by ML item ID → Product.ExternalId)
- [ ] Triggers cost calculation via `CostCalculationService`
- [ ] Triggers stock decrement for fulfilled orders
- [ ] Sends SignalR notification: "Nova venda: Pedido #XXX - R$ YYY"
- [ ] Dead letter queue: after 3 failed processing attempts, moves to `ml:webhooks:dead`
- [ ] Logging: processing start/end, success/failure, elapsed time

### US-047: Historical Order Sync
**Description:** As a seller, I want to import my past ML orders so the system has historical data for profitability analysis from day one.

**Acceptance Criteria:**
- [ ] `POST /api/integrations/mercadolivre/sync-orders` triggers historical sync
- [ ] Uses `GET /orders/search?seller={userId}&order.date_created.from=&order.date_created.to=` with scroll pagination
- [ ] Configurable date range (default: last 90 days)
- [ ] Processes orders in batches of 50
- [ ] Same mapping logic as webhook processing (reuses code)
- [ ] Skips orders already in system (by ExternalOrderId)
- [ ] Progress tracking: total found, processed, skipped, errors
- [ ] Frontend: button on Sales page "Importar Historico ML", progress indicator
- [ ] After sync, dashboard and finance pages reflect historical data

### US-048: ML Order → Internal Order Mapping
**Description:** As a developer, I want a reliable mapping layer between ML order format and internal Order entity so data is accurately translated.

**Acceptance Criteria:**
- [ ] Mapping handles all ML order fields:
  - `id` → `Order.ExternalOrderId`
  - `date_created` → `Order.OrderDate`
  - `buyer.id`, `buyer.nickname`, `buyer.email` → `Order.BuyerName`, creates/updates `Customer`
  - `order_items[].item.id` → links to Product by ExternalId
  - `order_items[].quantity`, `unit_price`, `full_unit_price` → `OrderItem` fields
  - `total_amount` → `Order.TotalAmount`
  - `payments[0].installments` → `Order.InstallmentCount`
  - `payments[0].status` → `Order.PaymentStatus`
  - `shipping.id` → stored for tracking lookup
  - `status` → `Order.Status` (mapped: paid, shipped, delivered, cancelled)
- [ ] Handles edge cases: cancelled orders, partial shipments, multiple payments
- [ ] Unit tests for mapping with sample ML API responses (at least 5 scenarios)

### US-049: Additional Webhooks — items, shipments, payments
**Description:** As a seller, I want the system to react to item changes, shipment updates, and payment confirmations from ML so data stays in sync.

**Acceptance Criteria:**
- [ ] Webhook receiver handles topics: `items`, `shipments`, `payments` (in addition to `orders_v2`)
- [ ] `items` webhook: fetches item, updates internal product (price, stock, status changes)
- [ ] `shipments` webhook: fetches shipping details via `GET /shipments/{id}`, updates order shipping status and tracking
- [ ] `payments` webhook: fetches payment via `GET /collections/{id}`, updates order payment status
- [ ] Each topic has its own Redis queue for independent processing
- [ ] Frontend: Order detail shows real-time shipping tracking updates
- [ ] Order timeline updated with shipping events (label created, picked up, in transit, delivered)

### US-050: Billing API Integration — Real Costs
**Description:** As a seller, I want actual ML charges fetched from the Billing API so my profitability shows real costs, not estimates.

**Acceptance Criteria:**
- [ ] `GET /billing/integration/group/ML/order/details?order_id={orderId}` called after order is processed
- [ ] Billing response parsed: actual commission, fixed fee, shipping cost, payment fee, fulfillment fee
- [ ] For each cost category: create/update `OrderCost` with `Source = "API"`
- [ ] API costs override "Calculated" costs for the same category
- [ ] Billing data may not be available immediately — retry logic: check at order creation, then 1h, 6h, 24h
- [ ] `BillingReconciliationWorker`: periodically checks orders missing billing data (up to 7 days old)
- [ ] Frontend: Cost breakdown shows "Fonte: API" or "Fonte: Calculado" badge per cost line
- [ ] Dashboard and finance reports use API costs when available, calculated as fallback

### US-051: Real Shipping Cost from ML
**Description:** As a seller, I want the actual shipping cost fetched from ML's shipping API so I see the real freight impact on profitability.

**Acceptance Criteria:**
- [ ] `GET /shipments/{shipmentId}` fetches shipping details including `cost_components`
- [ ] Shipping cost to seller extracted and stored as `OrderCost` category `shipping_seller`, Source "API"
- [ ] Handles both "Mercado Envios" (ML-managed shipping) and "envio proprio"
- [ ] For free shipping (products > R$79): captures the seller-absorbed cost
- [ ] Shipping tracking URL stored on Order for frontend display
- [ ] Frontend Order Detail: shipping section shows carrier, tracking number, cost, status

### US-052: Fulfillment Fee Lookup (ML Full)
**Description:** As a seller, I want fulfillment fees from ML Full captured per order so I see the true cost of using ML's warehouse.

**Acceptance Criteria:**
- [ ] Fulfillment fee extracted from billing API response
- [ ] Stored as `OrderCost` category `fulfillment_fee`, Source "API"
- [ ] If not available in billing: estimated from product dimensions using ML's fee table
- [ ] `GET /inventories/{inventoryId}/stock/fulfillment` for checking Full stock levels
- [ ] Frontend Order Detail: fulfillment fee shown in cost breakdown with "Full" badge
- [ ] Products page: indicator showing which products use Full vs own shipping

### US-053: Auto Stock Update on ML After Internal Changes
**Description:** As a seller, I want stock changes in the system automatically pushed to ML so my listings always show correct availability.

**Acceptance Criteria:**
- [ ] When `ProductVariant.Stock` changes (PO receive, manual adjustment, reconciliation):
  - If product is linked to ML item → enqueue stock update
- [ ] `StockSyncWorker` processes queue: `PUT /items/{itemId}` with `{ available_quantity: X }`
- [ ] For items with variations: `PUT /items/{itemId}/variations/{variationId}` with `{ available_quantity: X }`
- [ ] Respects rate limit (queued, not synchronous)
- [ ] Uses allocated quantity (from `StockAllocation`) not total stock
- [ ] Retry on failure (3 attempts with backoff)
- [ ] Logging: stock sync attempts with old/new quantity, success/failure
- [ ] Frontend: stock sync status indicator per product (synced, pending, error)

### US-054: Items Webhook — External Stock Changes
**Description:** As a seller, I want the system to detect when stock changes on ML (from ML app, another tool, or ML Full operations) so local records stay accurate.

**Acceptance Criteria:**
- [ ] `items` webhook triggers item re-fetch when `available_quantity` changes
- [ ] Compares ML quantity vs local allocated quantity
- [ ] If ML quantity < local: possible external sale or ML adjustment → log discrepancy, create `StockMovement` type "Ajuste" with reason "Sync ML"
- [ ] If ML quantity > local: ML Full received stock or manual ML change → update local
- [ ] Does NOT trigger a stock push back to ML (would create infinite loop)
- [ ] Discrepancy alert sent as notification if difference > configurable threshold (default: 5 units)

### US-055: Periodic Stock Reconciliation Worker
**Description:** As a seller, I want periodic comparison between my local stock and ML stock so discrepancies are caught and reported.

**Acceptance Criteria:**
- [ ] `StockReconciliationWorker` runs every 6 hours (configurable)
- [ ] For each ML-linked product: `GET /items/{itemId}` → compare `available_quantity` with local allocation
- [ ] Generates reconciliation report: total items checked, matches, discrepancies (with details)
- [ ] Small discrepancies (≤ threshold): auto-corrects local stock, creates adjustment StockMovement
- [ ] Large discrepancies (> threshold): creates notification for manual review, does NOT auto-correct
- [ ] Report saved: `GET /api/inventory/reconciliation-reports` with date filter
- [ ] Frontend: Reconciliation history page with reports list and detail view

### US-056: ML Fulfillment Stock Query
**Description:** As a seller, I want to see my stock levels at ML's warehouse (Full) separately from my own stock so I have complete visibility.

**Acceptance Criteria:**
- [ ] `GET /inventories/{inventoryId}/stock/fulfillment` called for Full products
- [ ] Response shows: available, not_available, damaged, lost, in_transfer quantities
- [ ] Stored/cached (Redis, 15 min TTL) per product
- [ ] `GET /api/inventory/{productId}/fulfillment-stock` returns Full stock breakdown
- [ ] Frontend: Inventory detail shows "Estoque Full" section with status breakdown for Full products
- [ ] Badge on product cards: "Full" indicator when product uses ML Full

### US-057: Daily Storage Cost Calculation Worker
**Description:** As a seller, I want daily storage costs accumulated per SKU for Full products so I see the true warehousing impact on profitability.

**Acceptance Criteria:**
- [ ] `StorageCostWorker` runs daily at midnight
- [ ] For each product with Full inventory: calculates daily storage cost based on size category
- [ ] ML storage cost table: Pequeno R$0.007, Medio R$0.015, Grande R$0.035, Especial R$0.050, Extra R$0.107
- [ ] Penalty multipliers: 91-180 days = 2x, 181-365 = 3x, >365 = 4x
- [ ] Accumulated cost stored in new entity `StorageCostAccumulation`: ProductId, Date, DailyCost, CumulativeCost, DaysStored
- [ ] `GET /api/inventory/{productId}/storage-costs` returns history
- [ ] Storage cost included in order cost decomposition for Full orders
- [ ] Frontend: Product detail shows cumulative storage cost chart

### US-058: Full vs Own Shipping Simulator
**Description:** As a seller, I want to compare Full vs own shipping costs per SKU so I make data-driven fulfillment decisions.

**Acceptance Criteria:**
- [ ] `POST /api/pricing/fulfillment-compare` accepts: productId
- [ ] Calculates Full cost: (dailyStorageCost × avgDaysInStock) + fulfillmentFeePerSale
- [ ] Calculates own shipping cost: avgShippingCost + packagingCost + laborCostPerShipment (configurable)
- [ ] Returns: fullCost, ownShippingCost, recommendation ("Full" or "Envio Proprio"), savings amount
- [ ] Uses historical data: average days in stock, average shipping cost from past orders
- [ ] Frontend: Inventory page "Simulador Full" tab with per-product comparison table
- [ ] Color coding: green for recommended option, shows savings amount

### US-059: Email Provider Integration
**Description:** As a platform operator, I want transactional email infrastructure so the system can send emails (verification, alerts, reports).

**Acceptance Criteria:**
- [ ] `IEmailService` interface: `SendAsync(to, subject, htmlBody, textBody)`
- [ ] Implementation using Resend (or SendGrid) HTTP API
- [ ] API key configurable via environment variable `EMAIL_API_KEY`
- [ ] From address configurable: `EMAIL_FROM` (default: `noreply@perushophub.com.br`)
- [ ] Email templates as Razor views or simple HTML builders
- [ ] Retry logic: 3 attempts with backoff on transient failures
- [ ] Logging: email sent/failed with recipient and subject (not body)
- [ ] When `EMAIL_API_KEY` not set: log email content to console (dev mode)

### US-060: Welcome Email on Registration
**Description:** As a new user, I want a welcome email after registering so I feel confident the registration worked and know next steps.

**Acceptance Criteria:**
- [ ] Email sent after successful registration (async, don't block response)
- [ ] Subject: "Bem-vindo ao PeruShopHub!"
- [ ] Content: greeting with user name, brief "what to do next" steps (connect ML, import products, check first report), link to dashboard
- [ ] Styled HTML with PeruShopHub branding (dark blue + orange accent)
- [ ] Plain text fallback
- [ ] Does not fail registration if email fails (fire-and-forget with logging)

### US-061: Forgot Password Flow
**Description:** As a user, I want to reset my password via email so I can regain access if I forget it.

**Acceptance Criteria:**
- [ ] `POST /api/auth/forgot-password` accepts `{ email }`, returns 200 always (no email enumeration)
- [ ] If email exists: generates reset token (random 64-char string), stores hashed in DB with 1 hour expiry
- [ ] Sends email with reset link: `{frontendUrl}/reset-password?token={token}&email={email}`
- [ ] `POST /api/auth/reset-password` accepts `{ email, token, newPassword }`, validates token, updates password
- [ ] Token is single-use (deleted after successful reset)
- [ ] Frontend: "Esqueci minha senha" link on login page → email input form → success message
- [ ] Frontend: `/reset-password` page with new password + confirmation fields
- [ ] Password validation: minimum 8 characters

### US-062: Email Notifications — Sales, Stock, Margin Alerts
**Description:** As a seller, I want optional email notifications for key events so I stay informed when not logged in.

**Acceptance Criteria:**
- [ ] New entity `NotificationPreference`: TenantId, UserId, Type (enum), EmailEnabled, InAppEnabled
- [ ] Notification types: `NewSale`, `LowStock`, `MarginAlert`, `MLConnectionError`, `SyncError`
- [ ] Migration adds `notification_preferences` table with defaults (all enabled)
- [ ] When notification is created: check user preferences → send email if enabled
- [ ] Email templates per notification type (concise, actionable)
- [ ] `GET/PUT /api/settings/notification-preferences` to manage preferences
- [ ] Frontend Settings > Notificacoes: toggle matrix (type × channel) for each notification type
- [ ] Unsubscribe link in email footer that disables that notification type

### US-063: User Profile Self-Service
**Description:** As a user, I want to edit my profile, change my email, and manage my team so I'm self-sufficient.

**Acceptance Criteria:**
- [ ] `GET /api/profile` returns current user info: name, email, role, tenant info
- [ ] `PUT /api/profile` updates: name, email (email change requires confirmation via old email)
- [ ] `PUT /api/profile/password` changes password (requires current password)
- [ ] Frontend: `/perfil` page with profile form, password change section
- [ ] Team management (existing): list members, invite new, change roles, remove members
- [ ] Profile photo upload (optional, stored via IFileStorageService)
- [ ] Accessible from header dropdown menu

### US-064: Onboarding Wizard
**Description:** As a new seller, I want a guided step-by-step setup so I connect my marketplace and see value quickly.

**Acceptance Criteria:**
- [ ] New entity `OnboardingProgress`: TenantId, StepsCompleted (JSON array of step IDs), IsCompleted, CompletedAt
- [ ] Migration adds `onboarding_progress` table
- [ ] Steps: 1) Complete profile, 2) Connect Mercado Livre, 3) Import products, 4) Set product costs, 5) View first profitability report
- [ ] `GET /api/onboarding/progress` returns current progress
- [ ] `POST /api/onboarding/complete-step` marks a step as done
- [ ] Frontend: `/onboarding` page with step-by-step wizard (progress bar at top)
- [ ] Each step has: description, action button (links to relevant page), completion check
- [ ] Wizard shown automatically on first login after registration
- [ ] Can be dismissed and accessed later from header ("Configuracao Inicial")
- [ ] Step 5 triggers a "moment aha": shows profitability summary with real data, confetti animation
- [ ] After all steps complete: celebration message, redirect to dashboard

### US-065: Setup Checklist & Contextual Tooltips
**Description:** As a new seller, I want contextual help and a persistent setup checklist so I'm guided even outside the wizard.

**Acceptance Criteria:**
- [ ] Dashboard shows "Setup Checklist" card when onboarding not complete
- [ ] Checklist shows steps with checkmarks for completed, links for incomplete
- [ ] Contextual tooltips on key UI elements (first-time only, using localStorage flag):
  - Dashboard: "Estes KPIs sao calculados em tempo real a partir das suas vendas"
  - Products: "Vincule seus produtos aos anuncios do ML para sincronizacao automatica"
  - Finance: "Aqui voce ve a decomposicao completa de custos por venda"
- [ ] Tooltips dismissible (click X or "Entendi"), stored in localStorage per tooltip ID
- [ ] "Mostrar dicas novamente" option in Settings to reset all tooltips

---

## Phase 4 — Post-Sale & Messaging

### US-066: Questions API Integration
**Description:** As a seller, I want to see and answer ML product questions from within the system so I manage all communications in one place.

**Acceptance Criteria:**
- [ ] `GET /my/received_questions/search` fetches unanswered + recent questions
- [ ] Questions synced periodically (every 5 minutes) and on `questions` webhook
- [ ] New entity `MarketplaceQuestion`: TenantId, ExternalId, ItemId (ML), ProductId (internal), BuyerName, QuestionText, AnswerText, Status (Unanswered/Answered), QuestionDate, AnswerDate
- [ ] Migration adds `marketplace_questions` table
- [ ] `GET /api/questions?status=&productId=&page=&pageSize=` lists questions
- [ ] `POST /api/questions/{id}/answer` body: `{ answer }` → calls `POST /answers` on ML API
- [ ] Answer updates local record status to "Answered"
- [ ] Rate limit respected for answer submissions

### US-067: Questions Page (Frontend)
**Description:** As a seller, I want a dedicated questions inbox page so I can quickly see and respond to buyer questions.

**Acceptance Criteria:**
- [ ] New page `/perguntas` with route guard
- [ ] Tabs: "Nao respondidas" (default), "Respondidas", "Todas"
- [ ] Each question card shows: product thumbnail + title, buyer name, question text, time ago
- [ ] Inline answer field (textarea + "Responder" button) — no need to open a modal
- [ ] After answering: card moves from "Nao respondidas" to "Respondidas" with animation
- [ ] Search/filter by product
- [ ] Sort: newest first (default)
- [ ] Sidebar badge: count of unanswered questions (red dot if > 0)
- [ ] Real-time updates via SignalR when new questions arrive

### US-068: Response Templates
**Description:** As a seller, I want reusable response templates so I answer common questions faster.

**Acceptance Criteria:**
- [ ] New entity `ResponseTemplate`: TenantId, Name, Category (string), Body, Placeholders (JSON), UsageCount
- [ ] Migration adds `response_templates` table
- [ ] `GET /api/response-templates` lists templates
- [ ] `POST/PUT/DELETE /api/response-templates` CRUD
- [ ] Placeholders: `{produto}`, `{preco}`, `{prazo}` — auto-replaced with context when applied
- [ ] Frontend: Questions page has "Templates" dropdown on answer textarea
- [ ] Selecting a template fills the answer field with template text (placeholders resolved)
- [ ] Settings > Templates page for managing templates (create, edit, delete, reorder)
- [ ] Seed data: 5 common templates (availability, shipping time, warranty, returns, bulk discount)

### US-069: Post-Sale Messages Inbox
**Description:** As a seller, I want to see and send post-sale messages per order so buyer communication is centralized.

**Acceptance Criteria:**
- [ ] `GET /messages/packs/{packId}/sellers/{sellerId}` fetches message thread per order
- [ ] `POST /messages/packs/{packId}/sellers/{sellerId}` sends message
- [ ] New entity `MarketplaceMessage`: TenantId, ExternalPackId, OrderId, SenderType (buyer/seller), Text, SentAt, IsRead
- [ ] Migration adds `marketplace_messages` table
- [ ] Messages synced on `messages` webhook and periodically
- [ ] `GET /api/messages?orderId=` returns message thread for an order
- [ ] `POST /api/messages` body: `{ orderId, text }` → sends to ML and stores locally
- [ ] Frontend: Order Detail page has "Mensagens" tab with chat-like thread view
- [ ] Message input with send button, character counter (ML limit: 350 chars)
- [ ] Unread message indicator on sidebar "Vendas" item

### US-070: Claims and Returns Management
**Description:** As a seller, I want to track claims and returns so I manage post-sale issues with full context.

**Acceptance Criteria:**
- [ ] ML claims fetched via `GET /post-purchase/v1/claims/search?seller_id={id}`
- [ ] New entity `Claim`: TenantId, ExternalId, OrderId, Type (claim/return), Status, Reason, CreatedAt, ResolvedAt, BuyerComment, SellerComment
- [ ] Migration adds `claims` table
- [ ] Claims synced on webhook and periodically
- [ ] `GET /api/claims?status=&type=&page=` lists claims with filters
- [ ] `POST /api/claims/{id}/respond` sends seller response to ML
- [ ] Frontend: `/reclamacoes` page with claims list, status badges, filter by type/status
- [ ] Claim detail view: timeline of events, order context, buyer info, response form
- [ ] Open claims count shown on sidebar badge
- [ ] Claims impact on product metrics: dashboard shows return rate per product

### US-071: Unanswered Question/Message Alert
**Description:** As a seller, I want alerts when questions or messages go unanswered too long so my response time doesn't hurt my ML reputation.

**Acceptance Criteria:**
- [ ] `ResponseTimeAlertWorker` runs every 30 minutes
- [ ] Checks unanswered questions older than configurable threshold (default: 4 hours)
- [ ] Checks unread buyer messages older than threshold (default: 12 hours)
- [ ] Creates notification: "Voce tem X perguntas sem resposta ha mais de Y horas"
- [ ] Sends email notification if enabled in preferences
- [ ] Threshold configurable per tenant: `GET/PUT /api/settings/response-time-settings`
- [ ] Frontend Settings: response time configuration (hours input for questions and messages)

### US-072: LGPD Compliance — Privacy Policy & Terms
**Description:** As a platform operator, I want LGPD-compliant privacy policy and terms of use so the platform meets Brazilian data protection requirements.

**Acceptance Criteria:**
- [ ] `/termos-de-uso` page with Terms of Use content (pt-BR)
- [ ] `/politica-de-privacidade` page with Privacy Policy content (pt-BR)
- [ ] Both pages publicly accessible (no auth required)
- [ ] Privacy policy covers: data collected, purpose, storage duration, third-party sharing (ML), user rights
- [ ] Terms cover: platform usage, billing, limitations, cancellation
- [ ] Registration form includes checkbox: "Li e aceito os Termos de Uso e Politica de Privacidade" (required)
- [ ] Acceptance timestamp stored on SystemUser: `TermsAcceptedAt`, `PrivacyAcceptedAt`
- [ ] Footer links on all pages to both documents

### US-073: Cookie Consent Banner
**Description:** As a user, I want a cookie consent banner so the platform complies with LGPD requirements for tracking cookies.

**Acceptance Criteria:**
- [ ] Cookie consent banner shown at bottom of page on first visit
- [ ] Options: "Aceitar todos", "Apenas essenciais", "Configurar"
- [ ] Essential cookies: auth token, theme preference, sidebar state (always active)
- [ ] Analytics cookies: if any analytics tool is added (optional, disabled by default)
- [ ] Consent stored in localStorage and cookie (`cookie_consent`)
- [ ] Banner does not appear again after consent given
- [ ] Settings link to modify consent later (accessible from footer)

### US-074: User Data Export & Deletion (LGPD Rights)
**Description:** As a user, I want to export all my data and request account deletion so my LGPD rights are fulfilled.

**Acceptance Criteria:**
- [ ] `POST /api/profile/export-data` generates JSON/ZIP file with all user data:
  - Profile info, tenant info, products, orders, costs, inventory, questions, messages, notifications
  - Excludes other users' data in the same tenant
- [ ] Export runs async — notification with download link when ready (link valid 24h)
- [ ] `POST /api/profile/delete-account` initiates deletion:
  - If user is the only Owner: requires tenant deletion (must confirm, irreversible warning)
  - If user is member: removes TenantUser record, anonymizes SystemUser
  - Confirmation required: password + typed confirmation phrase
- [ ] Data anonymized, not immediately deleted (30-day grace period for recovery)
- [ ] After 30 days: hard delete via background worker
- [ ] Frontend: Profile page has "Exportar meus dados" and "Excluir minha conta" buttons
- [ ] Deletion flow: multi-step confirmation dialog with warnings

### US-075: Accounting Software Integration (Export)
**Description:** As a seller, I want to export my sales data in formats compatible with Brazilian accounting software (Bling/Tiny) so my accountant can process it.

**Acceptance Criteria:**
- [ ] `GET /api/reports/accounting-export?dateFrom=&dateTo=&format=bling` generates Bling-compatible CSV
- [ ] `GET /api/reports/accounting-export?dateFrom=&dateTo=&format=tiny` generates Tiny-compatible CSV
- [ ] Export includes: order ID, date, buyer info, items, amounts, costs, tax info
- [ ] Field mapping matches expected format for each software
- [ ] Frontend: Finance page > "Exportar para ERP" button with format dropdown
- [ ] Downloaded file has descriptive name: `vendas_bling_2026-03-01_2026-03-31.csv`
- [ ] Documentation: which fields map to which Bling/Tiny fields (help text in UI)

---

## Phase 5 — Testing, Security & Go-Live

### US-076: Test Coverage — 70%+ in Services
**Description:** As a developer, I want service layer test coverage above 70% so the financial and business logic is trustworthy.

**Acceptance Criteria:**
- [ ] Test coverage measured with `coverlet` and reported in CI
- [ ] `CostCalculationService`: 90%+ coverage (critical financial logic)
- [ ] `ProductService`, `OrderService`, `InventoryService`: 70%+ each
- [ ] `FinanceService`, `DashboardService`: 70%+ each
- [ ] `PurchaseOrderService`: 70%+ coverage
- [ ] `MercadoLivreAdapter`: 60%+ coverage (mocked HTTP calls)
- [ ] Edge cases tested: null inputs, empty collections, boundary values, concurrent access
- [ ] Coverage report generated in CI and fails if below threshold

### US-077: Integration Tests — Complete
**Description:** As a developer, I want comprehensive integration tests covering all critical flows so deployments are safe.

**Acceptance Criteria:**
- [ ] All controller endpoints have at least one happy-path integration test
- [ ] Webhook processing tested end-to-end: receive webhook → process → verify order created
- [ ] OAuth flow tested with mocked ML API responses
- [ ] Multi-tenant isolation tested: create data as Tenant A, verify invisible to Tenant B across all entities
- [ ] Concurrent PO receive + sale fulfillment: verify stock consistency
- [ ] Token renewal flow tested: expired token → refresh → retry succeeds
- [ ] Report generation tested (PDF + Excel): verify files are generated without errors
- [ ] Tests use TestContainers (PostgreSQL + Redis)
- [ ] All tests run in CI and pass

### US-078: Load Tests — Webhook Processing
**Description:** As a platform operator, I want load tests verifying the system handles 100 simultaneous webhooks so it's ready for peak sales periods.

**Acceptance Criteria:**
- [ ] Load test project using NBomber or k6
- [ ] Scenario 1: 100 concurrent webhook POST requests → all return 200 in < 500ms
- [ ] Scenario 2: 50 concurrent order detail fetches → all return in < 2s
- [ ] Scenario 3: 20 concurrent product creates → no conflicts or data loss
- [ ] Results documented: p50, p95, p99 latency, error rate, throughput
- [ ] Performance baseline established for future regression detection
- [ ] Tests runnable locally and in CI (optional in CI — manual trigger)

### US-079: Angular Test Coverage — Critical Components
**Description:** As a developer, I want Angular tests covering critical user flows so frontend regressions are caught.

**Acceptance Criteria:**
- [ ] Test coverage: 50%+ on shared components (DataGrid, FormField, Dialog, Toast)
- [ ] Auth flow tests: login form validation, token storage, redirect after login
- [ ] Product form tests: required fields, SKU generation, cost fields, save flow
- [ ] Dashboard tests: KPI card rendering, chart data binding, period selector
- [ ] Questions page tests: answer submission, template insertion, tab switching
- [ ] Tests run in CI with headless Chrome
- [ ] Minimum 40 test cases

### US-080: Security Tests — Tenant Isolation & Auth
**Description:** As a platform operator, I want automated security tests verifying tenant isolation and auth boundaries so no data leaks between tenants.

**Acceptance Criteria:**
- [ ] Test: Tenant A creates product → Tenant B GET /products → product NOT in response
- [ ] Test: Tenant A creates order → Tenant B GET /orders/{id} → 404
- [ ] Test: expired JWT → 401 on all endpoints
- [ ] Test: valid JWT without tenant → endpoints that require tenant return 403
- [ ] Test: non-admin user → admin-only endpoints return 403
- [ ] Test: SQL injection attempts in search/filter parameters → properly escaped, no error
- [ ] Test: XSS payload in product name/description → stored safely, rendered escaped
- [ ] Test: CSRF token validation on state-changing endpoints
- [ ] All security tests pass in CI

### US-081: Automated Deploy to VPS
**Description:** As a platform operator, I want automated deployment to production VPS so releases are fast and repeatable.

**Acceptance Criteria:**
- [ ] GitHub Actions workflow: `.github/workflows/deploy.yml`
- [ ] Triggers: manual dispatch or push to `main` with tag `v*`
- [ ] Steps: build Docker images → push to GHCR → SSH to VPS → pull images → docker compose up -d
- [ ] Zero-downtime deployment: pull new images → stop old containers → start new ones
- [ ] Environment variables managed via `.env` file on VPS (not in repo)
- [ ] Post-deploy health check: curl `/health` endpoint, fail deploy if unhealthy
- [ ] Rollback procedure documented: `docker compose up -d --force-recreate` with previous image tag
- [ ] SSL/TLS via Let's Encrypt (certbot) on Nginx

### US-082: Automated PostgreSQL Backup
**Description:** As a platform operator, I want automated daily database backups so data is recoverable in case of failure.

**Acceptance Criteria:**
- [ ] Backup script: `pg_dump` with compression, daily at 3 AM (cron)
- [ ] Backups stored locally and synced to offsite storage (S3-compatible or Backblaze B2)
- [ ] Retention: keep last 7 daily, last 4 weekly, last 3 monthly
- [ ] Backup verification: restore test runs weekly (automated)
- [ ] Monitoring: alert if backup hasn't run in 25 hours
- [ ] Recovery procedure documented: step-by-step restore from backup
- [ ] Backup script tested: full dump + restore + verify data integrity

### US-083: Production Monitoring
**Description:** As a platform operator, I want monitoring for uptime, errors, and performance so I know when something goes wrong.

**Acceptance Criteria:**
- [ ] Uptime monitoring: external service pings `/health` every 60 seconds (UptimeRobot, Betteruptime, or similar)
- [ ] Alert channels: email + (optionally) Telegram/Slack for downtime alerts
- [ ] Sentry dashboard configured with alert rules: new error type → immediate notification
- [ ] Nginx access logs + error logs with rotation
- [ ] Disk space monitoring: alert at 80% usage
- [ ] Redis memory monitoring: alert at 80% max memory
- [ ] PostgreSQL: connection count monitoring, slow query log enabled (> 1s)
- [ ] Basic Grafana dashboard (optional but recommended) or `/health-ui` page showing system status

### US-084: Security Review & Hardening
**Description:** As a platform operator, I want a security review covering OWASP top 10 so the platform is safe for real seller data.

**Acceptance Criteria:**
- [ ] SQL injection review: verify all queries use parameterized queries (EF Core does this by default — verify no raw SQL without parameters)
- [ ] XSS review: verify Angular sanitizes output (default) + backend encodes responses
- [ ] CSRF: verify anti-forgery tokens on state-changing endpoints (or SameSite cookies for JWT)
- [ ] Auth: verify JWT secret is strong (256+ bits), stored in environment variable
- [ ] HTTPS enforced: HTTP → HTTPS redirect, HSTS header
- [ ] CORS configured: only allow frontend origin
- [ ] Rate limiting active on auth endpoints (prevent brute force): 5 login attempts/minute
- [ ] Passwords: BCrypt with work factor ≥ 12
- [ ] OAuth tokens: encrypted at rest (verified in US-035)
- [ ] Tenant isolation: verified in US-080 tests
- [ ] Dependency audit: `dotnet list package --vulnerable`, `npm audit` — no critical vulnerabilities
- [ ] Security headers: X-Content-Type-Options, X-Frame-Options, CSP header
- [ ] Findings documented with severity and remediation status

### US-085: UX Review — All Screens
**Description:** As a product owner, I want a UX review of all screens so the beta experience is polished and consistent.

**Acceptance Criteria:**
- [ ] All 16+ pages reviewed for: consistency, responsiveness, loading states, error states, empty states
- [ ] Mobile responsiveness verified on all pages (viewport 375px width)
- [ ] Dark theme verified on all pages — no broken colors, unreadable text, or missing styles
- [ ] Financial values consistently formatted: R$ 1.234,56 with monospace font
- [ ] All forms have validation messages (no silent failures)
- [ ] All delete actions have confirmation dialogs
- [ ] All loading states use skeleton placeholders (no blank screens)
- [ ] Toast notifications work for all CRUD operations
- [ ] Navigation is consistent: sidebar, breadcrumbs, back buttons
- [ ] Issues documented as GitHub Issues and resolved before beta launch

### US-086: Technical Documentation
**Description:** As a platform operator, I want documentation covering how to run, configure, and troubleshoot the system so operations are not person-dependent.

**Acceptance Criteria:**
- [ ] `Docs/deployment.md`: step-by-step VPS setup, environment variables, Docker commands
- [ ] `Docs/environment-variables.md`: all env vars with descriptions, defaults, and examples
- [ ] `Docs/troubleshooting.md`: common issues and solutions (DB connection, Redis, ML auth, etc.)
- [ ] `Docs/backup-restore.md`: backup verification and restore procedures
- [ ] `.env.example` file in repo root with all required variables (dummy values)
- [ ] Docker Compose `docker-compose.prod.yml` for production (separate from dev)
- [ ] README.md updated with: project overview, quick start, architecture diagram, links to docs

### ~~US-087: Waitlist Landing Page~~ — REMOVED (separate project)

---

## Functional Requirements Summary

| ID | Requirement | Phase |
|----|-------------|-------|
| FR-01 | CI/CD pipeline with build, test, lint on PRs | 0.5 |
| FR-02 | Docker images auto-built and pushed to GHCR | 0.5 |
| FR-03 | Integration test infrastructure with TestContainers | 0.5 |
| FR-04 | Per-tenant API rate limiting (100 req/min) | 0.5 |
| FR-05 | Structured JSON logging with request correlation | 0.5 |
| FR-06 | Sentry error tracking for unhandled exceptions | 0.5 |
| FR-07 | Health check endpoints with dependency verification | 0.5 |
| FR-08 | PO receive automatically adjusts stock and costs | 1 |
| FR-09 | Stock allocation per marketplace channel | 1 |
| FR-10 | Min/max stock thresholds with configurable alerts | 1 |
| FR-11 | Complete stock movement audit trail | 1 |
| FR-12 | Physical stock reconciliation flow | 1 |
| FR-13 | Product cost history with effective dates | 1 |
| FR-14 | Packaging and daily storage cost per product | 1 |
| FR-15 | Commission engine with category/listing type resolution | 2 |
| FR-16 | Tax calculation based on seller's tax regime | 2 |
| FR-17 | Payment fee calculation by installment count | 2 |
| FR-18 | Complete cost decomposition per sale (10 categories) | 2 |
| FR-19 | Materialized view for SKU profitability | 2 |
| FR-20 | PDF report export (QuestPDF) | 2 |
| FR-21 | Excel report export (ClosedXML) | 2 |
| FR-22 | ABC curve analysis from real sales data | 2 |
| FR-23 | Financial dashboard with real KPIs and period selector | 2 |
| FR-24 | Automated profitability reports by email | 2 |
| FR-25 | Audit trail for financial data changes | 2 |
| FR-26 | Target-margin pricing calculator | 2 |
| FR-27 | Scenario simulator for cost changes | 2 |
| FR-28 | Configurable margin alerts | 2 |
| FR-29 | ML adapter with HttpClientFactory | 3 |
| FR-30 | OAuth 2.0 with PKCE for ML connection | 3 |
| FR-31 | OAuth token encryption at rest (AES-256) | 3 |
| FR-32 | Proactive token renewal (30 min before expiry) | 3 |
| FR-33 | Circuit breaker + rate limiter for ML API calls | 3 |
| FR-34 | ML integration settings page with connection status | 3 |
| FR-35 | Import all ML listings with product mapping | 3 |
| FR-36 | Manual link/unlink products to ML items | 3 |
| FR-37 | ML variation → internal variant sync | 3 |
| FR-38 | ML photo import and local storage | 3 |
| FR-39 | Periodic product sync worker (2h interval) | 3 |
| FR-40 | Listings management page | 3 |
| FR-41 | Webhook receiver with < 500ms response and Redis queue | 3 |
| FR-42 | Webhook queue worker with dead letter queue | 3 |
| FR-43 | Historical order import (last 90 days) | 3 |
| FR-44 | ML order → internal order mapping | 3 |
| FR-45 | Webhooks for items, shipments, payments | 3 |
| FR-46 | Billing API integration for real costs | 3 |
| FR-47 | Real shipping cost from ML shipping API | 3 |
| FR-48 | Fulfillment fee from ML billing/dimensions | 3 |
| FR-49 | Auto stock push to ML on internal changes | 3 |
| FR-50 | Detect external stock changes via items webhook | 3 |
| FR-51 | Periodic stock reconciliation with ML (6h) | 3 |
| FR-52 | ML Full stock status query | 3 |
| FR-53 | Daily storage cost accumulation for Full | 3 |
| FR-54 | Full vs own shipping cost simulator | 3 |
| FR-55 | Transactional email infrastructure | 3 |
| FR-56 | Welcome email on registration | 3 |
| FR-57 | Forgot password flow via email | 3 |
| FR-58 | Configurable email notification preferences | 3 |
| FR-59 | User profile self-service (edit, password change) | 3 |
| FR-60 | Onboarding wizard (5-step guided setup) | 3 |
| FR-61 | Setup checklist and contextual tooltips | 3 |
| FR-62 | Questions API: fetch, answer, sync | 4 |
| FR-63 | Questions inbox page with inline answers | 4 |
| FR-64 | Response templates with placeholders | 4 |
| FR-65 | Post-sale messages per order | 4 |
| FR-66 | Claims and returns tracking | 4 |
| FR-67 | Response time alerts for unanswered questions | 4 |
| FR-68 | LGPD privacy policy and terms of use pages | 4 |
| FR-69 | Cookie consent banner | 4 |
| FR-70 | User data export (LGPD portability) | 4 |
| FR-71 | Account deletion with 30-day grace (LGPD erasure) | 4 |
| FR-72 | Accounting export (Bling/Tiny compatible) | 4 |
| FR-73 | 70%+ test coverage in service layer | 5 |
| FR-74 | Comprehensive integration tests | 5 |
| FR-75 | Load tests for webhook processing (100 concurrent) | 5 |
| FR-76 | Angular test coverage on critical components | 5 |
| FR-77 | Automated security tests for tenant isolation | 5 |
| FR-78 | Automated deploy pipeline to VPS | 5 |
| FR-79 | Automated PostgreSQL backup with offsite sync | 5 |
| FR-80 | Production monitoring (uptime, errors, performance) | 5 |
| FR-81 | OWASP security review and hardening | 5 |
| FR-82 | UX review of all screens (responsive, dark mode, consistency) | 5 |
| FR-83 | Technical documentation (deployment, env vars, troubleshooting) | 5 |
| ~~FR-84~~ | ~~Waitlist landing page~~ — removed (separate project) | — |

---

## Non-Goals (Out of Scope for MVP)

- **Amazon/Shopee integration** — Phase 9, post-MVP
- **Billing/Subscriptions** — Phase 6, no payment collection in beta
- **NF-e emission** — Phase 7, sellers use existing ERP for NF-e during beta
- **Advertising/Ads management** — Phase 8, sellers manage ads in ML directly
- **AI insights (Claude API)** — Phase 8, not needed for beta
- **PWA/Push notifications** — Phase 10
- **Multi-language support** — UI is pt-BR only
- **Drag-and-drop reordering** — categories and products sorted alphabetically
- **Real-time price monitoring** — beyond scope
- **Competitor analysis** — Phase 10
- **Public API** — Phase 10
- **Simples Nacional full tax table** — simplified percentage for MVP, full table in Phase 7
- **Landing page / waitlist** — separate project, not part of this PRD

---

## Design Considerations

- **Reuse existing components**: DataGrid, FormField, Dialog, ConfirmDialog, Toast, Badge, Skeleton, EmptyState are already built
- **Design system**: CSS custom properties with light/dark themes, Inter + Roboto Mono fonts
- **Financial values**: always R$ format with Roboto Mono, color-coded (green profit, red loss)
- **Responsive**: all new pages must work on mobile (375px+), tablet, and desktop
- **Loading states**: skeleton placeholders matching actual layout for every async operation
- **Forms**: Angular Reactive Forms, `form-layout` class, saving state with `form-layout--saving`
- **New pages** (Anuncios, Perguntas, Reclamacoes, Onboarding, Landing): follow existing page structure (page-header + content area)

---

## Technical Considerations

- **ML API rate limit**: 18,000 req/hour — all ML calls go through rate-limited HttpClient
- **Webhook response time**: < 500ms mandatory — validate and enqueue only
- **ML has no sandbox**: use test users in production (max 10)
- **Token encryption**: `IDataProtectionProvider` (ASP.NET Core built-in), keys stored on filesystem
- **Financial precision**: always `decimal` (C#) / `NUMERIC(18,4)` (PostgreSQL)
- **Materialized view refresh**: `REFRESH MATERIALIZED VIEW CONCURRENTLY` requires unique index
- **TestContainers**: require Docker-in-Docker or socket access in CI
- **Polly v8**: use new `ResiliencePipeline` API (not deprecated `PolicyBuilder`)
- **Background workers**: use `IHostedService` / `BackgroundService` for MVP (Hangfire for Phase 6+)
- **Email provider**: Resend preferred (simpler API, generous free tier: 100 emails/day)

---

## Success Metrics

- **10-20 sellers** actively using the system in closed beta within 30 days of launch
- **Zero data loss**: all ML orders captured via webhooks with < 1% missed (recovered via missed_feeds)
- **Stock accuracy**: < 2% discrepancy between local and ML stock after reconciliation
- **Cost accuracy**: billing API costs match or override calculated costs for 95%+ of orders
- **Response time**: API p95 < 500ms, webhook processing p95 < 200ms
- **Uptime**: 99.5%+ during beta period
- **Seller satisfaction**: qualitative feedback — "I can see exactly where my money goes per sale"

---

## Decisions

- **Landing page**: Separate project (not part of this PRD). US-087 removed from scope.
- **Beta invite process**: Self-registration, no approval required. Registration is open.
- **VPS provider**: TBD — decide closer to Phase 5 deploy.

## Open Questions

1. **Email provider**: Resend vs SendGrid? Resend is simpler, SendGrid has more features.
2. **ML test users**: How many test users to create (max 10)? Need at least 2 (buyer + seller).
3. **Backup storage**: S3-compatible (Backblaze B2 cheapest) or cloud provider's built-in?
4. **Monitoring**: Self-hosted Grafana stack vs external service (Betteruptime + Sentry)?
5. **VPS provider**: Hetzner vs Contabo? Decide before Phase 5.

---

## Story Dependency Map

```
Phase 0.5 (no dependencies, can start immediately)
  US-001 → US-002 (CI before Docker push)
  US-003 → US-004, US-005, US-006 (test infra before tests)
  US-007 (independent)
  US-008, US-009, US-010, US-011 (independent of each other)

Phase 1 (depends on Phase 0.5 for test infrastructure)
  US-012 (PO receive) → US-013 (allocations depend on stock flow)
  US-014 (min/max) independent
  US-015 (movements) independent
  US-016 (reconciliation) depends on US-015
  US-017 (cost history) independent
  US-018 (packaging/storage) → feeds into Phase 2 cost engine

Phase 2 (depends on Phase 1 for cost data)
  US-019, US-020, US-021 (parallel — commission, tax, payment fee engines)
  US-022 (cost composition) depends on US-019, US-020, US-021
  US-023 (materialized view) depends on US-022
  US-024, US-025 (exports) depend on US-022
  US-026 (ABC curve) depends on US-023
  US-027 (dashboard) depends on US-022, US-023
  US-028 (email reports) depends on US-024/025 + US-059 (email)
  US-029 (audit trail) independent
  US-030, US-031 (pricing) depend on US-022
  US-032 (alerts) depends on US-022

Phase 3 (depends on Phase 2 for financial engine)
  US-033 → US-034 → US-035 → US-036 → US-037 (ML connection chain)
  US-038 (settings page) depends on US-034
  US-039 → US-040 → US-041 → US-042 → US-043 (product sync chain)
  US-044 (listings page) depends on US-039
  US-045 → US-046 → US-048 (webhook → worker → mapping)
  US-047 (historical sync) depends on US-048
  US-049 (additional webhooks) depends on US-046
  US-050, US-051, US-052 (billing/shipping/fulfillment) depend on US-046
  US-053 → US-054 → US-055 (stock sync chain)
  US-056, US-057, US-058 (Full features) depend on US-039
  US-059 → US-060 → US-061 → US-062 (email chain)
  US-063 (profile) independent
  US-064 → US-065 (onboarding chain)

Phase 4 (depends on Phase 3 ML connection)
  US-066 → US-067 → US-068 (questions chain)
  US-069 (messages) depends on US-046 (webhook worker)
  US-070 (claims) depends on US-046
  US-071 (alerts) depends on US-066, US-069
  US-072, US-073, US-074 (LGPD) independent of ML
  US-075 (accounting export) depends on Phase 2 exports

Phase 5 (depends on all previous phases)
  US-076-080 (tests) after features are built
  US-081-083 (deploy/backup/monitoring) independent
  US-084 (security review) after US-080
  US-085 (UX review) after all UI stories
  US-086 (docs) after US-081
```
