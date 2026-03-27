# MVP-Deployable: Auth + Docker + Core Wiring

**Date:** 2026-03-27
**Scope:** Make PeruShopHub deployable as a working MVP with authentication, containerization, and remaining critical wiring.

---

## 1. JWT Authentication

### 1.1 Backend Auth

**Goal:** Add JWT + Refresh Token auth to the ASP.NET Core API.

**SystemUser entity changes:**
- Add `PasswordHash` (string, required) — BCrypt hash
- Add `RefreshToken` (string, nullable) — opaque token for refresh flow
- Add `RefreshTokenExpiresAt` (DateTime, nullable)
- Keep existing fields: Email, Name, Role, IsActive, LastLogin, CreatedAt
- New migration to add these columns + seed a default admin user

**New endpoint: `AuthController`**
- `POST /api/auth/login` — accepts `{ email, password }`, returns `{ accessToken, refreshToken, user: { id, name, email, role } }`
- `POST /api/auth/refresh` — accepts `{ refreshToken }`, returns new token pair
- `POST /api/auth/logout` — invalidates refresh token
- `GET /api/auth/me` — returns current user from JWT claims

**Token configuration:**
- Access token: JWT, 15 min expiry, signed with HMAC-SHA256 secret from `appsettings.json > Jwt:Secret`
- Refresh token: cryptographically random, 7 day expiry, stored in DB
- Claims: `sub` (user ID), `email`, `name`, `role`

**Middleware:**
- Add `AddAuthentication().AddJwtBearer()` to `Program.cs`
- Add `app.UseAuthentication()` + `app.UseAuthorization()` before `app.MapControllers()`
- Add `[Authorize]` attribute to all controllers EXCEPT `AuthController.Login` and health check
- No role-based authorization for MVP — just authenticated vs not

**Password hashing:** Use `BCrypt.Net-Next` NuGet package.

**Seed data:** Create admin user `admin@perushophub.com` / `admin123` (dev only).

### 1.2 Frontend Auth

**New `AuthService` (Angular service):**
- `login(email, password): Promise<User>` — calls `/api/auth/login`, stores tokens in `localStorage`, returns user
- `logout(): void` — calls `/api/auth/logout`, clears localStorage, navigates to `/login`
- `refreshToken(): Promise<string>` — calls `/api/auth/refresh`, updates stored tokens
- `getAccessToken(): string | null` — reads from localStorage
- `isAuthenticated(): boolean` — checks token exists and not expired (decode JWT exp claim)
- `currentUser` signal — reactive user state

**Token storage keys:** `psh_access_token`, `psh_refresh_token`, `psh_user`

**New `AuthInterceptor` (HTTP interceptor):**
- Attaches `Authorization: Bearer <token>` to all API requests
- On 401 response: attempt token refresh, retry original request
- On refresh failure: logout and redirect to `/login`
- Skip auth header for `/api/auth/login` and `/api/auth/refresh`

**New `AuthGuard` (route guard):**
- Checks `AuthService.isAuthenticated()`
- Redirects to `/login` if not authenticated
- Applied to the layout route (protects all child routes)

**Login page wiring:**
- Replace `setTimeout` stub with `AuthService.login()` call
- Show API error messages (wrong password, account disabled)
- Redirect to `/dashboard` on success

**Header component update:**
- Show current user name + avatar placeholder
- Add logout button/menu item

---

## 2. Docker Setup

**Goal:** Single `docker-compose up` to run the full stack.

### Files to create:

**`docker/Dockerfile.api`** — Multi-stage build for PeruShopHub.API
- Stage 1: `mcr.microsoft.com/dotnet/sdk:9.0` for build
- Stage 2: `mcr.microsoft.com/dotnet/aspnet:9.0` for runtime
- Expose port 5000

**`docker/Dockerfile.worker`** — Multi-stage build for PeruShopHub.Worker
- Same base images as API
- No port exposure (background service)

**`docker/Dockerfile.web`** — Multi-stage build for Angular
- Stage 1: `node:22-alpine` for build (`ng build --configuration=production`)
- Stage 2: `nginx:alpine` to serve static files
- Expose port 80

**`docker/nginx.conf`** — Reverse proxy config
- `/` → Angular static files
- `/api/` → proxy to API container (port 5000)
- `/hubs/` → proxy to API container with WebSocket upgrade headers
- `/health` → proxy to API health endpoint

**`docker-compose.yml`** — Orchestration
```yaml
services:
  db:        # PostgreSQL 16
  redis:     # Redis 7-alpine
  api:       # PeruShopHub.API (depends on db, redis)
  worker:    # PeruShopHub.Worker (depends on db)
  web:       # Nginx + Angular (depends on api)
```

**Environment variables via `docker-compose.yml`:**
- `ConnectionStrings__DefaultConnection` — PostgreSQL connection
- `ConnectionStrings__Redis` — Redis connection (StackExchange format)
- `Jwt__Secret` — HMAC signing key
- `Jwt__Issuer` / `Jwt__Audience` — token metadata
- `ASPNETCORE_ENVIRONMENT=Production`

**Volumes:**
- `postgres_data` — persistent DB storage
- `redis_data` — persistent cache
- `uploads` — file uploads

**Network:** All services on `perushophub-net` bridge network.

---

## 3. Wire Sale-Detail to Real API

**Goal:** Replace `MOCK_ORDER` in sale-detail with real API data.

### Backend changes:

**`OrdersController` — add/verify endpoints:**
- `GET /api/orders/{id}` — returns full order detail with items, buyer, shipping, payment, costs
- `POST /api/orders/{id}/costs` — add manual cost to an order
- `PUT /api/orders/{id}/costs/{costId}` — update manual cost
- `DELETE /api/orders/{id}/costs/{costId}` — remove manual cost
- `POST /api/orders/{id}/recalculate` — already exists, verify it returns updated order

**DTO:** `OrderDetailDto` should include:
- Order metadata (id, date, status, externalOrderId)
- Items array with name, sku, variation, quantity, unitPrice, subtotal
- Buyer info (name, nickname, email, phone, totalOrders, totalSpent)
- Shipping info (trackingNumber, carrier, logisticType, timeline)
- Payment info (method, installments, amount, status)
- Costs array (category, value, source, description, color)
- Revenue, totalCosts, netProfit, profitMargin

### Frontend changes:

**`OrderService` — add methods:**
- `getDetail(id: string): Observable<OrderDetail>` — fetches full detail
- `addCost(orderId: string, dto): Observable<OrderCost>` — create manual cost
- `updateCost(orderId: string, costId: string, dto): Observable<OrderCost>`
- `deleteCost(orderId: string, costId: string): Observable<void>`

**`SaleDetailComponent` — replace mock data:**
- Remove `MOCK_ORDER`, `MOCK_SUPPLIES` constants
- Load real data via `OrderService.getDetail()` in `ngOnInit`
- Wire cost form to `addCost/updateCost/deleteCost` API calls
- Wire recalculate button to real endpoint (already partially done)
- Wire supply section to real Supply API (if supply-sale relation exists in backend; if not, note as future work)

---

## 4. SignalR Activation

**Goal:** Wire the existing SignalR hub into the frontend for real-time updates.

### Backend:

**`NotificationHub`** — implement the hub (currently empty stub):
- `OnConnectedAsync()` — log connection
- No custom methods needed — server-to-client push only
- The `SignalRNotificationDispatcher` already broadcasts via hub context

### Frontend:

**`SignalRService` changes:**
- Call `.start()` on app initialization (after successful auth)
- Disconnect on logout
- Expose `notifications$` observable or signal for UI consumption

**`NotificationService` integration:**
- On `ReceiveNotification` event: add to notification panel, show toast
- On `DataChanged` event: emit to a shared signal that page components can subscribe to for refresh

**Header notification badge:**
- Show unread count from `NotificationService`
- Already has notification panel component — wire it to real notifications

---

## 5. Out of Scope (Future Work)

- Mercado Livre OAuth + API integration
- Webhook receiver for ML orders
- Role-based authorization (admin vs viewer)
- CI/CD (GitHub Actions)
- Export (PDF/Excel)
- Questions/AI integration
- Listings page (ML listing sync)
- Rate limiting / Polly resilience
- Supply-to-sale linkage (backend schema change needed)

---

## 6. Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Password hashing | BCrypt | Industry standard, built-in salt, configurable work factor |
| Token storage | localStorage | Simpler than httpOnly cookies for SPA; acceptable for internal business tool |
| Refresh strategy | Silent refresh on 401 | No background timer; refresh only when needed |
| Docker base | Official MS images | Smallest attack surface, best .NET support |
| Nginx role | Reverse proxy + static server | Single entry point, handles CORS/SSL in production |
| Auth scope | All-or-nothing for MVP | No role granularity yet; just authenticated = full access |

---

## 7. Success Criteria

1. `docker-compose up` starts all services and the app is accessible at `http://localhost`
2. Login with seeded admin credentials works end-to-end
3. Unauthenticated requests to `/api/*` return 401
4. Sale detail page loads real order data with cost breakdown
5. Real-time notifications appear when data changes (e.g., stock alert)
6. Token refresh works transparently when access token expires
