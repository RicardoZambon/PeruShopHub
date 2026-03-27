# Multi-Tenancy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add multi-tenant SaaS support so multiple shops can use PeruShopHub with fully isolated data, self-service signup, and super-admin management.

**Architecture:** Shared PostgreSQL database with EF Core global query filters for row-level tenant isolation. New `Tenant` and `TenantUser` entities. `ITenantScoped` interface on all data entities. Tenant context resolved from JWT claims via scoped middleware. Super-admin role for platform management.

**Tech Stack:** ASP.NET Core 9, EF Core 9 (PostgreSQL), Angular 17+ (signals, standalone components), JWT auth with tenant claims.

**Spec:** `docs/superpowers/specs/2026-03-27-multi-tenancy-design.md`

---

## File Structure

### New Files — Backend

| File | Responsibility |
|------|---------------|
| `src/PeruShopHub.Core/Entities/Tenant.cs` | Tenant (shop) entity |
| `src/PeruShopHub.Core/Entities/TenantUser.cs` | Join table: user ↔ tenant with role |
| `src/PeruShopHub.Core/Interfaces/ITenantScoped.cs` | Interface marking tenant-scoped entities |
| `src/PeruShopHub.Core/Interfaces/ITenantContext.cs` | Scoped service interface for current tenant |
| `src/PeruShopHub.Infrastructure/Persistence/TenantContext.cs` | ITenantContext implementation |
| `src/PeruShopHub.Infrastructure/Persistence/Configurations/TenantConfiguration.cs` | Tenant EF config |
| `src/PeruShopHub.Infrastructure/Persistence/Configurations/TenantUserConfiguration.cs` | TenantUser EF config |
| `src/PeruShopHub.API/Middleware/TenantMiddleware.cs` | Resolves tenant from JWT claims |
| `src/PeruShopHub.Application/DTOs/Tenant/TenantDtos.cs` | DTOs for tenant operations |
| `src/PeruShopHub.Application/Services/ITenantService.cs` | Tenant service interface |
| `src/PeruShopHub.Application/Services/TenantService.cs` | Tenant CRUD + member management |
| `src/PeruShopHub.API/Controllers/TenantController.cs` | Tenant management endpoints |
| `src/PeruShopHub.API/Controllers/AdminController.cs` | Super-admin endpoints |

### New Files — Frontend

| File | Responsibility |
|------|---------------|
| `src/PeruShopHub.Web/src/app/pages/register/register.component.ts` | Self-service signup page |
| `src/PeruShopHub.Web/src/app/guards/tenant.guard.ts` | Ensures tenant context exists |
| `src/PeruShopHub.Web/src/app/guards/super-admin.guard.ts` | Protects admin routes |
| `src/PeruShopHub.Web/src/app/services/tenant.service.ts` | Tenant API calls |
| `src/PeruShopHub.Web/src/app/pages/admin/admin-tenants.component.ts` | Super-admin tenant list |

### Modified Files — Backend

| File | Changes |
|------|---------|
| `src/PeruShopHub.Core/Entities/SystemUser.cs` | Add `IsSuperAdmin`, remove `Role`, add `TenantMemberships` |
| All 18 tenant-scoped entity files | Add `TenantId` property, implement `ITenantScoped` |
| All 18 entity configuration files | Add `TenantId` column config + index |
| `src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs` | Add DbSets, global query filters, auto-set TenantId |
| `src/PeruShopHub.API/Program.cs` | Register ITenantContext, add TenantMiddleware |
| `src/PeruShopHub.Application/DependencyInjection.cs` | Register ITenantService |
| `src/PeruShopHub.API/Controllers/AuthController.cs` | Add register endpoint, tenant claims in JWT |
| `src/PeruShopHub.Application/DTOs/Auth/AuthDtos.cs` | Update UserDto with tenant info |
| `src/PeruShopHub.Application/Services/UserService.cs` | Tenant-scoped user/member management |
| `src/PeruShopHub.Application/DTOs/Settings/UserDtos.cs` | Update DTOs for tenant roles |
| `src/PeruShopHub.API/Controllers/SettingsController.cs` | Remove user endpoints (moved to TenantController) |
| `src/PeruShopHub.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs` | Update for new fields |

### Modified Files — Frontend

| File | Changes |
|------|---------|
| `src/PeruShopHub.Web/src/app/services/auth.service.ts` | Add tenant context, register, switchTenant |
| `src/PeruShopHub.Web/src/app/models/api.models.ts` | Add tenant interfaces |
| `src/PeruShopHub.Web/src/app/app.routes.ts` | Add register and admin routes |
| `src/PeruShopHub.Web/src/app/shared/components/header/header.component.ts` | Show tenant name |
| `src/PeruShopHub.Web/src/app/shared/components/sidebar/sidebar.component.ts` | Show shop name, admin section |
| `src/PeruShopHub.Web/src/app/pages/settings/settings.component.ts` | Update user management for tenant members |
| `src/PeruShopHub.Web/src/app/pages/login/login.component.ts` | Add register link |

---

## Task 1: Core Domain — ITenantScoped Interface + Tenant Entity

**Files:**
- Create: `src/PeruShopHub.Core/Interfaces/ITenantScoped.cs`
- Create: `src/PeruShopHub.Core/Entities/Tenant.cs`
- Create: `src/PeruShopHub.Core/Entities/TenantUser.cs`
- Create: `src/PeruShopHub.Core/Interfaces/ITenantContext.cs`

- [ ] **Step 1: Create ITenantScoped interface**

```csharp
// src/PeruShopHub.Core/Interfaces/ITenantScoped.cs
namespace PeruShopHub.Core.Interfaces;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
```

- [ ] **Step 2: Create Tenant entity**

```csharp
// src/PeruShopHub.Core/Entities/Tenant.cs
namespace PeruShopHub.Core.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TenantUser> Members { get; set; } = new List<TenantUser>();
}
```

- [ ] **Step 3: Create TenantUser entity**

```csharp
// src/PeruShopHub.Core/Entities/TenantUser.cs
namespace PeruShopHub.Core.Entities;

public class TenantUser
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Viewer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public SystemUser User { get; set; } = null!;
}
```

- [ ] **Step 4: Create ITenantContext interface**

```csharp
// src/PeruShopHub.Core/Interfaces/ITenantContext.cs
namespace PeruShopHub.Core.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsSuperAdmin { get; }
    void Set(Guid? tenantId, bool isSuperAdmin);
}
```

- [ ] **Step 5: Verify it compiles**

Run: `dotnet build src/PeruShopHub.Core/PeruShopHub.Core.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/PeruShopHub.Core/Interfaces/ITenantScoped.cs \
        src/PeruShopHub.Core/Entities/Tenant.cs \
        src/PeruShopHub.Core/Entities/TenantUser.cs \
        src/PeruShopHub.Core/Interfaces/ITenantContext.cs
git commit -m "feat: add Tenant, TenantUser entities and ITenantScoped interface"
```

---

## Task 2: Modify SystemUser + All Tenant-Scoped Entities

**Files:**
- Modify: `src/PeruShopHub.Core/Entities/SystemUser.cs`
- Modify: All 18 entity files listed below

- [ ] **Step 1: Update SystemUser — add IsSuperAdmin, remove Role, add navigation**

Replace the full contents of `src/PeruShopHub.Core/Entities/SystemUser.cs`:

```csharp
namespace PeruShopHub.Core.Entities;

public class SystemUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TenantUser> TenantMemberships { get; set; } = new List<TenantUser>();
}
```

Note: The `Role` property is removed. Roles are now per-tenant in `TenantUser`.

- [ ] **Step 2: Add ITenantScoped to all 18 data entities**

For each entity file below, add `using PeruShopHub.Core.Interfaces;` and `, ITenantScoped` to the class declaration, and add the property `public Guid TenantId { get; set; }`.

Entities to modify (all in `src/PeruShopHub.Core/Entities/`):

1. **Product.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
2. **ProductVariant.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
3. **ProductCostHistory.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
4. **Category.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
5. **VariationField.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
6. **Order.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
7. **OrderItem.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
8. **OrderCost.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
9. **Customer.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
10. **PurchaseOrder.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
11. **PurchaseOrderItem.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
12. **PurchaseOrderCost.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
13. **Supply.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
14. **StockMovement.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
15. **MarketplaceConnection.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
16. **CommissionRule.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
17. **Notification.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`
18. **FileUpload.cs** — add `ITenantScoped` + `public Guid TenantId { get; set; }`

Example for Product.cs — change:
```csharp
namespace PeruShopHub.Core.Entities;

public class Product
{
```
To:
```csharp
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class Product : ITenantScoped
{
    public Guid TenantId { get; set; }
```

Apply the same pattern to all 18 entities.

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build src/PeruShopHub.Core/PeruShopHub.Core.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/PeruShopHub.Core/Entities/
git commit -m "feat: add TenantId to all data entities, remove Role from SystemUser"
```

---

## Task 3: EF Core Configurations — Tenant, TenantUser, TenantId Columns

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- Create: `src/PeruShopHub.Infrastructure/Persistence/Configurations/TenantUserConfiguration.cs`
- Modify: `src/PeruShopHub.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs`
- Modify: All 18 entity configuration files to add TenantId column + index

- [ ] **Step 1: Create TenantConfiguration**

```csharp
// src/PeruShopHub.Infrastructure/Persistence/Configurations/TenantConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(300).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.HasMany(t => t.Members)
            .WithOne(m => m.Tenant)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Create TenantUserConfiguration**

```csharp
// src/PeruShopHub.Infrastructure/Persistence/Configurations/TenantUserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.HasKey(tu => new { tu.TenantId, tu.UserId });

        builder.Property(tu => tu.Role).HasMaxLength(50).IsRequired();

        builder.HasOne(tu => tu.User)
            .WithMany(u => u.TenantMemberships)
            .HasForeignKey(tu => tu.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 3: Update SystemUserConfiguration — remove Role, add IsSuperAdmin**

Replace full contents of `src/PeruShopHub.Infrastructure/Persistence/Configurations/SystemUserConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class SystemUserConfiguration : IEntityTypeConfiguration<SystemUser>
{
    public void Configure(EntityTypeBuilder<SystemUser> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(300).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).HasMaxLength(300).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.RefreshToken).HasMaxLength(200);
    }
}
```

- [ ] **Step 4: Add TenantId config to all 18 entity configurations**

For each configuration file in `src/PeruShopHub.Infrastructure/Persistence/Configurations/`, add inside the `Configure` method:

```csharp
builder.Property(e => e.TenantId).IsRequired();
builder.HasIndex(e => e.TenantId);
```

Files to modify (use `e` or the entity's existing parameter name):

1. `ProductConfiguration.cs` — add after `builder.Property(p => p.Version).IsConcurrencyToken();`
2. `ProductVariantConfiguration.cs` — add at end of Configure method
3. `ProductCostHistoryConfiguration.cs` — add at end
4. `CategoryConfiguration.cs` — add at end
5. `VariationFieldConfiguration.cs` — add at end
6. `OrderConfiguration.cs` — add at end
7. `OrderItemConfiguration.cs` — add at end
8. `OrderCostConfiguration.cs` — add at end
9. `CustomerConfiguration.cs` — add at end
10. `PurchaseOrderConfiguration.cs` — add at end
11. `PurchaseOrderItemConfiguration.cs` — add at end
12. `PurchaseOrderCostConfiguration.cs` — add at end
13. `SupplyConfiguration.cs` — add at end
14. `StockMovementConfiguration.cs` — add at end
15. `MarketplaceConnectionConfiguration.cs` — add at end
16. `CommissionRuleConfiguration.cs` — add at end
17. `NotificationConfiguration.cs` — add at end
18. `FileUploadConfiguration.cs` — add at end

The property name matches each entity's parameter (`p`, `o`, `c`, etc.) — use whatever the existing builder parameter name is. Example for ProductConfiguration.cs where parameter is `p`:

```csharp
builder.Property(p => p.TenantId).IsRequired();
builder.HasIndex(p => p.TenantId);
```

- [ ] **Step 5: Verify it compiles**

Run: `dotnet build src/PeruShopHub.Infrastructure/PeruShopHub.Infrastructure.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/PeruShopHub.Infrastructure/Persistence/Configurations/
git commit -m "feat: add EF Core configurations for Tenant, TenantUser, and TenantId columns"
```

---

## Task 4: DbContext — Global Query Filters + Auto-Set TenantId

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Persistence/TenantContext.cs`
- Modify: `src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs`

- [ ] **Step 1: Create TenantContext implementation**

```csharp
// src/PeruShopHub.Infrastructure/Persistence/TenantContext.cs
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Persistence;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public bool IsSuperAdmin { get; private set; }

    public void Set(Guid? tenantId, bool isSuperAdmin)
    {
        TenantId = tenantId;
        IsSuperAdmin = isSuperAdmin;
    }
}
```

- [ ] **Step 2: Update DbContext with query filters and auto-set**

Replace full contents of `src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs`:

```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Persistence;

public class PeruShopHubDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public PeruShopHubDbContext(DbContextOptions<PeruShopHubDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderCost> OrderCosts => Set<OrderCost>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    public DbSet<MarketplaceConnection> MarketplaceConnections => Set<MarketplaceConnection>();
    public DbSet<FileUpload> FileUploads => Set<FileUpload>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseOrderCost> PurchaseOrderCosts => Set<PurchaseOrderCost>();
    public DbSet<ProductCostHistory> ProductCostHistories => Set<ProductCostHistory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<VariationField> VariationFields => Set<VariationField>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PeruShopHubDbContext).Assembly);

        // Apply global query filters to all ITenantScoped entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(PeruShopHubDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _tenantContext == null ||
            _tenantContext.IsSuperAdmin ||
            e.TenantId == _tenantContext.TenantId);
    }

    public override int SaveChanges()
    {
        SetTenantIdOnNewEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdOnNewEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTenantIdOnNewEntities()
    {
        if (_tenantContext?.TenantId is null) return;

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId == Guid.Empty))
        {
            entry.Entity.TenantId = _tenantContext.TenantId.Value;
        }
    }
}
```

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build src/PeruShopHub.Infrastructure/PeruShopHub.Infrastructure.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/PeruShopHub.Infrastructure/Persistence/TenantContext.cs \
        src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs
git commit -m "feat: add global query filters and auto-set TenantId in DbContext"
```

---

## Task 5: EF Core Migration — Add Multi-Tenancy Schema

**Files:**
- Generate: `src/PeruShopHub.Infrastructure/Migrations/` (new migration files)

- [ ] **Step 1: Generate the migration**

Run:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub
dotnet ef migrations add AddMultiTenancy \
  --project src/PeruShopHub.Infrastructure \
  --startup-project src/PeruShopHub.API
```

Expected: Migration files created in `src/PeruShopHub.Infrastructure/Migrations/`

- [ ] **Step 2: Review the generated migration**

Open the generated migration file and verify it includes:
- `CreateTable` for `Tenants` (Id, Name, Slug, IsActive, CreatedAt)
- `CreateTable` for `TenantUsers` (TenantId, UserId, Role, CreatedAt — composite PK)
- `AddColumn` for `IsSuperAdmin` on `SystemUsers`
- `DropColumn` for `Role` on `SystemUsers`
- `AddColumn` for `TenantId` on all 18 entity tables
- Unique index on `Tenants.Slug`
- Index on `TenantId` for each entity table
- Foreign keys from `TenantUsers` to both `Tenants` and `SystemUsers`

If the migration doesn't include the data migration (it won't — EF only generates schema), that's expected. Data migration will be in the next step.

- [ ] **Step 3: Add data migration SQL to the Up method**

After the schema changes in the generated migration's `Up` method, add SQL to migrate existing data. Insert this BEFORE making TenantId non-nullable:

The migration should:
1. Create `Tenants` table first
2. Insert the demo tenant
3. Add nullable `TenantId` columns
4. Populate all TenantId columns with demo tenant ID
5. Make `TenantId` non-nullable
6. Add `IsSuperAdmin`, set admin user to super-admin
7. Create `TenantUsers` for existing users
8. Drop `Role` from `SystemUsers`

If the auto-generated migration creates TenantId as non-nullable, manually split it:
- First add as nullable
- Then `migrationBuilder.Sql(...)` to populate
- Then `AlterColumn` to make non-nullable

Key SQL statements to add:

```csharp
// Insert demo tenant
migrationBuilder.Sql(@"
    INSERT INTO ""Tenants"" (""Id"", ""Name"", ""Slug"", ""IsActive"", ""CreatedAt"")
    VALUES ('a0000000-0000-0000-0000-000000000001', 'Demo Shop', 'demo-shop', true, NOW());
");

// Set all existing data to demo tenant
var tables = new[] {
    "Products", "ProductVariants", "ProductCostHistories", "Categories",
    "VariationFields", "Orders", "OrderItems", "OrderCosts", "Customers",
    "PurchaseOrders", "PurchaseOrderItems", "PurchaseOrderCosts",
    "Supplies", "StockMovements", "MarketplaceConnections", "CommissionRules",
    "Notifications", "FileUploads"
};
foreach (var table in tables)
{
    migrationBuilder.Sql($@"
        UPDATE ""{table}"" SET ""TenantId"" = 'a0000000-0000-0000-0000-000000000001';
    ");
}

// Promote admin to super-admin
migrationBuilder.Sql(@"
    UPDATE ""SystemUsers"" SET ""IsSuperAdmin"" = true
    WHERE ""Email"" = 'admin@perushophub.com';
");

// Create TenantUser records for all existing users
migrationBuilder.Sql(@"
    INSERT INTO ""TenantUsers"" (""TenantId"", ""UserId"", ""Role"", ""CreatedAt"")
    SELECT 'a0000000-0000-0000-0000-000000000001', ""Id"",
        CASE ""Role""
            WHEN 'Admin' THEN 'Owner'
            WHEN 'Manager' THEN 'Manager'
            ELSE 'Viewer'
        END,
        NOW()
    FROM ""SystemUsers"";
");
```

- [ ] **Step 4: Verify migration compiles**

Run: `dotnet build src/PeruShopHub.Infrastructure/PeruShopHub.Infrastructure.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Infrastructure/Migrations/
git commit -m "feat: add multi-tenancy migration with data migration for existing records"
```

---

## Task 6: Tenant Middleware + DI Registration

**Files:**
- Create: `src/PeruShopHub.API/Middleware/TenantMiddleware.cs`
- Modify: `src/PeruShopHub.API/Program.cs`
- Modify: `src/PeruShopHub.Application/DependencyInjection.cs`

- [ ] **Step 1: Create TenantMiddleware**

```csharp
// src/PeruShopHub.API/Middleware/TenantMiddleware.cs
using System.Security.Claims;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/health"
    };

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip for unauthenticated endpoints
        if (SkipPaths.Contains(path) || path.StartsWith("/hubs/") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var isSuperAdmin = user.FindFirstValue("is_super_admin") == "true";
        Guid? tenantId = null;

        if (isSuperAdmin)
        {
            // Super-admin can impersonate via header
            var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (Guid.TryParse(headerTenantId, out var headerGuid))
            {
                tenantId = headerGuid;
            }
            else
            {
                // Check JWT claim as fallback
                var claimTenantId = user.FindFirstValue("tenant_id");
                if (Guid.TryParse(claimTenantId, out var claimGuid))
                    tenantId = claimGuid;
            }
        }
        else
        {
            var claimTenantId = user.FindFirstValue("tenant_id");
            if (Guid.TryParse(claimTenantId, out var claimGuid))
            {
                tenantId = claimGuid;
            }

            // Non-super-admin must have a tenant on tenant-scoped endpoints
            if (tenantId is null && !path.StartsWith("/api/auth/"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Contexto de tenant ausente." });
                return;
            }
        }

        tenantContext.Set(tenantId, isSuperAdmin);
        await _next(context);
    }
}
```

- [ ] **Step 2: Update Program.cs — register services and add middleware**

In `src/PeruShopHub.API/Program.cs`, add these registrations:

After the `AddApplicationServices()` line, add:
```csharp
// ── Tenant Context ──────────────────────────────────────
builder.Services.AddScoped<ITenantContext, TenantContext>();
```

Add the using statements at the top:
```csharp
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.API.Middleware;
```

In the middleware pipeline, add `app.UseMiddleware<TenantMiddleware>();` AFTER authentication/authorization and BEFORE `app.MapControllers()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

app.MapControllers();
```

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build src/PeruShopHub.API/PeruShopHub.API.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/PeruShopHub.API/Middleware/TenantMiddleware.cs \
        src/PeruShopHub.API/Program.cs
git commit -m "feat: add tenant resolution middleware and DI registration"
```

---

## Task 7: Auth Changes — Register, JWT Claims, Updated DTOs

**Files:**
- Modify: `src/PeruShopHub.Application/DTOs/Auth/AuthDtos.cs`
- Modify: `src/PeruShopHub.Application/DTOs/Settings/UserDtos.cs`
- Create: `src/PeruShopHub.Application/DTOs/Tenant/TenantDtos.cs`
- Modify: `src/PeruShopHub.API/Controllers/AuthController.cs`

- [ ] **Step 1: Update AuthDtos — add tenant context to UserDto**

Replace full contents of `src/PeruShopHub.Application/DTOs/Auth/AuthDtos.cs`:

```csharp
namespace PeruShopHub.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string ShopName, string Name, string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record SwitchTenantRequest(Guid TenantId);

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? TenantRole,
    Guid? TenantId,
    string? TenantName,
    bool IsSuperAdmin);

public record TenantSummaryDto(Guid Id, string Name, string Slug, string Role);
```

- [ ] **Step 2: Update UserDtos — change Role to TenantRole**

Replace full contents of `src/PeruShopHub.Application/DTOs/Settings/UserDtos.cs`:

```csharp
namespace PeruShopHub.Application.DTOs.Settings;

public record UserDetailDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLogin,
    DateTime CreatedAt);

public record CreateUserRequest(string Name, string Email, string Password, string Role);

public record UpdateUserRequest(string Name, string Email, string Role);

public record ResetPasswordRequest(string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
```

Note: `UserDetailDto` keeps `Role` because it's used for tenant member management — the role comes from `TenantUser.Role`.

- [ ] **Step 3: Create TenantDtos**

```csharp
// src/PeruShopHub.Application/DTOs/Tenant/TenantDtos.cs
namespace PeruShopHub.Application.DTOs.Tenant;

public record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    int MemberCount,
    DateTime CreatedAt);

public record UpdateTenantRequest(string Name);

public record InviteMemberRequest(string Email, string Role);

public record UpdateMemberRoleRequest(string Role);
```

- [ ] **Step 4: Rewrite AuthController with tenant-aware JWT and register endpoint**

Replace full contents of `src/PeruShopHub.API/Controllers/AuthController.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly IConfiguration _config;
    private readonly IUserService _userService;

    public AuthController(PeruShopHubDbContext db, IConfiguration config, IUserService userService)
    {
        _db = db;
        _config = config;
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.SystemUsers
            .Include(u => u.TenantMemberships)
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "E-mail ou senha incorretos." });

        // Pick the first tenant membership (or none for super-admin without memberships)
        var membership = user.TenantMemberships
            .FirstOrDefault(m => m.Tenant.IsActive);

        var accessToken = GenerateAccessToken(user, membership);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken,
            refreshToken,
            BuildUserDto(user, membership)));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.ShopName))
            AddError(errors, "ShopName", "Nome da loja é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!Regex.IsMatch(request.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            AddError(errors, "Email", "E-mail inválido.");
        else if (await _db.SystemUsers.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant()))
            AddError(errors, "Email", "E-mail já está em uso.");

        if (string.IsNullOrWhiteSpace(request.Password))
            AddError(errors, "Password", "Senha é obrigatória.");
        else if (request.Password.Length < 8)
            AddError(errors, "Password", "Senha deve ter no mínimo 8 caracteres.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var slug = GenerateSlug(request.ShopName);

        // Ensure slug uniqueness
        var baseSlug = slug;
        var counter = 1;
        while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.ShopName.Trim(),
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new SystemUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsSuperAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var membership = new TenantUser
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow,
            Tenant = tenant,
            User = user
        };

        _db.Tenants.Add(tenant);
        _db.SystemUsers.Add(user);
        _db.TenantUsers.Add(membership);

        var accessToken = GenerateAccessToken(user, membership);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        user.LastLogin = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Created("", new AuthResponse(
            accessToken,
            refreshToken,
            BuildUserDto(user, membership)));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var user = await _db.SystemUsers
            .Include(u => u.TenantMemberships)
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken && u.IsActive);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Token expirado. Faça login novamente." });

        var membership = user.TenantMemberships
            .FirstOrDefault(m => m.Tenant.IsActive);

        var accessToken = GenerateAccessToken(user, membership);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken,
            newRefreshToken,
            BuildUserDto(user, membership)));
    }

    [Authorize]
    [HttpGet("tenants")]
    public async Task<ActionResult<List<TenantSummaryDto>>> GetMyTenants()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tenants = await _db.TenantUsers
            .Include(tu => tu.Tenant)
            .Where(tu => tu.UserId == userId && tu.Tenant.IsActive)
            .Select(tu => new TenantSummaryDto(tu.TenantId, tu.Tenant.Name, tu.Tenant.Slug, tu.Role))
            .ToListAsync();

        return Ok(tenants);
    }

    [Authorize]
    [HttpPost("switch-tenant")]
    public async Task<ActionResult<AuthResponse>> SwitchTenant([FromBody] SwitchTenantRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await _db.SystemUsers
            .Include(u => u.TenantMemberships)
                .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user is null)
            return Unauthorized(new { message = "Usuário não encontrado." });

        var membership = user.TenantMemberships
            .FirstOrDefault(m => m.TenantId == request.TenantId && m.Tenant.IsActive);

        if (membership is null && !user.IsSuperAdmin)
            return Forbid();

        // For super-admin without membership, create a virtual admin context
        if (membership is null && user.IsSuperAdmin)
        {
            var tenant = await _db.Tenants.FindAsync(request.TenantId);
            if (tenant is null) return NotFound();
            membership = new TenantUser
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = "Admin",
                Tenant = tenant,
                User = user
            };
        }

        var accessToken = GenerateAccessToken(user, membership);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(
            _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(
            accessToken,
            refreshToken,
            BuildUserDto(user, membership)));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userId, out var id))
        {
            var user = await _db.SystemUsers.FindAsync(id);
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiresAt = null;
                await _db.SaveChangesAsync();
            }
        }
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var tenantId = User.FindFirstValue("tenant_id");
        return Ok(new UserDto(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue("name") ?? "",
            User.FindFirstValue(ClaimTypes.Email) ?? "",
            User.FindFirstValue("tenant_role"),
            tenantId is not null ? Guid.Parse(tenantId) : null,
            User.FindFirstValue("tenant_name"),
            User.FindFirstValue("is_super_admin") == "true"));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.ChangePasswordAsync(userId, request, ct);
        return NoContent();
    }

    private string GenerateAccessToken(SystemUser user, TenantUser? membership)
    {
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("name", user.Name),
            new("is_super_admin", user.IsSuperAdmin.ToString().ToLowerInvariant()),
        };

        if (membership is not null)
        {
            claims.Add(new Claim("tenant_id", membership.TenantId.ToString()));
            claims.Add(new Claim("tenant_role", membership.Role));
            claims.Add(new Claim("tenant_name", membership.Tenant?.Name ?? ""));
            claims.Add(new Claim(ClaimTypes.Role, membership.Role));
        }

        if (user.IsSuperAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }

        var expMinutes = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto BuildUserDto(SystemUser user, TenantUser? membership)
    {
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            membership?.Role,
            membership?.TenantId,
            membership?.Tenant?.Name,
            user.IsSuperAdmin);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[áàãâä]", "a");
        slug = Regex.Replace(slug, @"[éèêë]", "e");
        slug = Regex.Replace(slug, @"[íìîï]", "i");
        slug = Regex.Replace(slug, @"[óòõôö]", "o");
        slug = Regex.Replace(slug, @"[úùûü]", "u");
        slug = Regex.Replace(slug, @"[ç]", "c");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }
}
```

- [ ] **Step 5: Verify it compiles**

Run: `dotnet build src/PeruShopHub.API/PeruShopHub.API.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/PeruShopHub.Application/DTOs/Auth/AuthDtos.cs \
        src/PeruShopHub.Application/DTOs/Settings/UserDtos.cs \
        src/PeruShopHub.Application/DTOs/Tenant/ \
        src/PeruShopHub.API/Controllers/AuthController.cs
git commit -m "feat: tenant-aware auth with register, switch-tenant, and updated JWT claims"
```

---

## Task 8: Update UserService for Tenant-Scoped Members

**Files:**
- Modify: `src/PeruShopHub.Application/Services/IUserService.cs`
- Modify: `src/PeruShopHub.Application/Services/UserService.cs`

- [ ] **Step 1: Update IUserService**

Replace full contents of `src/PeruShopHub.Application/Services/IUserService.cs`:

```csharp
using PeruShopHub.Application.DTOs.Settings;

namespace PeruShopHub.Application.Services;

public interface IUserService
{
    // Tenant-scoped member operations
    Task<IReadOnlyList<UserDetailDto>> GetTenantMembersAsync(Guid tenantId, CancellationToken ct = default);
    Task<UserDetailDto> InviteMemberAsync(Guid tenantId, CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetailDto> UpdateMemberAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}
```

- [ ] **Step 2: Rewrite UserService for tenant-scoped operations**

Replace full contents of `src/PeruShopHub.Application/Services/UserService.cs`:

```csharp
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class UserService : IUserService
{
    private readonly PeruShopHubDbContext _db;

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Manager", "Viewer"
    };

    public UserService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDetailDto>> GetTenantMembersAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.TenantUsers
            .AsNoTracking()
            .Include(tu => tu.User)
            .Where(tu => tu.TenantId == tenantId)
            .OrderBy(tu => tu.User.Name)
            .Select(tu => new UserDetailDto(
                tu.UserId, tu.User.Name, tu.User.Email, tu.Role,
                tu.User.IsActive, tu.User.LastLogin, tu.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<UserDetailDto> InviteMemberAsync(Guid tenantId, CreateUserRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.Email))
            AddError(errors, "Email", "E-mail inválido.");

        if (string.IsNullOrWhiteSpace(request.Password))
            AddError(errors, "Password", "Senha é obrigatória.");
        else if (request.Password.Length < 8)
            AddError(errors, "Password", "Senha deve ter no mínimo 8 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Role))
            AddError(errors, "Role", "Perfil é obrigatório.");
        else if (!ValidRoles.Contains(request.Role))
            AddError(errors, "Role", "Perfil deve ser Admin, Manager ou Viewer.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var email = request.Email.Trim().ToLowerInvariant();

        // Check if user already exists
        var existingUser = await _db.SystemUsers.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (existingUser is not null)
        {
            // Check if already a member of this tenant
            var existingMembership = await _db.TenantUsers
                .AnyAsync(tu => tu.TenantId == tenantId && tu.UserId == existingUser.Id, ct);

            if (existingMembership)
                throw new AppValidationException("Email", "Usuário já é membro desta loja.");

            // Add to tenant
            var membership = new TenantUser
            {
                TenantId = tenantId,
                UserId = existingUser.Id,
                Role = NormalizeRole(request.Role),
                CreatedAt = DateTime.UtcNow
            };
            _db.TenantUsers.Add(membership);
            await _db.SaveChangesAsync(ct);

            return new UserDetailDto(
                existingUser.Id, existingUser.Name, existingUser.Email,
                membership.Role, existingUser.IsActive, existingUser.LastLogin, membership.CreatedAt);
        }

        // Create new user + membership
        var user = new SystemUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsSuperAdmin = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var newMembership = new TenantUser
        {
            TenantId = tenantId,
            UserId = user.Id,
            Role = NormalizeRole(request.Role),
            CreatedAt = DateTime.UtcNow
        };

        _db.SystemUsers.Add(user);
        _db.TenantUsers.Add(newMembership);
        await _db.SaveChangesAsync(ct);

        return new UserDetailDto(
            user.Id, user.Name, user.Email,
            newMembership.Role, user.IsActive, user.LastLogin, newMembership.CreatedAt);
    }

    public async Task<UserDetailDto> UpdateMemberAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var membership = await _db.TenantUsers
            .Include(tu => tu.User)
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId, ct)
            ?? throw new NotFoundException("Membro", userId);

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(errors, "Name", "Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            AddError(errors, "Email", "E-mail é obrigatório.");
        else if (!IsValidEmail(request.Email))
            AddError(errors, "Email", "E-mail inválido.");
        else if (await _db.SystemUsers.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant() && u.Id != userId, ct))
            AddError(errors, "Email", "E-mail já está em uso.");

        if (string.IsNullOrWhiteSpace(request.Role))
            AddError(errors, "Role", "Perfil é obrigatório.");
        else if (!ValidRoles.Contains(request.Role) && !request.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            AddError(errors, "Role", "Perfil deve ser Owner, Admin, Manager ou Viewer.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        membership.User.Name = request.Name.Trim();
        membership.User.Email = request.Email.Trim().ToLowerInvariant();
        membership.Role = NormalizeRole(request.Role);

        await _db.SaveChangesAsync(ct);

        return new UserDetailDto(
            membership.UserId, membership.User.Name, membership.User.Email,
            membership.Role, membership.User.IsActive, membership.User.LastLogin, membership.CreatedAt);
    }

    public async Task RemoveMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var membership = await _db.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenantId && tu.UserId == userId, ct)
            ?? throw new NotFoundException("Membro", userId);

        if (membership.Role == "Owner")
            throw new AppValidationException("Role", "Não é possível remover o proprietário da loja.");

        _db.TenantUsers.Remove(membership);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { id }, ct)
            ?? throw new AppValidationException("Id", "Usuário não encontrado.");

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            AddError(errors, "NewPassword", "Nova senha é obrigatória.");
        else if (request.NewPassword.Length < 8)
            AddError(errors, "NewPassword", "Nova senha deve ter no mínimo 8 caracteres.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.SystemUsers.FindAsync(new object[] { userId }, ct)
            ?? throw new AppValidationException("Id", "Usuário não encontrado.");

        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            AddError(errors, "CurrentPassword", "Senha atual é obrigatória.");
        else if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            AddError(errors, "CurrentPassword", "Senha atual incorreta.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            AddError(errors, "NewPassword", "Nova senha é obrigatória.");
        else if (request.NewPassword.Length < 8)
            AddError(errors, "NewPassword", "Nova senha deve ter no mínimo 8 caracteres.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _db.SaveChangesAsync(ct);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim() switch
        {
            var r when r.Equals("Owner", StringComparison.OrdinalIgnoreCase) => "Owner",
            var r when r.Equals("Admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            var r when r.Equals("Manager", StringComparison.OrdinalIgnoreCase) => "Manager",
            var r when r.Equals("Viewer", StringComparison.OrdinalIgnoreCase) => "Viewer",
            _ => role.Trim()
        };
    }
}
```

- [ ] **Step 3: Verify it compiles**

Run: `dotnet build src/PeruShopHub.Application/PeruShopHub.Application.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/PeruShopHub.Application/Services/IUserService.cs \
        src/PeruShopHub.Application/Services/UserService.cs
git commit -m "feat: rewrite UserService for tenant-scoped member management"
```

---

## Task 9: Tenant Management Controller + DI

**Files:**
- Create: `src/PeruShopHub.Application/Services/ITenantService.cs`
- Create: `src/PeruShopHub.Application/Services/TenantService.cs`
- Create: `src/PeruShopHub.API/Controllers/TenantController.cs`
- Create: `src/PeruShopHub.API/Controllers/AdminController.cs`
- Modify: `src/PeruShopHub.Application/DependencyInjection.cs`
- Modify: `src/PeruShopHub.API/Controllers/SettingsController.cs`

- [ ] **Step 1: Create ITenantService**

```csharp
// src/PeruShopHub.Application/Services/ITenantService.cs
using PeruShopHub.Application.DTOs.Tenant;

namespace PeruShopHub.Application.Services;

public interface ITenantService
{
    Task<TenantDetailDto> GetByIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantDetailDto> UpdateAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken ct = default);

    // Super-admin operations
    Task<IReadOnlyList<TenantDetailDto>> GetAllAsync(CancellationToken ct = default);
    Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create TenantService**

```csharp
// src/PeruShopHub.Application/Services/TenantService.cs
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class TenantService : ITenantService
{
    private readonly PeruShopHubDbContext _db;

    public TenantService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<TenantDetailDto> GetByIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantDetailDto(
                t.Id, t.Name, t.Slug, t.IsActive,
                t.Members.Count, t.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        return tenant;
    }

    public async Task<TenantDetailDto> UpdateAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException("Name", "Nome da loja é obrigatório.");

        tenant.Name = request.Name.Trim();
        await _db.SaveChangesAsync(ct);

        var memberCount = await _db.TenantUsers.CountAsync(tu => tu.TenantId == tenantId, ct);
        return new TenantDetailDto(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, memberCount, tenant.CreatedAt);
    }

    public async Task<IReadOnlyList<TenantDetailDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantDetailDto(
                t.Id, t.Name, t.Slug, t.IsActive,
                t.Members.Count, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync(new object[] { tenantId }, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        tenant.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Create TenantController**

```csharp
// src/PeruShopHub.API/Controllers/TenantController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;
    private readonly ITenantContext _tenantContext;

    public TenantController(ITenantService tenantService, IUserService userService, ITenantContext tenantContext)
    {
        _tenantService = tenantService;
        _userService = userService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<TenantDetailDto>> Get(CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _tenantService.GetByIdAsync(_tenantContext.TenantId.Value, ct));
    }

    [HttpPut]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<TenantDetailDto>> Update([FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _tenantService.UpdateAsync(_tenantContext.TenantId.Value, request, ct));
    }

    [HttpGet("members")]
    public async Task<ActionResult<IReadOnlyList<UserDetailDto>>> GetMembers(CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _userService.GetTenantMembersAsync(_tenantContext.TenantId.Value, ct));
    }

    [HttpPost("members/invite")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<UserDetailDto>> InviteMember([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        var member = await _userService.InviteMemberAsync(_tenantContext.TenantId.Value, request, ct);
        return Created("", member);
    }

    [HttpPut("members/{userId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<UserDetailDto>> UpdateMember(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        return Ok(await _userService.UpdateMemberAsync(_tenantContext.TenantId.Value, userId, request, ct));
    }

    [HttpDelete("members/{userId:guid}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> RemoveMember(Guid userId, CancellationToken ct)
    {
        if (_tenantContext.TenantId is null) return Forbid();
        await _userService.RemoveMemberAsync(_tenantContext.TenantId.Value, userId, ct);
        return NoContent();
    }

    [HttpPost("members/{userId:guid}/reset-password")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _userService.ResetPasswordAsync(userId, request, ct);
        return NoContent();
    }
}
```

- [ ] **Step 4: Create AdminController**

```csharp
// src/PeruShopHub.API/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public AdminController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<TenantDetailDto>>> GetTenants(CancellationToken ct)
    {
        return Ok(await _tenantService.GetAllAsync(ct));
    }

    [HttpGet("tenants/{id:guid}")]
    public async Task<ActionResult<TenantDetailDto>> GetTenant(Guid id, CancellationToken ct)
    {
        return Ok(await _tenantService.GetByIdAsync(id, ct));
    }

    [HttpPut("tenants/{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken ct)
    {
        await _tenantService.SetActiveAsync(id, true, ct);
        return NoContent();
    }

    [HttpPut("tenants/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken ct)
    {
        await _tenantService.SetActiveAsync(id, false, ct);
        return NoContent();
    }
}
```

- [ ] **Step 5: Update DependencyInjection.cs — register TenantService**

In `src/PeruShopHub.Application/DependencyInjection.cs`, add:

```csharp
services.AddScoped<ITenantService, TenantService>();
```

after the existing `services.AddScoped<INotificationService, NotificationService>();` line.

- [ ] **Step 6: Update SettingsController — remove user management endpoints**

In `src/PeruShopHub.API/Controllers/SettingsController.cs`, remove all user-related methods: `GetUsers`, `GetUser`, `CreateUser`, `UpdateUser`, `DeactivateUser`, `ResetPassword`. Keep the integrations, costs, and commission rule methods.

Also remove the `IUserService _userService` dependency from the constructor and field (it's only used for user management).

Update the class-level authorization from `[Authorize(Roles = "Admin")]` to `[Authorize(Roles = "Owner,Admin")]` since the role names changed.

- [ ] **Step 7: Verify full solution compiles**

Run: `dotnet build`
Expected: Build succeeded (may have warnings from other services referencing `user.Role` — fix in next step)

- [ ] **Step 8: Fix any remaining compilation errors**

Any service that previously read `SystemUser.Role` must be updated. Common places:
- Services projecting `UserDetailDto` from `SystemUser.Role` — these now need to join through `TenantUser`
- Any `[Authorize(Roles = "Admin")]` attributes should be updated to `[Authorize(Roles = "Owner,Admin")]` or similar

Run: `dotnet build` until build succeeds

- [ ] **Step 9: Commit**

```bash
git add src/PeruShopHub.Application/Services/ITenantService.cs \
        src/PeruShopHub.Application/Services/TenantService.cs \
        src/PeruShopHub.API/Controllers/TenantController.cs \
        src/PeruShopHub.API/Controllers/AdminController.cs \
        src/PeruShopHub.Application/DependencyInjection.cs \
        src/PeruShopHub.API/Controllers/SettingsController.cs
git commit -m "feat: add TenantController, AdminController, and TenantService"
```

---

## Task 10: Update Remaining Controllers — Role Authorization

**Files:**
- Modify: All controllers in `src/PeruShopHub.API/Controllers/` that use `[Authorize(Roles = "Admin,Manager")]` or `[Authorize(Roles = "Admin")]`

- [ ] **Step 1: Search for Authorize attributes referencing old roles**

Run: `grep -rn 'Authorize.*Roles' src/PeruShopHub.API/Controllers/`

For each file found, update:
- `[Authorize(Roles = "Admin")]` → `[Authorize(Roles = "Owner,Admin")]`
- `[Authorize(Roles = "Admin,Manager")]` → `[Authorize(Roles = "Owner,Admin,Manager")]`
- `[Authorize]` → leave as-is (any authenticated user)

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build src/PeruShopHub.API/PeruShopHub.API.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.API/Controllers/
git commit -m "feat: update controller authorization for tenant roles"
```

---

## Task 11: Update Seed Data

**Files:**
- Modify: `src/PeruShopHub.Infrastructure/Persistence/Seeds/SeedData.sql`

- [ ] **Step 1: Update SeedData.sql**

At the beginning of the file (after any existing comments), add:

```sql
-- Tenants
INSERT INTO "Tenants" ("Id", "Name", "Slug", "IsActive", "CreatedAt")
VALUES ('a0000000-0000-0000-0000-000000000001', 'Demo Shop', 'demo-shop', true, NOW())
ON CONFLICT DO NOTHING;
```

For every existing INSERT statement in the file, add `"TenantId"` column and `'a0000000-0000-0000-0000-000000000001'` value to each row.

Update the SystemUsers INSERT statements:
- Remove `"Role"` column
- Add `"IsSuperAdmin"` column (true for admin, false for others)

After the SystemUsers INSERTs, add TenantUser records:

```sql
-- TenantUsers
INSERT INTO "TenantUsers" ("TenantId", "UserId", "Role", "CreatedAt")
VALUES
  ('a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'Owner', NOW()),
  ('a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'Manager', NOW()),
  ('a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'Viewer', NOW())
ON CONFLICT DO NOTHING;
```

(Use the actual user IDs from the existing seed data.)

- [ ] **Step 2: Verify SQL syntax**

Run: `dotnet build` (seed data is embedded resource, build will catch gross errors)

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Infrastructure/Persistence/Seeds/SeedData.sql
git commit -m "feat: update seed data with tenant and TenantUser records"
```

---

## Task 12: Frontend — Update Auth Service + Models

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/services/auth.service.ts`
- Modify: `src/PeruShopHub.Web/src/app/models/api.models.ts`

- [ ] **Step 1: Update AuthUser and AuthResponse interfaces + add register/switchTenant**

Replace full contents of `src/PeruShopHub.Web/src/app/services/auth.service.ts`:

```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  tenantRole: string | null;
  tenantId: string | null;
  tenantName: string | null;
  isSuperAdmin: boolean;
}

export interface TenantSummary {
  id: string;
  name: string;
  slug: string;
  role: string;
}

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

const TOKEN_KEY = 'psh_access_token';
const REFRESH_KEY = 'psh_refresh_token';
const USER_KEY = 'psh_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  readonly currentUser = signal<AuthUser | null>(this.loadStoredUser());

  readonly isSuperAdmin = computed(() => this.currentUser()?.isSuperAdmin ?? false);
  readonly tenantName = computed(() => this.currentUser()?.tenantName ?? '');
  readonly tenantRole = computed(() => this.currentUser()?.tenantRole ?? '');
  readonly hasTenant = computed(() => !!this.currentUser()?.tenantId);

  private refreshPromise: Promise<string> | null = null;

  get accessToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  async login(email: string, password: string): Promise<AuthUser> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email, password })
    );
    this.storeTokens(res);
    return res.user;
  }

  async register(shopName: string, name: string, email: string, password: string): Promise<AuthUser> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/register`, { shopName, name, email, password })
    );
    this.storeTokens(res);
    return res.user;
  }

  async getMyTenants(): Promise<TenantSummary[]> {
    return await firstValueFrom(
      this.http.get<TenantSummary[]>(`${this.baseUrl}/tenants`)
    );
  }

  async switchTenant(tenantId: string): Promise<AuthUser> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/switch-tenant`, { tenantId })
    );
    this.storeTokens(res);
    return res.user;
  }

  async refreshAccessToken(): Promise<string> {
    if (this.refreshPromise) return this.refreshPromise;

    const token = this.refreshToken;
    if (!token) {
      this.logout();
      throw new Error('No refresh token');
    }

    this.refreshPromise = firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken: token })
    ).then(res => {
      this.storeTokens(res);
      return res.accessToken;
    }).catch(err => {
      this.logout();
      throw err;
    }).finally(() => {
      this.refreshPromise = null;
    });

    return this.refreshPromise;
  }

  logout(): void {
    const token = this.accessToken;
    if (token) {
      this.http.post(`${this.baseUrl}/logout`, {}).subscribe({ error: () => {} });
    }
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private storeTokens(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(REFRESH_KEY, res.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this.currentUser.set(res.user);
  }

  private loadStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }
}
```

- [ ] **Step 2: Verify frontend compiles**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: Build should succeed or show only unrelated warnings

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/services/auth.service.ts
git commit -m "feat: update frontend AuthService with tenant context, register, switchTenant"
```

---

## Task 13: Frontend — Register Page

**Files:**
- Create: `src/PeruShopHub.Web/src/app/pages/register/register.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/app.routes.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/login/login.component.ts`

- [ ] **Step 1: Create register component**

```typescript
// src/PeruShopHub.Web/src/app/pages/register/register.component.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-header">
          <h1>PeruShopHub</h1>
          <p>Crie sua loja e comece a vender</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="form-field">
            <label for="shopName">Nome da Loja</label>
            <input id="shopName" formControlName="shopName" placeholder="Minha Loja" />
            @if (form.get('shopName')?.touched && form.get('shopName')?.hasError('required')) {
              <span class="error">Nome da loja é obrigatório</span>
            }
          </div>

          <div class="form-field">
            <label for="name">Seu Nome</label>
            <input id="name" formControlName="name" placeholder="João Silva" />
            @if (form.get('name')?.touched && form.get('name')?.hasError('required')) {
              <span class="error">Nome é obrigatório</span>
            }
          </div>

          <div class="form-field">
            <label for="email">E-mail</label>
            <input id="email" type="email" formControlName="email" placeholder="joao@exemplo.com" />
            @if (form.get('email')?.touched && form.get('email')?.hasError('required')) {
              <span class="error">E-mail é obrigatório</span>
            }
            @if (form.get('email')?.touched && form.get('email')?.hasError('email')) {
              <span class="error">E-mail inválido</span>
            }
          </div>

          <div class="form-field">
            <label for="password">Senha</label>
            <input id="password" type="password" formControlName="password" placeholder="Mínimo 8 caracteres" />
            @if (form.get('password')?.touched && form.get('password')?.hasError('required')) {
              <span class="error">Senha é obrigatória</span>
            }
            @if (form.get('password')?.touched && form.get('password')?.hasError('minlength')) {
              <span class="error">Senha deve ter no mínimo 8 caracteres</span>
            }
          </div>

          @if (serverError()) {
            <div class="server-error">{{ serverError() }}</div>
          }

          <button type="submit" [disabled]="loading()" class="btn-primary">
            {{ loading() ? 'Criando...' : 'Criar Conta' }}
          </button>
        </form>

        <p class="auth-link">
          Já tem uma conta? <a routerLink="/login">Fazer login</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--neutral-50);
      padding: var(--space-4);
    }
    .auth-card {
      width: 100%;
      max-width: 400px;
      background: var(--surface);
      border-radius: var(--radius-lg);
      padding: var(--space-6);
      box-shadow: var(--shadow-lg);
    }
    .auth-header { text-align: center; margin-bottom: var(--space-5); }
    .auth-header h1 { font-size: 1.5rem; font-weight: 700; color: var(--primary); }
    .auth-header p { color: var(--neutral-500); margin-top: var(--space-1); }
    .auth-form { display: flex; flex-direction: column; gap: var(--space-3); }
    .form-field { display: flex; flex-direction: column; gap: var(--space-1); }
    .form-field label { font-size: 0.875rem; font-weight: 500; color: var(--neutral-700); }
    .form-field input {
      padding: var(--space-2) var(--space-3);
      border: 1px solid var(--neutral-300);
      border-radius: var(--radius-sm);
      font-size: 0.875rem;
    }
    .form-field input:focus { outline: 2px solid var(--primary); border-color: var(--primary); }
    .error { font-size: 0.75rem; color: var(--danger); }
    .server-error {
      padding: var(--space-2);
      background: var(--danger-light, #fef2f2);
      color: var(--danger);
      border-radius: var(--radius-sm);
      font-size: 0.875rem;
    }
    .btn-primary {
      padding: var(--space-2) var(--space-4);
      background: var(--primary);
      color: white;
      border: none;
      border-radius: var(--radius-sm);
      font-weight: 600;
      cursor: pointer;
    }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .auth-link { text-align: center; margin-top: var(--space-4); font-size: 0.875rem; color: var(--neutral-500); }
    .auth-link a { color: var(--primary); text-decoration: none; font-weight: 500; }
  `]
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  loading = signal(false);
  serverError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    shopName: ['', Validators.required],
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor() {
    if (this.auth.isAuthenticated) {
      this.router.navigate(['/dashboard']);
    }
  }

  async onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.serverError.set(null);

    try {
      const { shopName, name, email, password } = this.form.getRawValue();
      await this.auth.register(shopName, name, email, password);
      this.router.navigate(['/dashboard']);
    } catch (err: any) {
      const msg = err?.error?.errors
        ? Object.values(err.error.errors).flat().join('. ')
        : err?.error?.message || 'Erro ao criar conta. Tente novamente.';
      this.serverError.set(msg as string);
    } finally {
      this.loading.set(false);
    }
  }
}
```

- [ ] **Step 2: Add register route to app.routes.ts**

In `src/PeruShopHub.Web/src/app/app.routes.ts`, add after the login route:

```typescript
{
  path: 'register',
  loadComponent: () =>
    import('./pages/register/register.component').then(m => m.RegisterComponent),
},
```

- [ ] **Step 3: Add register link to login page**

In `src/PeruShopHub.Web/src/app/pages/login/login.component.ts`, add a link to the register page. Find the login form template and add after the submit button:

```html
<p class="auth-link">
  Não tem uma conta? <a routerLink="/register">Criar conta</a>
</p>
```

Add `RouterLink` to the imports array if not already present.

- [ ] **Step 4: Verify frontend compiles**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/register/ \
        src/PeruShopHub.Web/src/app/app.routes.ts \
        src/PeruShopHub.Web/src/app/pages/login/login.component.ts
git commit -m "feat: add self-service register page and route"
```

---

## Task 14: Frontend — Update Header and Sidebar for Tenant Context

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/shared/components/header/header.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/shared/components/sidebar/sidebar.component.ts`

- [ ] **Step 1: Update header to show tenant name**

In the header component, update the computed signals that derive user info. Replace references to `user.role` with `user.tenantRole`. Add a `tenantName` computed signal:

```typescript
readonly tenantName = computed(() => this.auth.tenantName());
```

In the template, display the tenant name near the user info area. For example, next to the user name display:

```html
@if (tenantName()) {
  <span class="tenant-badge">{{ tenantName() }}</span>
}
```

Add corresponding CSS for `.tenant-badge`.

- [ ] **Step 2: Update sidebar to show shop name**

In the sidebar component, inject `AuthService` and add a computed signal for the shop name:

```typescript
readonly shopName = computed(() => this.auth.tenantName() || 'PeruShopHub');
readonly isSuperAdmin = computed(() => this.auth.isSuperAdmin());
```

Display the shop name at the top of the sidebar. If user is super-admin, add an "Admin" nav group with a link to `/admin/tenants`.

- [ ] **Step 3: Update role references**

Search the header and sidebar templates for `user.role` or `userRole` and update to use `user.tenantRole` or `auth.tenantRole()`.

- [ ] **Step 4: Verify frontend compiles**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`

- [ ] **Step 5: Commit**

```bash
git add src/PeruShopHub.Web/src/app/shared/components/header/ \
        src/PeruShopHub.Web/src/app/shared/components/sidebar/
git commit -m "feat: show tenant name in header and sidebar, add admin nav for super-admins"
```

---

## Task 15: Frontend — Guards and Settings Page Update

**Files:**
- Create: `src/PeruShopHub.Web/src/app/guards/tenant.guard.ts`
- Create: `src/PeruShopHub.Web/src/app/guards/super-admin.guard.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/settings/settings.component.ts`
- Create: `src/PeruShopHub.Web/src/app/services/tenant.service.ts`

- [ ] **Step 1: Create tenant guard**

```typescript
// src/PeruShopHub.Web/src/app/guards/tenant.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const tenantGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.hasTenant()) return true;
  if (auth.isSuperAdmin()) return true;

  router.navigate(['/login']);
  return false;
};
```

- [ ] **Step 2: Create super-admin guard**

```typescript
// src/PeruShopHub.Web/src/app/guards/super-admin.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const superAdminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isSuperAdmin()) return true;

  router.navigate(['/dashboard']);
  return false;
};
```

- [ ] **Step 3: Create tenant service**

```typescript
// src/PeruShopHub.Web/src/app/services/tenant.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TenantDetail {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  memberCount: number;
  createdAt: string;
}

export interface TenantMember {
  id: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLogin: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenant`;

  getTenant(): Observable<TenantDetail> {
    return this.http.get<TenantDetail>(this.baseUrl);
  }

  updateTenant(name: string): Observable<TenantDetail> {
    return this.http.put<TenantDetail>(this.baseUrl, { name });
  }

  getMembers(): Observable<TenantMember[]> {
    return this.http.get<TenantMember[]>(`${this.baseUrl}/members`);
  }

  inviteMember(data: { name: string; email: string; password: string; role: string }): Observable<TenantMember> {
    return this.http.post<TenantMember>(`${this.baseUrl}/members/invite`, data);
  }

  updateMember(userId: string, data: { name: string; email: string; role: string }): Observable<TenantMember> {
    return this.http.put<TenantMember>(`${this.baseUrl}/members/${userId}`, data);
  }

  removeMember(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/members/${userId}`);
  }

  resetPassword(userId: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/members/${userId}/reset-password`, { newPassword });
  }
}
```

- [ ] **Step 4: Update settings component — use TenantService for member management**

In `src/PeruShopHub.Web/src/app/pages/settings/settings.component.ts`:

- Replace `SettingsService` user calls with `TenantService` calls:
  - `settingsService.getUsers()` → `tenantService.getMembers()`
  - `settingsService.createUser(...)` → `tenantService.inviteMember(...)`
  - `settingsService.updateUser(...)` → `tenantService.updateMember(...)`
  - `settingsService.deleteUser(...)` → `tenantService.removeMember(...)`
- Update role options to include "Owner" (display only, not selectable for new members)
- Import and inject `TenantService`

- [ ] **Step 5: Update app.routes.ts with guards**

Add `tenantGuard` to the Layout children. Add admin route:

```typescript
{
  path: 'admin',
  children: [
    {
      path: 'tenants',
      loadComponent: () =>
        import('./pages/admin/admin-tenants.component').then(m => m.AdminTenantsComponent),
    },
  ],
  canActivate: [authGuard, superAdminGuard],
},
```

Import the new guards.

- [ ] **Step 6: Verify frontend compiles**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`

- [ ] **Step 7: Commit**

```bash
git add src/PeruShopHub.Web/src/app/guards/tenant.guard.ts \
        src/PeruShopHub.Web/src/app/guards/super-admin.guard.ts \
        src/PeruShopHub.Web/src/app/services/tenant.service.ts \
        src/PeruShopHub.Web/src/app/pages/settings/settings.component.ts \
        src/PeruShopHub.Web/src/app/app.routes.ts
git commit -m "feat: add tenant/super-admin guards, tenant service, update settings for members"
```

---

## Task 16: Frontend — Super-Admin Tenants Page

**Files:**
- Create: `src/PeruShopHub.Web/src/app/pages/admin/admin-tenants.component.ts`

- [ ] **Step 1: Create admin tenants component**

```typescript
// src/PeruShopHub.Web/src/app/pages/admin/admin-tenants.component.ts
import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface TenantRow {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  memberCount: number;
  createdAt: string;
}

@Component({
  selector: 'app-admin-tenants',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Administração — Lojas</h1>
        <p class="subtitle">Gerenciamento de todos os tenants da plataforma</p>
      </div>

      @if (loading()) {
        <div class="loading">Carregando...</div>
      } @else {
        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Nome</th>
                <th>Slug</th>
                <th>Membros</th>
                <th>Status</th>
                <th>Criado em</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              @for (tenant of tenants(); track tenant.id) {
                <tr>
                  <td>{{ tenant.name }}</td>
                  <td><code>{{ tenant.slug }}</code></td>
                  <td>{{ tenant.memberCount }}</td>
                  <td>
                    <span [class]="tenant.isActive ? 'badge-success' : 'badge-danger'">
                      {{ tenant.isActive ? 'Ativo' : 'Inativo' }}
                    </span>
                  </td>
                  <td>{{ tenant.createdAt | date:'dd/MM/yyyy' }}</td>
                  <td>
                    <button
                      class="btn-ghost"
                      (click)="toggleActive(tenant)">
                      {{ tenant.isActive ? 'Desativar' : 'Ativar' }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: var(--space-4); }
    .page-header h1 { font-size: 1.25rem; font-weight: 600; }
    .subtitle { color: var(--neutral-500); font-size: 0.875rem; }
    .table-container { margin-top: var(--space-4); overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: var(--space-2) var(--space-3); text-align: left; border-bottom: 1px solid var(--neutral-200); }
    th { font-weight: 600; font-size: 0.75rem; text-transform: uppercase; color: var(--neutral-500); }
    code { font-family: 'Roboto Mono', monospace; font-size: 0.8125rem; }
    .badge-success { color: var(--success); font-weight: 500; }
    .badge-danger { color: var(--danger); font-weight: 500; }
    .btn-ghost {
      background: none;
      border: none;
      color: var(--primary);
      cursor: pointer;
      font-size: 0.875rem;
      padding: var(--space-1) var(--space-2);
      border-radius: var(--radius-sm);
    }
    .btn-ghost:hover { background: var(--neutral-100); }
    .loading { padding: var(--space-6); text-align: center; color: var(--neutral-500); }
  `]
})
export class AdminTenantsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin`;

  tenants = signal<TenantRow[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadTenants();
  }

  private loadTenants() {
    this.loading.set(true);
    this.http.get<TenantRow[]>(`${this.baseUrl}/tenants`).subscribe({
      next: data => {
        this.tenants.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  toggleActive(tenant: TenantRow) {
    const action = tenant.isActive ? 'deactivate' : 'activate';
    this.http.put(`${this.baseUrl}/tenants/${tenant.id}/${action}`, {}).subscribe({
      next: () => {
        this.tenants.update(list =>
          list.map(t => t.id === tenant.id ? { ...t, isActive: !t.isActive } : t)
        );
      }
    });
  }
}
```

- [ ] **Step 2: Verify frontend compiles**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development 2>&1 | head -30`

- [ ] **Step 3: Commit**

```bash
git add src/PeruShopHub.Web/src/app/pages/admin/
git commit -m "feat: add super-admin tenant management page"
```

---

## Task 17: Full Build Verification + Fix Remaining Issues

**Files:** Any files that fail compilation

- [ ] **Step 1: Build the full backend**

Run: `dotnet build`
Expected: Build succeeded. If not, fix each error:
- Missing `using` statements
- References to removed `SystemUser.Role` property
- Method signature mismatches from changed `IUserService`

- [ ] **Step 2: Build the full frontend**

Run: `cd src/PeruShopHub.Web && npx ng build --configuration development`
Expected: Build succeeded. If not, fix each error:
- References to old `AuthUser.role` property (now `tenantRole`)
- Missing imports

- [ ] **Step 3: Search for any remaining references to old Role property**

Run: `grep -rn '\.Role' src/PeruShopHub.Application/ src/PeruShopHub.API/ --include="*.cs" | grep -v "tenant_role\|TenantUser\|membership\|NormalizeRole\|ValidRoles\|CommissionRule\|\.Role =\|.Role)"`

Fix any remaining references to `SystemUser.Role` that should now reference `TenantUser.Role`.

- [ ] **Step 4: Commit any fixes**

```bash
git add -A
git commit -m "fix: resolve compilation errors from multi-tenancy refactor"
```

---

## Task 18: Apply Database Migration

- [ ] **Step 1: Start PostgreSQL (if not running)**

Run: `docker compose up -d db`
Expected: PostgreSQL container running

- [ ] **Step 2: Apply migration**

Run:
```bash
cd /workspaces/Repos/GitHub/PeruShopHub
dotnet ef database update \
  --project src/PeruShopHub.Infrastructure \
  --startup-project src/PeruShopHub.API
```

Expected: Migration applied successfully. If it fails due to existing data conflicts, check the migration SQL and fix.

- [ ] **Step 3: Verify tables exist**

Run:
```bash
docker compose exec db psql -U perushophub -d perushophub -c "\dt" | grep -E "Tenant|tenant"
```

Expected: `Tenants` and `TenantUsers` tables exist.

- [ ] **Step 4: Verify data migration**

Run:
```bash
docker compose exec db psql -U perushophub -d perushophub -c "SELECT \"Id\", \"Name\", \"Slug\" FROM \"Tenants\";"
```

Expected: Demo Shop tenant exists.

```bash
docker compose exec db psql -U perushophub -d perushophub -c "SELECT \"IsSuperAdmin\", \"Email\" FROM \"SystemUsers\";"
```

Expected: admin@perushophub.com has IsSuperAdmin=true.

- [ ] **Step 5: Commit migration snapshot if any changes were needed**

```bash
git add src/PeruShopHub.Infrastructure/Migrations/
git commit -m "fix: finalize multi-tenancy migration"
```

---

## Task 19: Smoke Test

- [ ] **Step 1: Start the API**

Run: `dotnet run --project src/PeruShopHub.API &`
Wait for "Now listening on http://localhost:5000" (or similar)

- [ ] **Step 2: Test login with existing user**

Run:
```bash
curl -s http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@perushophub.com","password":"Admin123!"}' | jq .
```

Expected: Response includes `accessToken`, `refreshToken`, and `user` with `tenantId`, `tenantRole`, `tenantName`, `isSuperAdmin: true`.

- [ ] **Step 3: Test register new tenant**

Run:
```bash
curl -s http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"shopName":"Test Shop","name":"Test User","email":"test@example.com","password":"Test1234!"}' | jq .
```

Expected: Returns new user with `tenantId`, `tenantName: "Test Shop"`, `tenantRole: "Owner"`.

- [ ] **Step 4: Test data isolation**

Using the token from step 3, list products:
```bash
TOKEN="<access_token_from_step_3>"
curl -s http://localhost:5000/api/products \
  -H "Authorization: Bearer $TOKEN" | jq .totalCount
```

Expected: `0` (new tenant has no products)

Using admin token from step 2:
```bash
ADMIN_TOKEN="<access_token_from_step_2>"
curl -s http://localhost:5000/api/products \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq .totalCount
```

Expected: `10` (demo tenant's products)

- [ ] **Step 5: Test tenant management**

```bash
curl -s http://localhost:5000/api/admin/tenants \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq .
```

Expected: Lists both "Demo Shop" and "Test Shop" tenants.

- [ ] **Step 6: Stop the API**

Kill the background dotnet process.

- [ ] **Step 7: Commit any final fixes**

If any smoke test revealed issues, fix them and commit.

```bash
git add -A
git commit -m "fix: smoke test fixes for multi-tenancy"
```

---

## Summary

| Task | Description | Estimated Complexity |
|------|-------------|---------------------|
| 1 | Core domain entities + interfaces | Low |
| 2 | Add ITenantScoped to 18 entities | Low (repetitive) |
| 3 | EF Core configurations for tenant tables + columns | Low (repetitive) |
| 4 | DbContext global query filters + auto-set | Medium |
| 5 | Database migration generation + data migration SQL | Medium |
| 6 | Tenant middleware + DI registration | Medium |
| 7 | Auth controller rewrite (register, JWT claims, switch) | High |
| 8 | UserService rewrite for tenant-scoped members | Medium |
| 9 | TenantController + AdminController + TenantService | Medium |
| 10 | Update remaining controller role attributes | Low |
| 11 | Update seed data | Low |
| 12 | Frontend auth service update | Medium |
| 13 | Frontend register page | Medium |
| 14 | Frontend header/sidebar tenant context | Low |
| 15 | Frontend guards + settings + tenant service | Medium |
| 16 | Frontend super-admin page | Low |
| 17 | Full build verification + fixes | Variable |
| 18 | Database migration application | Low |
| 19 | Smoke test | Low |
