# Multi-Tenancy Design Spec

**Date:** 2026-03-27
**Status:** Approved
**Branch:** ralph/backend-wiring

## Overview

Add multi-tenancy to PeruShopHub so multiple shops can use the platform as a SaaS, each with fully isolated data. Tenant = Shop. Shared database with EF Core global query filters for row-level isolation.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Tenant model | Flat (1 tenant = 1 shop) | Covers 90% of use cases, extensible later |
| Data isolation | Shared DB, row-level filtering | Simplest ops, EF Core native support |
| Signup | Self-service | Standard SaaS, no approval gate |
| Super-admin | Yes | Needed for tenant management without DB access |
| Existing data | Migrate to demo tenant, promote admin to super-admin | Preserves dev workflow |

## New Entities

### Tenant

```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; }          // "Loja do Joao"
    public string Slug { get; set; }          // "loja-do-joao" (unique)
    public bool IsActive { get; set; }        // Super-admin can disable
    public DateTime CreatedAt { get; set; }
}
```

### TenantUser (join table)

```csharp
public class TenantUser
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; }          // "Owner", "Admin", "Manager", "Viewer"
    public DateTime CreatedAt { get; set; }

    public Tenant Tenant { get; set; }
    public SystemUser User { get; set; }
}
```

### SystemUser Changes

```csharp
// ADD:
public bool IsSuperAdmin { get; set; }        // Default false
public ICollection<TenantUser> TenantMemberships { get; set; }

// REMOVE:
// public string Role { get; set; }           // Moved to TenantUser
```

**Role is now per-tenant.** A user could theoretically be Admin in one shop and Viewer in another.

## Tenant-Scoped Entities

Every entity below gets a `TenantId` (Guid, FK, non-nullable, indexed) column:

| Entity | Notes |
|--------|-------|
| Product | |
| ProductVariant | Inherits via Product FK, but also gets TenantId for direct query filtering |
| ProductCostHistory | |
| Category | |
| VariationField | |
| Order | |
| OrderItem | Inherits via Order FK, also gets TenantId |
| OrderCost | Inherits via Order FK, also gets TenantId |
| Customer | |
| PurchaseOrder | |
| PurchaseOrderItem | Also gets TenantId |
| PurchaseOrderCost | Also gets TenantId |
| Supply | |
| StockMovement | |
| MarketplaceConnection | |
| CommissionRule | |
| Notification | |
| FileUpload | |

**Why TenantId on child entities too?** Global query filters in EF Core apply per-entity. If OrderItem doesn't have a filter, a cross-tenant join could leak data. Adding TenantId to children is the safe pattern.

## ITenantContext Service

```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsSuperAdmin { get; }
    void SetTenantId(Guid tenantId);
}
```

Registered as **Scoped**. Set by middleware on each request.

## Tenant Resolution Middleware

```
Request arrives
  → If path starts with /api/auth/login, /api/auth/register, /api/auth/refresh → skip
  → Extract JWT claims:
      - tenant_id → ITenantContext.TenantId
      - is_super_admin → ITenantContext.IsSuperAdmin
  → If super-admin + X-Tenant-Id header present → use header value (impersonation)
  → If no tenant_id and not super-admin → 403
```

## DbContext Changes

### Base Entity

```csharp
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
```

All 18 tenant-scoped entities implement `ITenantScoped`.

### Global Query Filters

In `OnModelCreating`:

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
    {
        // Apply filter: e => e.TenantId == _tenantContext.TenantId
        modelBuilder.Entity(entityType.ClrType)
            .HasQueryFilter(BuildTenantFilter(entityType.ClrType));
    }
}
```

**Super-admin bypass:** When `ITenantContext.IsSuperAdmin` is true and no tenant is set, filters are ignored via `.IgnoreQueryFilters()` on specific admin queries.

### Auto-set TenantId on SaveChanges

Override `SaveChangesAsync` to automatically set `TenantId` on new entities:

```csharp
foreach (var entry in ChangeTracker.Entries<ITenantScoped>()
    .Where(e => e.State == EntityState.Added))
{
    entry.Entity.TenantId = _tenantContext.TenantId!.Value;
}
```

## JWT Claims

Current claims:
- `sub` (user id), `email`, `name`, `role`

New claims:
- `tenant_id` (Guid) — current tenant
- `tenant_role` (string) — role within current tenant
- `is_super_admin` (bool)

Remove: `role` (replaced by `tenant_role`)

## API Endpoints

### New Auth Endpoints

```
POST /api/auth/register
  Body: { shopName, name, email, password }
  Creates: SystemUser + Tenant + TenantUser(Owner)
  Returns: AuthResponse with tenant-scoped JWT

GET /api/auth/tenants
  Returns: list of tenants the current user belongs to

POST /api/auth/switch-tenant
  Body: { tenantId }
  Returns: new AuthResponse scoped to that tenant
```

### New Tenant Management Endpoints (tenant-scoped)

```
GET    /api/tenant                    → current tenant details
PUT    /api/tenant                    → update shop name/settings
GET    /api/tenant/members            → list members
POST   /api/tenant/members/invite     → invite by email
PUT    /api/tenant/members/{userId}   → change member role
DELETE /api/tenant/members/{userId}   → remove member
```

### New Super-Admin Endpoints

```
GET    /api/admin/tenants             → list all tenants (paginated)
GET    /api/admin/tenants/{id}        → tenant detail with member count
PUT    /api/admin/tenants/{id}        → enable/disable tenant
POST   /api/admin/impersonate/{tenantId} → get tenant-scoped token
GET    /api/admin/stats               → platform-level metrics
```

### Existing Endpoints

**No URL changes.** All existing endpoints (products, orders, categories, etc.) continue working identically. The tenant filter is applied transparently by the DbContext. The only change is that the JWT must now contain `tenant_id`.

### Settings/Users Endpoints Change

The current `/api/settings/users` endpoints become tenant-scoped member management (moved to `/api/tenant/members`). The settings controller keeps cost/integration/commission endpoints.

## Authorization Matrix

| Action | SuperAdmin | Owner | Admin | Manager | Viewer |
|--------|-----------|-------|-------|---------|--------|
| Platform admin panel | Yes | - | - | - | - |
| Create/delete tenant | Yes | - | - | - | - |
| Impersonate tenant | Yes | - | - | - | - |
| Update shop settings | - | Yes | Yes | - | - |
| Invite/remove members | - | Yes | Yes | - | - |
| Change member roles | - | Yes | - | - | - |
| Create/edit products | - | Yes | Yes | Yes | - |
| Create/edit orders | - | Yes | Yes | Yes | - |
| View data | - | Yes | Yes | Yes | Yes |
| Delete products/orders | - | Yes | Yes | - | - |

## Onboarding Flow

1. User navigates to `/register`
2. Fills form: Shop Name, Full Name, Email, Password
3. `POST /api/auth/register`:
   - Validate email uniqueness
   - Create `SystemUser` (IsSuperAdmin = false)
   - Create `Tenant` (Name, Slug auto-generated from name)
   - Create `TenantUser` (Role = "Owner")
   - Generate JWT with tenant context
4. Frontend receives token, stores in localStorage, redirects to `/dashboard`

## Migration Strategy

Single migration with these steps:

1. Create `Tenants` table
2. Create `TenantUsers` table (composite PK: TenantId + UserId)
3. Add `IsSuperAdmin` to `SystemUsers`, default false
4. Add `TenantId` (nullable) to all 18 tenant-scoped tables
5. Insert demo tenant: `INSERT INTO Tenants (Id, Name, Slug, IsActive) VALUES ('demo-guid', 'Demo Shop', 'demo-shop', true)`
6. `UPDATE SystemUsers SET IsSuperAdmin = true WHERE Email = 'admin@perushophub.com'`
7. Create TenantUser rows for all 3 existing users → demo tenant
8. `UPDATE Products SET TenantId = 'demo-guid'` (and all other tables)
9. Make `TenantId` non-nullable
10. Add FK constraints and indexes
11. Drop `Role` column from `SystemUsers`

## Frontend Changes

### New Pages
- `/register` — public signup page
- `/admin/tenants` — super-admin tenant list (if IsSuperAdmin)

### Modified Components
- **Header** — show current tenant name, add tenant switcher dropdown (if user has multiple tenants)
- **Sidebar** — show shop name at top, add "Admin" section for super-admins
- **Login page** — add "Create account" link
- **Settings > Users** — becomes tenant member management (invite, roles)

### Auth Service Changes
- Store `tenantId`, `tenantRole`, `isSuperAdmin` from JWT
- Add `switchTenant()` method
- Add `register()` method
- Update `currentUser` signal to include tenant context

### Route Guards
- `authGuard` — unchanged (checks authentication)
- `tenantGuard` — new, ensures user has active tenant context
- `superAdminGuard` — new, protects `/admin` routes
- `roleGuard` — updated to check `tenant_role` instead of `role`

### Interceptor Changes
- Auth interceptor — no changes needed (JWT already contains tenant context)

## Testing Strategy

- **Unit tests:** TenantContext, query filter application, auto-set TenantId on save
- **Integration tests:** Cross-tenant data isolation (create data in tenant A, verify invisible from tenant B)
- **Auth tests:** Register flow, switch-tenant, super-admin impersonation
