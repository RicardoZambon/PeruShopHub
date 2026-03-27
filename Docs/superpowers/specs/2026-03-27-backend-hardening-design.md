# Backend Hardening Sprint — Design Spec

> Date: 2026-03-27
> Branch: `ralph/backend-wiring`
> Status: Approved

## Goal

Extract business logic from fat controllers into a testable service layer, add missing CRUD endpoints, implement input validation with consistent error responses, add role-based authorization, and introduce optimistic locking on key entities.

No frontend changes except handling 409 Conflict responses.

---

## 1. Service Layer Architecture

### Structure

```
src/PeruShopHub.Application/
├── Services/
│   ├── IProductService.cs + ProductService.cs
│   ├── ICategoryService.cs + CategoryService.cs
│   ├── IOrderService.cs + OrderService.cs
│   ├── IPurchaseOrderService.cs + PurchaseOrderService.cs
│   ├── ICustomerService.cs + CustomerService.cs
│   ├── ISupplyService.cs + SupplyService.cs
│   ├── IDashboardService.cs + DashboardService.cs
│   ├── IFinanceService.cs + FinanceService.cs
│   ├── IInventoryService.cs + InventoryService.cs
│   ├── IUserService.cs + UserService.cs
│   └── IFileService.cs + FileService.cs
├── Exceptions/
│   ├── NotFoundException.cs
│   ├── ValidationException.cs
│   └── ConflictException.cs
├── DTOs/ (existing)
└── Validation/ (inline in services, not a separate framework)
```

### Design Decisions

- **No repository pattern.** EF Core's DbContext is the unit-of-work and repository. Adding another abstraction adds complexity without value for this project size.
- **Services take `AppDbContext` directly** via constructor injection.
- **Services return DTOs**, not entities. Controllers never touch entities.
- **Services throw typed exceptions** (`NotFoundException`, `ValidationException`, `ConflictException`). A global `ExceptionFilter` in the API project catches these and returns proper HTTP responses.
- **Interface + implementation** for each service. Registered as scoped in DI.
- **Controllers become thin HTTP adapters**: parse request → call service → return result. No business logic, no EF queries, no calculations.

### Exception Filter

Registered globally in `Program.cs`:

| Exception | HTTP Status | Response Body |
|-----------|-------------|---------------|
| `ValidationException` | 400 | `{ "errors": { "Field": ["message"] } }` |
| `NotFoundException` | 404 | `{ "error": "Recurso não encontrado" }` |
| `ConflictException` | 409 | `{ "error": "message" }` |
| `UnauthorizedAccessException` | 403 | `{ "error": "Acesso negado" }` |

### DI Registration

All services registered in a single `AddApplicationServices()` extension method on `IServiceCollection`, called from `Program.cs`.

---

## 2. Missing CRUD Endpoints

### Supply

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/supplies/{id}` | Supply detail |
| DELETE | `/api/supplies/{id}` | Deactivate supply (soft-delete via `IsActive = false`) |

### Purchase Order Costs

| Method | Route | Description |
|--------|-------|-------------|
| PUT | `/api/purchase-orders/{id}/costs/{costId}` | Update cost value, description, distribution method |

### User Management (Admin only)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/settings/users/{id}` | User detail |
| POST | `/api/settings/users` | Create user with bcrypt password hash |
| PUT | `/api/settings/users/{id}` | Update user profile and role |
| DELETE | `/api/settings/users/{id}` | Deactivate user (soft-delete) |
| POST | `/api/settings/users/{id}/reset-password` | Admin resets user password |

### Auth

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/change-password` | Self-service, requires current password |
| POST | `/api/auth/refresh` | Refresh token (if not already present) |

---

## 3. Input Validation

### Approach

Manual validation inside service methods. Each write method:
1. Creates a `Dictionary<string, List<string>>` for errors
2. Validates all fields
3. If any errors, throws `ValidationException(errors)`

No external validation library (FluentValidation would be overkill for this scope).

### Standard Error Response

```json
{
  "errors": {
    "Name": ["Nome é obrigatório"],
    "Price": ["Preço deve ser maior que zero"],
    "CategoryId": ["Categoria não encontrada"]
  }
}
```

All errors collected and returned at once — never fail on the first error only.

### Key Validation Rules

**Products:**
- Name required, max 200 chars
- SKU unique across products
- Price >= 0, Cost >= 0
- CategoryId must reference existing category
- Weight, dimensions >= 0 if provided

**Categories:**
- Name required, max 100 chars
- Slug required, unique
- ParentId must reference existing category (if provided)
- No circular parent references

**Purchase Orders:**
- Supplier required
- Items must have valid ProductId/VariantId references
- Quantities > 0, UnitCost >= 0
- Status transitions validated (e.g., can only edit if "Rascunho")

**Supplies:**
- Name required, max 200 chars
- CurrentStock >= 0, MinimumStock >= 0
- UnitCost >= 0

**Users:**
- Name required
- Email required, valid format, unique
- Password min 8 chars (on create/reset)
- Role must be one of: Admin, Manager, Viewer

---

## 4. Role-Based Authorization

### Roles

- `Admin` — full access including user management, settings, commission rules
- `Manager` — create/update/delete on business entities (products, categories, orders, supplies, POs)
- `Viewer` — read-only access to all data

### Authorization Matrix

| Endpoints | Required Role |
|-----------|--------------|
| `GET` on all resources | Any authenticated (Viewer+) |
| `POST/PUT/DELETE` on Products, Categories, Supplies, POs | Manager+ |
| `POST/PUT/DELETE` on Orders (costs, fulfillment) | Manager+ |
| Settings: users, integrations, commission rules | Admin |
| `POST /api/auth/login` | Anonymous |
| `POST /api/auth/refresh` | Anonymous (uses refresh token) |
| `POST /api/auth/change-password` | Any authenticated (self) |

### Implementation

- Role claim added to JWT token during login: `new Claim(ClaimTypes.Role, user.Role)`
- Controllers use `[Authorize(Roles = "Admin")]` or `[Authorize(Roles = "Admin,Manager")]`
- No per-entity ownership checks in this sprint (all users see all data)

---

## 5. Optimistic Locking

### Entities

Product, Category, PurchaseOrder, Supply

### Implementation

1. Add `public int Version { get; set; }` property to each entity
2. EF Core config: `builder.Property(e => e.Version).IsConcurrencyToken();`
3. Migration adds `version INTEGER NOT NULL DEFAULT 0` column to 4 tables
4. Update DTOs include `Version` field (required)
5. Service reads entity, checks `entity.Version == dto.Version`, updates, EF auto-increments
6. `DbUpdateConcurrencyException` caught in service → throws `ConflictException`
7. Frontend: on 409, show toast "Este registro foi modificado por outro usuário" and refresh

### Frontend Impact

Minimal: update DTOs to include `version` field, handle 409 in error interceptor with specific toast message.

---

## Scope Summary

| Area | Items |
|------|-------|
| New service files | 11 interfaces + 11 implementations |
| New exception classes | 3 (NotFoundException, ValidationException, ConflictException) |
| New exception filter | 1 (GlobalExceptionFilter) |
| New endpoints | 10 across Supply, PO, User, Auth |
| Entities modified | 4 (add Version property) |
| EF Migration | 1 (add version columns) |
| Controllers refactored | 11 (thin out to HTTP adapters) |
| Frontend changes | Handle 409 in error interceptor, pass version in update DTOs |

### Out of Scope (Deferred)

- Database indexes
- Product.CategoryId string→Guid migration
- Bulk operations
- Soft-delete standardization
- Repository pattern
- FluentValidation
