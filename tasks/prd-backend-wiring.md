# PRD: Backend Wiring + Seed Data + Frontend Integration

## Introduction

Wire up the ASP.NET Core 8 backend with all 5 modular monolith projects, create the PostgreSQL database schema via EF Core migrations, seed it with realistic example data matching the frontend's current mock structure, and replace all frontend mock data with real HTTP calls to the backend API. Includes SignalR for real-time notifications and data change broadcasts, Redis for caching and SignalR backplane, background workers for stock monitoring, and a file upload system for product photos (extensible to future document types). By the end, the Angular frontend is fully powered by the .NET API with zero hardcoded data.

## Goals

- Scaffold the full .NET backend solution (Core, Infrastructure, Application, API, Worker projects)
- Define EF Core entities and DbContext matching the documented data model
- Create PostgreSQL migrations for all entities needed by the current frontend
- Seed the database with realistic example data via toggleable SQL migration scripts
- Expose REST API endpoints that serve data matching what each frontend page consumes
- Create Angular services (one per domain) with `HttpClient` + interceptors
- Remove all mock/hardcoded data from frontend components
- Set up SignalR hub for real-time notifications and data change broadcasts to connected clients
- Set up Redis as cache layer for read-heavy endpoints and SignalR backplane
- Implement background workers for stock alert monitoring and notification cleanup
- Implement file upload system with local storage behind `IFileStorageService` abstraction (swappable to S3/Azure Blob later)
- Frontend looks and works the same as today — but powered by real API calls with real-time updates

## User Stories

### US-001: .NET Solution Scaffolding
**Description:** As a developer, I need the full modular monolith solution structure so all backend code has a proper home.

**Acceptance Criteria:**
- [ ] `PeruShopHub.sln` at repo root with 5 projects:
  - `src/PeruShopHub.Core/` — entities, interfaces, value objects
  - `src/PeruShopHub.Infrastructure/` — EF Core DbContext, persistence, configurations
  - `src/PeruShopHub.Application/` — DTOs, service interfaces, mapping
  - `src/PeruShopHub.API/` — controllers, middleware, Program.cs
  - `src/PeruShopHub.Worker/` — background services (stock alerts, notification cleanup)
- [ ] Project references follow dependency rule: API → Application → Core; Infrastructure → Core; API → Infrastructure (for DI registration); Worker → Application → Core; Worker → Infrastructure
- [ ] `dotnet build` succeeds with zero warnings
- [ ] `.gitignore` updated for .NET artifacts (bin/, obj/, etc.)

---

### US-002: Core Domain Entities
**Description:** As a developer, I need C# entity classes matching the documented data model so EF Core can generate the database schema.

**Acceptance Criteria:**
- [ ] Entities created in `PeruShopHub.Core/Entities/`:
  - `Product` — id (Guid), sku, name, purchaseCost, packagingCost, supplier, isActive, createdAt, updatedAt
  - `ProductVariant` — id, productId (FK), sku, attributes (JSON), price (nullable), stock, isActive
  - `Category` — id, name, slug, parentId (self-ref FK), icon, isActive, productCount, order
  - `Order` — id (Guid), externalOrderId, buyerName, buyerNickname, buyerEmail, itemCount, totalAmount, profit, status, orderDate, createdAt
  - `OrderItem` — id, orderId (FK), productId, name, sku, variation, quantity, unitPrice, subtotal
  - `OrderCost` — id, orderId (FK), category, description, value, source (API/Manual/Calculated)
  - `Customer` — id, name, nickname, email, totalOrders, totalSpent, lastPurchase
  - `Supply` — id, name, sku, category, unitCost, stock, minimumStock, supplier, isActive
  - `Notification` — id, type, title, description, timestamp, isRead, navigationTarget
  - `SystemUser` — id, email, name, role (admin/manager/viewer), isActive
  - `MarketplaceConnection` — id, marketplaceId, name, status, lastSyncAt
  - `FileUpload` — id (Guid), entityType (string), entityId (Guid), fileName, storagePath, contentType, sizeBytes, sortOrder, createdAt
- [ ] `Money` value object in `Core/ValueObjects/` encapsulating decimal + currency
- [ ] All monetary fields use `decimal` (never float/double)
- [ ] Navigation properties and collections defined correctly

---

### US-003: EF Core DbContext and Migrations
**Description:** As a developer, I need the database context and initial migration so PostgreSQL schema is created automatically.

**Acceptance Criteria:**
- [ ] `PeruShopHubDbContext` in `Infrastructure/Persistence/` with `DbSet<T>` for all entities
- [ ] Entity configurations via `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`
- [ ] All monetary columns mapped to `NUMERIC(18,4)` via `.HasPrecision(18, 4)`
- [ ] Proper indexes: Product.Sku (unique), Order.ExternalOrderId (unique), Category.Slug (unique), Customer.Email
- [ ] Self-referencing FK on Category.ParentId with cascade restrict
- [ ] Initial migration generated via `dotnet ef migrations add InitialCreate`
- [ ] `dotnet ef database update` creates schema successfully against local PostgreSQL
- [ ] Connection string configurable via `appsettings.json` and `appsettings.Development.json`

---

### US-004: Seed Data via Toggleable Migration
**Description:** As a developer, I want realistic seed data loaded via a SQL migration script that I can enable/disable, so the app has data to display without marketplace integration.

**Acceptance Criteria:**
- [ ] SQL seed script at `Infrastructure/Persistence/Seeds/SeedData.sql` containing:
  - 10 products with realistic Brazilian e-commerce data (electronics, accessories)
  - 12 product variants across 3 products (color/size/voltage combinations)
  - 27 product categories in a hierarchy (all categories from the product form: Eletrônicos, Celulares e Telefones, Informática, etc.)
  - 15 orders with varied statuses (Pago, Enviado, Entregue, Cancelado, Devolvido)
  - Order items and cost breakdowns for each order (commission, shipping, tax, etc.)
  - 10 customers with masked emails and purchase history
  - 7 supplies (packaging materials with stock levels)
  - 8 notifications (sales, stock alerts, margin warnings)
  - 3 system users (admin, manager, viewer)
  - 2 marketplace connections (ML connected, Amazon coming soon)
- [ ] Seed migration created: `dotnet ef migrations add SeedExampleData`
- [ ] Migration executes the SQL script via `migrationBuilder.Sql()`
- [ ] Seed can be toggled: include a `down` migration that truncates seeded data
- [ ] Data is realistic but fresh (not copy-pasted from frontend mocks — similar structure, new values)
- [ ] Financial values are consistent (order costs sum correctly, margins make sense)

---

### US-005: API Project Setup and Configuration
**Description:** As a developer, I need the API project configured with middleware, CORS, and Swagger so the Angular frontend can call it.

**Acceptance Criteria:**
- [ ] `Program.cs` configures:
  - PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`
  - CORS allowing `http://localhost:4200` (kept as fallback, primary dev access via Angular proxy)
  - Swagger/OpenAPI for development
  - JSON serialization with camelCase property naming
  - System.Text.Json configured for enum string serialization
- [ ] `appsettings.Development.json` with local PostgreSQL connection string
- [ ] Health check endpoint at `GET /health`
- [ ] API runs on `https://localhost:5001` / `http://localhost:5000`
- [ ] `dotnet run --project src/PeruShopHub.API` starts successfully
- [ ] Swagger UI accessible at `/swagger`

---

### US-005a: Redis Setup
**Description:** As a developer, I need Redis configured as a cache layer and SignalR backplane so read-heavy endpoints are fast and SignalR scales.

**Acceptance Criteria:**
- [ ] `StackExchange.Redis` NuGet package added to Infrastructure project
- [ ] Redis connection configured via `appsettings.json` (`ConnectionStrings:Redis`)
- [ ] `IDistributedCache` registered with Redis provider in DI
- [ ] `ICacheService` interface in Core with methods: `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `RemoveByPrefixAsync`
- [ ] `RedisCacheService` implementation in Infrastructure
- [ ] Cache applied to read-heavy endpoints: dashboard summary, dashboard charts, finance KPIs, product list
- [ ] Cache keys use consistent prefix pattern: `{entity}:{operation}:{params-hash}`
- [ ] Cache TTL: 60s for dashboard/finance KPIs, 300s for product/category lists
- [ ] Cache invalidated on write operations (create/update/delete) for the affected entity
- [ ] Developer runs Redis locally: `docker run -d -p 6379:6379 redis:7-alpine`
- [ ] App starts gracefully if Redis is unavailable (fallback to no-cache, log warning)

---

### US-005b: SignalR Hub Setup
**Description:** As a developer, I need a SignalR hub that broadcasts real-time notifications and data change events to connected Angular clients.

**Acceptance Criteria:**
- [ ] `Microsoft.AspNetCore.SignalR.StackExchangeRedis` package for Redis backplane
- [ ] `NotificationHub` class in `API/Hubs/` with:
  - `ReceiveNotification` — pushes new notification to all connected clients
  - `DataChanged` — pushes entity change events (type: string, action: created/updated/deleted, id: string)
- [ ] SignalR configured in `Program.cs` with Redis backplane
- [ ] SignalR endpoint mapped at `/hubs/notifications`
- [ ] CORS updated to allow SignalR WebSocket/SSE connections from `localhost:4200`
- [ ] `INotificationDispatcher` interface in Core, implemented in Infrastructure, that:
  - Saves notification to DB
  - Pushes via SignalR hub to connected clients
- [ ] Controllers call `INotificationDispatcher` when data changes occur (order created, product updated, etc.)
- [ ] Angular `@microsoft/signalr` npm package installed
- [ ] Angular `SignalRService` in `services/`:
  - Connects to hub on app init
  - Exposes `notifications$` Observable for new notifications
  - Exposes `dataChanged$` Observable for entity change events
  - Auto-reconnect with exponential backoff
- [ ] `NotificationService` updated to merge REST-fetched notifications with SignalR live push
- [ ] Components that display lists (products, orders, etc.) subscribe to `dataChanged$` and auto-refresh when relevant entity changes
- [ ] Angular proxy config updated to proxy `/hubs` → `http://localhost:5000/hubs`

---

### US-005c: Background Workers
**Description:** As a developer, I need background workers that run periodic tasks — stock monitoring and notification cleanup.

**Acceptance Criteria:**
- [ ] Worker project (`PeruShopHub.Worker`) configured as a .NET `Worker Service` (not a web host)
- [ ] Shares DI registration with API (DbContext, services, Redis) via shared extension method
- [ ] **StockAlertWorker** (`BackgroundService`):
  - Runs every 15 minutes (configurable via `appsettings.json`)
  - Queries supplies where `stock <= minimumStock`
  - Creates a `Notification` (type: `stock`, title: "Estoque baixo: {supply.name}") for each low-stock supply
  - Skips if an unread notification already exists for that supply (no spam)
  - Dispatches via `INotificationDispatcher` (saves + pushes via SignalR)
- [ ] **NotificationCleanupWorker** (`BackgroundService`):
  - Runs once daily (configurable)
  - Deletes read notifications older than 30 days
  - Logs count of deleted notifications
- [ ] Both workers use `ILogger` for structured logging
- [ ] Workers respect cancellation token for graceful shutdown
- [ ] `dotnet run --project src/PeruShopHub.Worker` starts successfully

---

### US-005d: File Upload System
**Description:** As a developer, I need a file upload API and storage abstraction so product photos (and future document types) can be uploaded and served.

**Acceptance Criteria:**
- [ ] `IFileStorageService` interface in Core:
  - `Task<string> UploadAsync(Stream file, string fileName, string contentType, string folder)`
  - `Task DeleteAsync(string storagePath)`
  - `Task<Stream> GetAsync(string storagePath)`
  - `string GetPublicUrl(string storagePath)`
- [ ] `LocalFileStorageService` implementation in Infrastructure:
  - Stores files in `wwwroot/uploads/{folder}/{guid}-{fileName}`
  - Returns relative URL path (e.g., `/uploads/products/abc123-photo.jpg`)
- [ ] Storage base path configurable via `appsettings.json` (`FileStorage:BasePath`)
- [ ] `POST /api/files/upload` endpoint:
  - Accepts `multipart/form-data` with fields: `file`, `entityType`, `entityId`, `sortOrder`
  - Validates: max 5MB, allowed types (jpg, jpeg, png, webp)
  - Saves file via `IFileStorageService`, creates `FileUpload` DB record
  - Returns: `{ id, url, fileName, contentType, sizeBytes }`
- [ ] `DELETE /api/files/{id}` deletes file from storage and DB record
- [ ] `GET /api/files?entityType=product&entityId={id}` returns files for an entity
- [ ] Static file serving configured for `/uploads` path in `Program.cs`
- [ ] `FileUpload` entity linked to products via `entityType` + `entityId` (polymorphic, not FK — supports future entity types like invoices/PDFs)
- [ ] Product API responses include `photos: FileUpload[]` populated from FileUpload table
- [ ] Angular `FileUploadService` in `services/`:
  - `upload(file: File, entityType: string, entityId: string): Observable<FileUploadResponse>`
  - `delete(fileId: string): Observable<void>`
  - `getFiles(entityType: string, entityId: string): Observable<FileUpload[]>`
- [ ] Product form updated with photo upload zone (drag & drop or click to browse)
- [ ] Product list and detail show uploaded photos instead of empty placeholders

---

### US-006: Dashboard API Endpoints
**Description:** As a user, I want the dashboard to load real data from the backend so I see actual KPIs, charts, and product rankings.

**Acceptance Criteria:**
- [ ] `GET /api/dashboard/summary?period={hoje|7dias|30dias}` returns:
  - KPI cards: totalSales, grossRevenue, netProfit, averageMargin (with change % vs prior period)
- [ ] `GET /api/dashboard/chart/revenue-profit?days=30` returns daily revenue vs profit arrays
- [ ] `GET /api/dashboard/chart/cost-breakdown?period=30dias` returns cost category distribution
- [ ] `GET /api/dashboard/top-products?limit=5` returns most profitable products (sales, revenue, profit, margin)
- [ ] `GET /api/dashboard/least-profitable?limit=5` returns least profitable products
- [ ] `GET /api/dashboard/pending-actions` returns counts: unanswered questions, pending orders, alerts
- [ ] All monetary values returned as numbers (not strings), frontend handles formatting
- [ ] Response DTOs defined in `Application/DTOs/Dashboard/`

---

### US-007: Products API Endpoints
**Description:** As a user, I want to browse, search, and view products from the backend.

**Acceptance Criteria:**
- [ ] `GET /api/products?page=1&pageSize=20&search=&status=&sortBy=&sortDir=` returns paginated product list
  - Response includes: id, photo (empty placeholder), name, sku, price, stock, status, margin, variantCount, needsReview
  - Supports filtering by status (Ativo, Pausado, Encerrado)
  - Supports search by name or SKU
  - Supports sorting by any column
- [ ] `GET /api/products/{id}` returns full product detail with variants
- [ ] `GET /api/products/{id}/variants` returns variants for a product
- [ ] `POST /api/products` creates a product (for product form)
- [ ] `PUT /api/products/{id}` updates a product
- [ ] Response includes variant count and stock aggregation
- [ ] DTOs in `Application/DTOs/Products/`

---

### US-008: Sales/Orders API Endpoints
**Description:** As a user, I want to browse orders and see detailed cost breakdowns per sale.

**Acceptance Criteria:**
- [ ] `GET /api/orders?page=1&pageSize=20&search=&status=&dateFrom=&dateTo=&sortBy=&sortDir=` returns paginated order list
  - Response includes: id, date, buyerName, itemCount, totalAmount, profit, status
  - Supports filtering by status and date range
  - Supports search by order ID or buyer name
- [ ] `GET /api/orders/{id}` returns full order detail:
  - Order items (product, SKU, variation, quantity, unitPrice, subtotal)
  - Buyer info (name, nickname, email, phone, totalOrders, totalSpent)
  - Shipping info (tracking, carrier, logisticType, timeline)
  - Payment info (method, installments, amount, status)
  - Cost breakdown (all cost line items with category, description, value, source)
- [ ] DTOs in `Application/DTOs/Orders/`

---

### US-009: Customers API Endpoints
**Description:** As a user, I want to see my customer list from the backend.

**Acceptance Criteria:**
- [ ] `GET /api/customers?page=1&pageSize=20&search=&sortBy=&sortDir=` returns paginated customer list
  - Response: id, name, nickname, email (masked), totalOrders, totalSpent, lastPurchase
  - Supports search by name, nickname, or email
- [ ] `GET /api/customers/{id}` returns customer detail with order history
- [ ] DTOs in `Application/DTOs/Customers/`

---

### US-010: Supplies API Endpoints
**Description:** As a user, I want to manage packaging supplies from the backend.

**Acceptance Criteria:**
- [ ] `GET /api/supplies?page=1&pageSize=20&search=&category=&status=&sortBy=&sortDir=` returns paginated supplies
  - Response: id, name, sku, category, unitCost, stock, minimumStock, supplier, status
  - Supports filtering by category and status
- [ ] `POST /api/supplies` creates a supply
- [ ] `PUT /api/supplies/{id}` updates a supply
- [ ] DTOs in `Application/DTOs/Supplies/`

---

### US-011: Finance API Endpoints
**Description:** As a user, I want financial analytics from the backend — KPIs, charts, SKU profitability, reconciliation.

**Acceptance Criteria:**
- [ ] `GET /api/finance/summary?period={hoje|7dias|30dias}` returns finance KPIs (revenue, costs, profit, margin, ticket)
- [ ] `GET /api/finance/chart/revenue-profit?days=30` returns daily bar chart data
- [ ] `GET /api/finance/chart/margin?days=30` returns daily margin % line chart data
- [ ] `GET /api/finance/sku-profitability?page=1&pageSize=20&sortBy=margin&sortDir=desc` returns per-SKU breakdown:
  - sku, product name, sales count, revenue, COGS, commissions, shipping, taxes, profit, margin
- [ ] `GET /api/finance/reconciliation?year=2026` returns monthly expected vs deposited with divergence
- [ ] `GET /api/finance/abc-curve` returns products classified A/B/C with cumulative profit %
- [ ] DTOs in `Application/DTOs/Finance/`

---

### US-012: Settings API Endpoints
**Description:** As a user, I want settings data served from the backend.

**Acceptance Criteria:**
- [ ] `GET /api/settings/users` returns system users (id, name, email, role, isActive, lastLogin)
- [ ] `GET /api/settings/integrations` returns marketplace connections (id, name, status, lastSync)
- [ ] `GET /api/settings/costs` returns default cost config (packaging cost, ICMS rate, fixed costs)
- [ ] DTOs in `Application/DTOs/Settings/`

---

### US-013: Notifications API Endpoint
**Description:** As a user, I want notifications served from the backend.

**Acceptance Criteria:**
- [ ] `GET /api/notifications` returns notification list (id, type, title, description, timestamp, isRead, navigationTarget)
- [ ] `PATCH /api/notifications/{id}/read` marks a notification as read
- [ ] `PATCH /api/notifications/read-all` marks all as read
- [ ] DTOs in `Application/DTOs/Notifications/`

---

### US-014a: Categories API Endpoints
**Description:** As a user, I want to browse and manage product categories from the backend with lazy-loaded children.

**Acceptance Criteria:**
- [ ] `GET /api/categories?parentId=` returns direct children of the given parent (roots when `parentId` is null/omitted)
  - Response: id, name, slug, parentId, icon, isActive, productCount, order, hasChildren (boolean)
- [ ] `GET /api/categories/{id}` returns category detail with variation fields
- [ ] `POST /api/categories` creates a category
- [ ] `PUT /api/categories/{id}` updates a category
- [ ] `DELETE /api/categories/{id}` deletes a category (only if no children or products)
- [ ] DTOs in `Application/DTOs/Categories/`

---

### US-014: Search API Endpoint
**Description:** As a user, I want the command palette (Ctrl+K) to search across products, orders, and customers from the backend.

**Acceptance Criteria:**
- [ ] `GET /api/search?q=term&limit=10` returns mixed results:
  - type (pedido/produto/cliente), id, primary text, secondary text, route
  - Searches across products (name, SKU), orders (ID, buyer), customers (name, email)
  - Results ordered by relevance/type
- [ ] DTO in `Application/DTOs/Search/`

---

### US-015: Angular HTTP Interceptor and Environment Config
**Description:** As a developer, I need a base URL interceptor and environment configuration so all Angular services call the correct API.

**Acceptance Criteria:**
- [ ] `proxy.conf.json` at `PeruShopHub.Web/` root proxying `/api` → `http://localhost:5000/api`
- [ ] `angular.json` dev server configured with `"proxyConfig": "proxy.conf.json"`
- [ ] `environment.ts` and `environment.development.ts` with `apiUrl: '/api'` (relative, proxy handles routing)
- [ ] HTTP interceptor for error handling (toast on 4xx/5xx)
- [ ] Interceptors registered in `app.config.ts` via `provideHttpClient(withInterceptors([...]))`
- [ ] `provideHttpClient()` added to app providers

---

### US-016: Angular Domain Services
**Description:** As a developer, I need one Angular service per domain that wraps HttpClient calls, replacing mock data access.

**Acceptance Criteria:**
- [ ] Services created in `services/`:
  - `DashboardService` — summary, charts, top/least profitable, pending actions
  - `ProductService` — list (paginated), detail, variants, create, update
  - `OrderService` — list (paginated), detail with costs
  - `CustomerService` — list (paginated), detail
  - `SupplyService` — list (paginated), create, update
  - `FinanceService` — summary, charts, SKU profitability, reconciliation, ABC curve
  - `SettingsService` — users, integrations, costs
  - `CategoryService` (rewire existing) — list by parent (lazy), detail, create, update, delete
  - `SearchService` — global search
- [ ] Each service method returns `Observable<T>` with proper DTOs/interfaces
- [ ] NotificationService updated to fetch from API (keep SignalR placeholder for future)
- [ ] All services use typed request/response interfaces from `models/`

---

### US-017: Wire Dashboard Component to API
**Description:** As a user, I want the dashboard to display real backend data instead of hardcoded values.

**Acceptance Criteria:**
- [ ] Dashboard component calls `DashboardService` for all data
- [ ] Period filter (Hoje/7dias/30dias) triggers new API calls
- [ ] KPI cards, charts, product tables, and pending actions all from API
- [ ] Loading skeletons shown while data loads
- [ ] All `MOCK_*` constants removed from `dashboard.component.ts`
- [ ] Verify in browser: dashboard looks the same as before

---

### US-017a: Wire Categories Page to API
**Description:** As a user, I want the categories page to load data from the backend with lazy-loaded children.

**Acceptance Criteria:**
- [ ] `CategoryService` rewired to call `GET /api/categories?parentId=` instead of using mock data
- [ ] Tree view loads root categories on init, loads children on expand (lazy)
- [ ] Create/update/delete operations call API endpoints
- [ ] All mock category data removed from `CategoryService`
- [ ] Verify in browser: categories page works with lazy loading

---

### US-018: Wire Products Pages to API
**Description:** As a user, I want the products list, detail, and form to use real backend data.

**Acceptance Criteria:**
- [ ] Products list calls `ProductService.list()` with pagination, search, status filter, sort
- [ ] Product detail calls `ProductService.getById()` and `ProductService.getVariants()`
- [ ] Product form calls `ProductService.create()` / `ProductService.update()`
- [ ] All `MOCK_PRODUCTS` and `MOCK_PRODUCT` constants removed
- [ ] `ProductVariantService` updated to fetch variants from API
- [ ] Verify in browser: products pages look the same

---

### US-019: Wire Sales Pages to API
**Description:** As a user, I want the sales list and detail pages to use real backend data.

**Acceptance Criteria:**
- [ ] Sales list calls `OrderService.list()` with pagination, search, status/date filters, sort
- [ ] Sale detail calls `OrderService.getById()` returning items, buyer, shipping, payment, costs
- [ ] All `MOCK_ORDERS`, `MOCK_ORDER`, `MOCK_SUPPLIES` (in sale detail) removed
- [ ] Verify in browser: sales pages look the same

---

### US-020: Wire Customers Page to API
**Description:** As a user, I want the customers page to use real backend data.

**Acceptance Criteria:**
- [ ] Customers list calls `CustomerService.list()` with pagination, search, sort
- [ ] All `MOCK_CUSTOMERS` removed
- [ ] Verify in browser: customers page looks the same

---

### US-021: Wire Supplies Page to API
**Description:** As a user, I want the supplies page to use real backend data.

**Acceptance Criteria:**
- [ ] Supplies list calls `SupplyService.list()` with pagination, search, category/status filters
- [ ] All `MOCK_SUPPLIES` removed
- [ ] Verify in browser: supplies page looks the same

---

### US-022: Wire Finance Page to API
**Description:** As a user, I want the finance page to use real backend data.

**Acceptance Criteria:**
- [ ] Finance KPIs, bar chart, margin chart, SKU profitability table, reconciliation, ABC curve all from `FinanceService`
- [ ] Period filter triggers new API calls
- [ ] All hardcoded chart data and `skuData` arrays removed
- [ ] Verify in browser: finance page looks the same

---

### US-023: Wire Settings Page to API
**Description:** As a user, I want the settings page to use real backend data.

**Acceptance Criteria:**
- [ ] Users, integrations, and cost config loaded from `SettingsService`
- [ ] All `MOCK_USERS`, `MOCK_INTEGRATIONS`, hardcoded costs removed
- [ ] Verify in browser: settings page looks the same

---

### US-024: Wire Search Palette to API
**Description:** As a user, I want the command palette (Ctrl+K) to search against the backend.

**Acceptance Criteria:**
- [ ] Search palette calls `SearchService.search(query)` with debounce (300ms)
- [ ] All mock product/order/customer arrays removed from search palette component
- [ ] Verify in browser: search works the same

---

### US-025: Wire Notifications to API + SignalR
**Description:** As a user, I want notifications loaded from the backend and new ones pushed in real-time via SignalR.

**Acceptance Criteria:**
- [ ] NotificationService fetches initial list from `GET /api/notifications`
- [ ] Mark-as-read calls `PATCH /api/notifications/{id}/read`
- [ ] SignalR connection established on app init via `SignalRService`
- [ ] New notifications pushed via SignalR appear immediately in the notification panel (no page refresh)
- [ ] Data change events from SignalR trigger list refreshes on relevant pages
- [ ] All `MOCK_NOTIFICATIONS` removed
- [ ] Verify in browser: notification panel shows real-time updates

---

### US-026: Final Mock Data Cleanup
**Description:** As a developer, I want to verify no mock data remains in the frontend codebase.

**Acceptance Criteria:**
- [ ] Search entire frontend for `MOCK_` constants — zero results
- [ ] Search for hardcoded arrays of objects that look like data — zero results
- [ ] All components load data via services, not inline constants
- [ ] `ng build` succeeds with zero errors
- [ ] `dotnet build` succeeds with zero errors
- [ ] App runs end-to-end: `dotnet run` API + `ng serve` frontend, all pages load data from API

## Functional Requirements

- FR-1: .NET solution with 5 projects following modular monolith architecture (Core, Infrastructure, Application, API, Worker)
- FR-2: EF Core entities for all domains currently represented in frontend mock data
- FR-3: PostgreSQL database with NUMERIC(18,4) precision for all monetary columns
- FR-4: Initial migration creates full schema; second migration seeds example data via SQL script
- FR-5: Seed migration is reversible (down migration truncates seeded data)
- FR-6: REST API endpoints serve paginated, filterable, sortable data for every frontend page
- FR-7: API response DTOs use camelCase JSON matching what Angular components expect
- FR-8: CORS configured to allow Angular dev server (localhost:4200)
- FR-9: Swagger UI available in development for API exploration
- FR-10: One Angular service per domain, using HttpClient with typed responses
- FR-11: HTTP interceptor handles base URL prefixing and global error handling
- FR-12: All frontend mock data completely removed after wiring
- FR-13: Global search endpoint searches across products, orders, and customers
- FR-14: SignalR hub broadcasts real-time notifications and data change events (created/updated/deleted) to connected clients
- FR-15: Redis serves as distributed cache (dashboard KPIs, product lists) and SignalR backplane
- FR-16: Cache invalidated on entity write operations; TTL-based expiry as fallback
- FR-17: StockAlertWorker checks supply levels every 15 minutes, creates notifications for low stock
- FR-18: NotificationCleanupWorker deletes read notifications older than 30 days (runs daily)
- FR-19: File upload API accepts product photos (jpg/png/webp, max 5MB) via multipart/form-data
- FR-20: `IFileStorageService` abstraction with local disk implementation, swappable to cloud storage
- FR-21: Angular components auto-refresh data when SignalR broadcasts relevant entity changes

## Non-Goals

- **No authentication/JWT** — login page stays as-is (static), auth is a separate PRD
- **No Mercado Livre API integration** — all data comes from seed, not ML API
- **No unit/integration tests** — separate PRD for test infrastructure
- **No full Docker Compose** — individual containers (PostgreSQL, Redis) run via `docker run`; full orchestration is a future PRD
- **No real cost calculations** — seeded financial data is pre-calculated, not computed dynamically (separate PRD)
- **No thumbnail generation** — uploaded photos served at original size
- **No cloud storage** — local disk only (abstraction in place for future swap to S3/Azure Blob)

## Technical Considerations

- **PostgreSQL local setup**: `docker run -d --name perushophub-db -p 5432:5432 -e POSTGRES_PASSWORD=dev postgres:16`
- **Redis local setup**: `docker run -d --name perushophub-redis -p 6379:6379 redis:7-alpine`
- **EF Core provider**: `Npgsql.EntityFrameworkCore.PostgreSQL`
- **JSON serialization**: `System.Text.Json` with `JsonStringEnumConverter` and `camelCase` naming policy
- **Pagination**: All list endpoints return `{ items: T[], totalCount: number, page: number, pageSize: number }`
- **Seed SQL script**: Uses PostgreSQL-compatible INSERT statements with explicit IDs for referential integrity
- **Angular proxy**: `proxy.conf.json` proxies `/api` and `/hubs` to `localhost:5000`
- **Categories**: `CategoryService` (already exists) rewired to HTTP calls with lazy-loading
- **Product variant service**: Already exists with mock data — rewire to API
- **SignalR packages**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` (server), `@microsoft/signalr` (Angular)
- **Redis packages**: `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis`
- **File upload**: Files stored at `wwwroot/uploads/`, served as static files; `FileUpload` entity uses polymorphic `entityType`+`entityId` pattern (no FK) to support products now, invoices/PDFs later
- **Worker hosting**: Worker project is a standalone `Worker Service` — runs as separate process via `dotnet run --project src/PeruShopHub.Worker`
- **Graceful degradation**: App starts and works if Redis is unavailable (cache miss = DB query, log warning); SignalR falls back to no-backplane mode

## Success Metrics

- All 12+ frontend pages load data from the API with no mock data remaining
- `dotnet build` and `ng build` both succeed with zero errors
- Developer can start the system with: PostgreSQL + Redis running → `dotnet ef database update` → `dotnet run` (API) → `dotnet run` (Worker) → `ng serve`
- Swagger shows all endpoints documented and callable
- Page load times under 500ms for list views with seed data volume
- Real-time: creating/updating data in Swagger triggers auto-refresh on the Angular page
- Notification panel receives live push when StockAlertWorker detects low stock
- Product photos can be uploaded and displayed on product list/detail/form
- App starts and works if Redis is down (degraded, no cache, no backplane)

## Resolved Questions

1. **Angular proxy.conf.json** — Yes, use `proxy.conf.json` to proxy `/api` and `/hubs` → `localhost:5000`. No CORS needed for dev.
2. **Category tree loading** — Support lazy-loading children (`GET /api/categories?parentId=` returns direct children; `parentId=null` returns roots). Frontend updated to lazy-load on expand.
3. **Seed categories** — Include all 27 product categories in the seed data (the full list from the product form), organized in a proper hierarchy.
4. **Categories endpoint** — Yes, create `GET /api/categories` and rewire the existing `CategoryService` to use HTTP calls.
5. **SignalR scope** — Notifications + data change broadcasts (any entity create/update/delete triggers `DataChanged` event so pages auto-refresh).
6. **Redis role** — Cache layer for read-heavy endpoints (dashboard, finance, product lists) + SignalR backplane. Graceful fallback if Redis is unavailable.
7. **Background workers** — StockAlertWorker (every 15min, checks supply stock vs minimum) + NotificationCleanupWorker (daily, deletes read notifications > 30 days old).
8. **File storage** — Local disk behind `IFileStorageService` abstraction (swappable to S3/Azure Blob later). Product photos for now; generic `entityType`+`entityId` pattern supports future PDFs for sales/purchases.
9. **File upload scope** — Product photos only for now. Generic design so future entity types (invoices, purchase orders) can reuse the same system.
