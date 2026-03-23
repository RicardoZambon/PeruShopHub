# PeruShopHub — Development Status & Roadmap

> Last updated: 2026-03-23

## Project Overview

**PeruShopHub** is a centralized multi-marketplace management system focused on **real per-sale profitability tracking**. It calculates true net profit per sale considering all costs (marketplace commission, fixed fees, shipping, fulfillment, advertising, taxes, product cost, packaging, coupon absorption).

**Tech Stack:** .NET 9 / ASP.NET Core / EF Core 9 / PostgreSQL 16 / Redis 7 / SignalR / Angular 21 / Chart.js

---

## What Has Been Accomplished

### Phase A — UI/UX Design System (Completed)

**PRDs:** `prd-ui-ux-design.md`, `prd-ui-improvements-batch2.md`, `prd-categories-variants.md`, `prd-product-form-redesign.md`

**Branch:** Merged to `main` via PR #1

Delivered a complete Angular 21 frontend with:

- **Design system**: CSS custom properties (light + dark themes), Inter + Roboto Mono fonts, color semantics for financial values (green=profit, red=loss), responsive breakpoints
- **Layout**: Collapsible sidebar (256px/64px), fixed 56px header, mobile drawer overlay
- **12+ pages** fully designed:
  - Dashboard (KPI cards, revenue/profit chart, cost breakdown donut, top/bottom products)
  - Products (list, detail, form with variants, gallery)
  - Categories (hierarchical tree with variation fields)
  - Sales (list, detail with full cost breakdown, timeline, buyer info)
  - Customers (list, detail with order history)
  - Supplies (CRUD with stock alerts)
  - Finance (KPIs, charts, SKU profitability, reconciliation, ABC curve)
  - Settings (users, integrations, costs, alerts, appearance)
  - Login page
- **Shared components**: DataTable (pagination, sort, search), KpiCard, Badge, Skeleton, EmptyState, Toast, SearchPalette (Ctrl+K)
- **All data was mocked** — hardcoded constants in components

---

### Phase B — Backend Wiring + Infrastructure (Completed)

**PRD:** `prd-backend-wiring.md`

**Branch:** `ralph/backend-wiring` (27 commits, pending PR to main)

Delivered a fully wired full-stack application:

#### Backend (.NET 9 — 5 projects)

| Project | Contents |
|---------|----------|
| **PeruShopHub.Core** | 12 entities (Product, ProductVariant, Category, Order, OrderItem, OrderCost, Customer, Supply, Notification, SystemUser, MarketplaceConnection, FileUpload), Money value object, 4 interfaces (ICacheService, INotificationDispatcher, INotificationHubContext, IFileStorageService) |
| **PeruShopHub.Infrastructure** | EF Core DbContext with 12 entity configurations, RedisCacheService, SignalRNotificationDispatcher, LocalFileStorageService, seed data migration |
| **PeruShopHub.Application** | 30+ DTOs across 11 domains, PagedResult\<T\> generic pagination |
| **PeruShopHub.API** | 11 controllers (Dashboard, Products, Categories, Orders, Customers, Supplies, Finance, Settings, Notifications, Search, Files), SignalR NotificationHub, Swagger, health checks |
| **PeruShopHub.Worker** | StockAlertWorker (15min interval), NotificationCleanupWorker (daily) |

#### Database (PostgreSQL 16)

- Full schema with 12 tables, proper indexes, NUMERIC(18,4) precision for all monetary columns
- Seed data: 27 categories, 10 products, 12 variants, 15 orders, 96 order cost items, 10 customers, 7 supplies, 8 notifications, 3 users, 2 marketplace connections
- Toggleable seed migration (can be reversed)

#### Infrastructure

| Component | Status | Details |
|-----------|--------|---------|
| **Redis** | Implemented | Cache on dashboard summary + product list (60s TTL), SignalR backplane, graceful fallback if Redis unavailable |
| **SignalR** | Implemented | NotificationHub at `/hubs/notifications`, real-time notification push, DataChanged broadcasts on product/supply create/update |
| **File Uploads** | Implemented | Local storage with `IFileStorageService` abstraction (swappable to S3/Azure Blob), 5MB limit, jpg/png/webp, polymorphic entity design |
| **Background Workers** | Implemented | StockAlertWorker checks supplies below minimum stock, NotificationCleanupWorker removes old read notifications |

#### Frontend Wiring (Angular 21)

- **11 HTTP services** created (Dashboard, Product, Order, Customer, Supply, Finance, Settings, Category, Search, Notification, FileUpload)
- **SignalR service** with auto-reconnect and `notifications$` / `dataChanged$` observables
- **Error interceptor** with toast notifications for HTTP errors
- **Proxy configuration** (`/api`, `/hubs`, `/uploads` → localhost:5000)
- **All 12+ pages rewired** from mock data to real API calls
- **Zero `MOCK_*` constants remaining** in the frontend codebase

#### API Endpoints (11 Controllers, 40+ Endpoints)

| Domain | Endpoints |
|--------|-----------|
| Dashboard | summary, chart/revenue-profit, chart/cost-breakdown, top-products, least-profitable, pending-actions |
| Products | list (paginated), getById, getVariants, create, update |
| Categories | list by parent (lazy-load), getById, create, update, delete |
| Orders | list (paginated, filterable), getById (with items, buyer, shipping, payment, costs) |
| Customers | list (paginated), getById (with order history) |
| Supplies | list (paginated), create, update |
| Finance | summary, chart/revenue-profit, chart/margin, sku-profitability, reconciliation, abc-curve |
| Settings | users, integrations, costs |
| Notifications | list, mark-read, mark-all-read |
| Search | global search across products, orders, customers |
| Files | upload, list by entity, delete |

---

## What Is Pending — Next Phases

### Phase C — Authentication & Authorization (Not Started)

**Priority: High** — Required before any real usage.

| Item | Description |
|------|-------------|
| JWT authentication | Login endpoint, access + refresh tokens |
| Route guards | Protect all API endpoints and Angular routes |
| Role-based access | admin, manager, viewer roles (entities already exist) |
| Password hashing | bcrypt for SystemUser passwords |
| Login page wiring | Connect the existing static login page to auth endpoints |

---

### Phase D — Real Cost Calculations (Not Started)

**Priority: High** — This is the core product differentiator.

Currently, all financial data is pre-calculated in seed data. Real cost calculation requires:

| Item | Description |
|------|-------------|
| Commission rules engine | ML commission varies by category, seller reputation, listing type |
| Shipping cost calculation | Weight × distance × carrier rates |
| Tax calculation | ICMS, PIS/COFINS, Simples Nacional (varies by state and tax regime) |
| Fulfillment fee lookup | ML Full storage + handling fees |
| Payment fee calculation | Card processing fees, installment-dependent |
| Cost aggregation service | Composes all cost categories per sale |
| ML Billing API integration | `GET /orders/{id}/billing_info` for real fee data |

This should be its own PRD with focused attention — it's the product's unique value proposition.

---

### Phase E — Mercado Livre API Integration (Not Started)

**Priority: High** — Required for real data flow. Maps to original Roadmap Fase 1.

| Item | Description |
|------|-------------|
| OAuth 2.0 flow | Authorize, token exchange, refresh via ML DevCenter app |
| Token management | AES-256 encryption at rest, proactive renewal worker, circuit breaker |
| Rate limiter | 18,000 req/hour client-side enforcement |
| Product sync | Import existing listings from ML (`GET /users/{id}/items`) |
| Order sync | `GET /orders/search` for historical, webhooks for real-time |
| Webhook receiver | `orders_v2`, `items`, `questions`, `payments`, `shipments` |
| Webhook processing | Validate signature → enqueue Redis → worker processes (< 500ms response) |
| Questions API | List and answer questions via ML API |
| IMarketplaceAdapter | Implement the adapter pattern for ML (keyed DI) |

---

### Phase F — Test Infrastructure (Not Started)

**Priority: Medium** — Important for maintainability as complexity grows.

| Item | Description |
|------|-------------|
| Unit test project | `tests/PeruShopHub.UnitTests/` with xUnit |
| Integration test project | `tests/PeruShopHub.IntegrationTests/` with TestContainers |
| Controller tests | Verify API response shapes and status codes |
| Service tests | Business logic validation |
| Angular tests | Component + service unit tests |
| CI pipeline | GitHub Actions running tests on PR |

---

### Phase G — Docker & Deployment (Not Started)

**Priority: Medium** — Required for production deployment.

| Item | Description |
|------|-------------|
| Dockerfile.api | Multi-stage build for API project |
| Dockerfile.worker | Multi-stage build for Worker project |
| Dockerfile.web | Nginx serving Angular build |
| docker-compose.yml | Full orchestration (API, Worker, Angular, PostgreSQL, Redis, Nginx) |
| nginx.conf | Reverse proxy configuration |
| Environment configuration | Secrets management, production connection strings |

---

### Phase H — Inventory Management (Not Started)

Maps to original Roadmap Fase 3.

| Item | Description |
|------|-------------|
| Stock movements | CRUD for entries, exits, adjustments with history |
| ML stock sync | Auto-update ML stock on local changes |
| Reconciliation worker | Periodic comparison local vs ML stock |
| Optimistic locking | Version column on inventory for race condition prevention |
| ML Full integration | Fulfillment stock, operations, storage cost calculation |

---

### Phase I — Marketing & Ads (Not Started)

Maps to original Roadmap Fase 4.

| Item | Description |
|------|-------------|
| Advertising API integration | Campaign metrics, ACOS, ROI per product |
| Ad cost attribution | Allocate advertising spend per sale |
| Promotion management | Create, edit, delete via ML API |

---

### Phase J — Multi-Marketplace Expansion (Not Started)

Maps to original Roadmap Fase 5.

| Item | Description |
|------|-------------|
| Amazon SP-API adapter | Implement `IMarketplaceAdapter` for Amazon |
| Shopee Open Platform adapter | Implement `IMarketplaceAdapter` for Shopee |
| Centralized inventory | Master stock with per-marketplace allocations |
| Cross-channel sync | Sale in one marketplace → update stock across all |
| Unified dashboard | Comparative metrics across marketplaces |

---

### Phase K — Post-Sale & Messaging (Not Started)

Maps to original Roadmap Fase 6.

| Item | Description |
|------|-------------|
| Unified inbox | Questions + post-sale messages across all marketplaces |
| Response templates | Configurable by situation type |
| Claims & returns | Workflow management with timeline and evidence |

---

## Known Technical Debt

Items identified during team lead review that should be addressed:

| Item | Severity | Description |
|------|----------|-------------|
| Fat controllers | Low | All business logic in controllers. Extract to Application services when adding tests. |
| `RemoveByPrefixAsync` stub | Low | Redis prefix deletion not implemented (requires direct `IConnectionMultiplexer`). Works for now since cache invalidation uses exact keys. |
| Worker NuGet version mismatch | Low | `Microsoft.EntityFrameworkCore.Relational` 9.0.1 vs 9.0.14 in Worker — pin to match Infrastructure. |
| Orphaned environment files | Low | Duplicate `environment.ts` at two paths — clean up the outer `src/environments/`. |
| Dashboard/Finance memory queries | Medium | `GetProductRankings` and `GetSkuProfitability` load all order items into memory. Add date range filtering for scalability. |
| Category hierarchy shallow | Low | Seed data has 27 categories but most are root-level. Deepen the hierarchy for realistic testing. |
| No application-layer services | Low | Acceptable for current scope, but needed before adding business rules or tests. |

---

## Recommended Next Phase Priority

```
1. Authentication (Phase C)     — gate for everything else
2. ML Integration (Phase E)     — real data flow
3. Cost Calculations (Phase D)  — core product value
4. Tests (Phase F)              — safety net for growing complexity
5. Docker (Phase G)             — production deployment
6. Everything else (H-K)        — feature expansion
```

---

## How to Run the Current System

```bash
# 1. Start PostgreSQL and Redis
docker run -d --name perushophub-db -p 5432:5432 -e POSTGRES_PASSWORD=dev postgres:16
docker run -d --name perushophub-redis -p 6379:6379 redis:7-alpine

# 2. Apply database migrations (creates schema + seed data)
dotnet ef database update --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API

# 3. Start the API (http://localhost:5000)
dotnet run --project src/PeruShopHub.API

# 4. Start the Worker (background jobs)
dotnet run --project src/PeruShopHub.Worker

# 5. Start the Angular frontend (http://localhost:4200)
cd src/PeruShopHub.Web && npm install && npx ng serve

# Swagger: http://localhost:5000/swagger
# Health: http://localhost:5000/health
```
