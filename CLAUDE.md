# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PeruShopHub** is a centralized multi-marketplace management system focused on **real per-sale profitability tracking**. It starts with Mercado Livre integration and is architected to expand to Amazon, Shopee, and others.

The core differentiator: no existing ERP/hub calculates true net profit per sale considering all costs (marketplace commission, fixed fees, real shipping cost, fulfillment, advertising, taxes, product cost, packaging, coupon absorption).

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# / ASP.NET Core 8+ Web API |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 16 (NUMERIC(18,4) for financial precision) |
| Cache/Queue | Redis 7+ |
| Background Jobs | .NET BackgroundService + Hangfire |
| Real-time | SignalR |
| Frontend | Angular 17+ |
| UI Components | Angular Material or PrimeNG |
| Charts | Chart.js (ng2-charts) |
| HTTP Resilience | HttpClientFactory + Polly (retry, circuit breaker) |
| Auth | JWT + Refresh Token |
| Export | QuestPDF (PDF) + ClosedXML (Excel) |
| AI (questions) | Claude API (Anthropic SDK) |
| Containers | Docker + Docker Compose |
| Reverse Proxy | Nginx |
| CI/CD | GitHub Actions |

## Architecture

**Modular Monolith** — microservices are overengineering for solo/small team. Clear module separation:

```
src/
├── PeruShopHub.Core/            # Domain: entities, interfaces, value objects
├── PeruShopHub.Infrastructure/  # Adapters, persistence, HTTP clients
├── PeruShopHub.Application/     # Use cases, services
├── PeruShopHub.API/             # Controllers, webhooks, SignalR hubs
├── PeruShopHub.Worker/          # Background jobs
└── PeruShopHub.Web/             # Angular frontend
tests/
├── PeruShopHub.UnitTests/
└── PeruShopHub.IntegrationTests/
docker/
├── Dockerfile.api
├── Dockerfile.worker
├── Dockerfile.web
└── nginx.conf
```

### Key Design Pattern: Adapter Pattern

Each marketplace implements `IMarketplaceAdapter`. The core never knows which marketplace it's using. New marketplaces are added via DI keyed services:

```csharp
services.AddKeyedScoped<IMarketplaceAdapter, MercadoLivreAdapter>("mercadolivre");
services.AddKeyedScoped<IMarketplaceAdapter, AmazonAdapter>("amazon");
```

### Financial Model

All monetary values use `decimal` (C#) / `NUMERIC(18,4)` (PostgreSQL). The `Money` value object encapsulates decimal + currency. The `sale_costs` table decomposes every sale into granular cost categories: `marketplace_commission`, `fixed_fee`, `shipping_seller`, `payment_fee`, `fulfillment_fee`, `storage_daily`, `product_cost`, `packaging`, `advertising`, `tax`, etc.

### Webhook Flow

```
Webhook arrives → API validates + enqueues (Redis) → Worker processes
    → Updates order → Updates inventory across all marketplaces
    → Records detailed financials → Notifies frontend (SignalR)
```

### Inventory

Master stock is the single source of truth, with per-marketplace allocations. Optimistic locking (`version` column) prevents overselling. Periodic reconciliation worker compares local vs marketplace stock.

## Build & Run Commands

```bash
# Start all services (API, Worker, Angular, PostgreSQL, Redis, Nginx)
docker compose up -d

# Run backend only
dotnet run --project src/PeruShopHub.API

# Run worker
dotnet run --project src/PeruShopHub.Worker

# Run frontend
cd src/PeruShopHub.Web && ng serve

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/PeruShopHub.UnitTests
dotnet test tests/PeruShopHub.IntegrationTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# EF Core migrations
dotnet ef migrations add MigrationName --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API
dotnet ef database update --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API

# Build production Docker images
docker compose -f docker-compose.yml build
```

## Important Constraints

- **Mercado Livre rate limit**: 18,000 req/hour — the API client must implement rate limiting
- **Webhook response time**: < 500ms (ML requirement) — validate and enqueue, process async
- **OAuth tokens**: encrypted at rest (AES-256), proactive renewal via background worker, circuit breaker after 3 consecutive failures
- **ML has no sandbox**: use test users in production (max 10) created via `POST /users/test_user`
- **Financial precision**: never use `float`/`double` for money — always `decimal`/`NUMERIC(18,4)`
- **Inventory locks**: optimistic locking with version column to prevent race conditions

## Design System & UI Guidelines

The full design spec is in `Docs/PeruShopHub-Design-System.md`. Key rules for frontend implementation:

### Theming
- Light + Dark themes via CSS custom properties (not Sass variables) — must support runtime switching
- Theme preference persisted in `localStorage`, initial value from `prefers-color-scheme`
- Primary: `#1A237E` (dark blue), Accent: `#FF6F00` (orange)

### Typography & Fonts
- UI font: **Inter** (400, 500, 600, 700)
- Financial data / IDs / SKUs: **Roboto Mono** (400, 500)
- All monetary values must use monospace font and BRL formatting: `R$ 1.234,56`

### Color Semantics for Financial Values
- Profit/positive: `--success` (green)
- Loss/negative: `--danger` (red)
- Margin thresholds: green >=20%, yellow 10-19%, red <10%
- Cost increases: red (up is bad); Revenue increases: green (up is good)

### Layout
- Collapsible sidebar: 256px expanded, 64px collapsed (icons only)
- Fixed header: 56px height
- Sidebar overlay drawer on mobile (<768px)
- Tables transform to card lists on mobile

### Responsive Breakpoints
- Mobile: 0-767px
- Tablet: 768-1023px
- Desktop: 1024px+
- Desktop-lg: 1280px+

### Component Patterns
- All tables: server-side pagination, column sorting, search filter
- All forms: Angular Reactive Forms, labels above fields, inline validation
- Loading: skeleton placeholders matching real layout dimensions
- Modals close on Esc and outside click
- Toast notifications: top-right, auto-dismiss 5s, max 3 stacked
- Global search: `Ctrl+K` / `Cmd+K` command palette

### Angular-Specific
- Use Angular `currency` pipe with `'BRL'` for money formatting
- Use Angular Reactive Forms (not template-driven)
- Route guards: `AuthGuard` on all routes, `RoleGuard` for admin settings
- Sidebar and theme state managed via Angular services + `localStorage`

## Current Status

**Pre-project phase** — design and documentation only. No code has been written yet. The `Docs/` folder contains complete requirements, architecture, data models, API references, and roadmap. The immediate next step is Phase 0 (project scaffolding, Docker setup, initial migrations).

## Documentation Reference

- `Docs/Sistema-Requisitos-e-Arquitetura.md` — Full requirements, architecture, data model, and development phases
- `Docs/PeruShopHub-Roadmap.md` — Detailed sprint-by-sprint roadmap with the definitive stack
- `Docs/PeruShopHub-Design-PreProjeto.md` — Complete data structure definitions, UI/UX navigation map, visual identity
- `Docs/PeruShopHub-Design-System.md` — Full design system: tokens, components, screen specs, responsive patterns
- `Docs/MercadoLivre-API-Referencia-Completa.md` — ML API reference
- `Docs/MercadoLivre-API-Avancada-Sellers.md` — Advanced seller API operations
- `Docs/MercadoLivre-Modelos-de-Venda.md` — ML sales models and fee structures
- `Docs/MercadoLivre-Produtos-e-Investimento.md` — ML product listing and investment details

## Language Note

Documentation is written in Brazilian Portuguese. Code (variables, comments, API names) should be in English. UI text should be in Portuguese (pt-BR).
