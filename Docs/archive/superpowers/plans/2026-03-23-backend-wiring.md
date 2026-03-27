# Backend Wiring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up the full ASP.NET Core 9 backend with PostgreSQL, Redis, SignalR, background workers, and file uploads — then connect the Angular frontend to replace all mock data with real API calls.

**Architecture:** Modular monolith with 5 .NET projects (Core, Infrastructure, Application, API, Worker). Angular frontend communicates via REST + SignalR through a dev proxy. Redis provides caching and SignalR backplane. Background workers run as a separate process.

**Tech Stack:** .NET 9 / ASP.NET Core / EF Core 9 / PostgreSQL 16 / Redis 7 / SignalR / Angular 21 / Chart.js

**PRD:** `tasks/prd-backend-wiring.md`

**Branch:** `ralph/backend-wiring`

---

## Execution Strategy

This plan is designed for **parallel team agent execution** across 6 phases:

| Phase | Agents | Mode | Depends On |
|-------|--------|------|------------|
| **1 — Foundation** | 1 sequential | worktree → merge to branch | — |
| **2 — Infrastructure** | 4 parallel | worktrees → merge | Phase 1 |
| **3 — API Endpoints** | 4 parallel | worktrees → merge | Phase 1 (concurrent with Phase 2) |
| **4a — Frontend Foundation** | 1 sequential | worktree → merge | Phase 2 + 3 |
| **4b — Frontend Wiring** | 4 parallel | worktrees → merge | Phase 4a |
| **5 — Cleanup** | 1 sequential | on branch | Phase 4b |
| **6 — Team Lead Review** | 1 code-reviewer | on branch | Phase 5 |

**Merge conflict hotspot:** `src/PeruShopHub.API/Program.cs` — each Phase 2/3 agent adds DI registrations. Merge sequentially and resolve additively. See **Appendix A** for the expected final merged Program.cs.

### File Ownership Rules (Parallel Agents)

To avoid merge conflicts, each file is owned by exactly one agent. **No two parallel agents may modify the same file.**

| File | Owner (Phase 2) | Owner (Phase 3) | Owner (Phase 4b) |
|------|-----------------|-----------------|-------------------|
| `Program.cs` (API) | Each adds a clearly marked block | No changes needed | — |
| `Program.cs` (Worker) | infra-workers only | — | — |
| `PeruShopHubDbContext.cs` | — | No changes needed | — |
| `category.service.ts` | — | — | wire-catalog only |
| `product-variant.service.ts` | — | — | wire-catalog only |
| `notification.service.ts` | — | — | wire-support only |

### Key Design Decision: No Service Layer (Intentional)

Controllers query DbContext directly in this phase. This is a deliberate simplification — there's no test infrastructure yet (separate PRD), and adding a service layer purely for DI would be premature abstraction. When tests are added, controllers can be refactored to use application services.

### Key Design Decision: VariationField Stays Frontend-Only

Category variation fields (`VariationField` type in `category.model.ts`) remain client-side only in this PRD. The backend `Category` entity does not include variation fields. The Angular `CategoryService` keeps its local variation field logic but fetches category tree data from the API. Variation fields will be backed by DB in a future PRD when marketplace integration needs them.

### Key Design Decision: Notification Field Naming

The Angular `Notification` interface uses `read` (boolean), but the C# `Notification` entity uses `IsRead`. JSON serialization (camelCase) produces `isRead`. **Frontend must be updated:** rename `read` → `isRead` in `notification.service.ts` and all templates that reference it. This is handled in Phase 4b (wire-support).

---

## File Map

### New .NET Projects

```
PeruShopHub.sln                              ← Solution file (root)
src/
├── PeruShopHub.Core/                        ← Domain layer
│   ├── PeruShopHub.Core.csproj
│   ├── Entities/
│   │   ├── Product.cs
│   │   ├── ProductVariant.cs
│   │   ├── Category.cs
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── OrderCost.cs
│   │   ├── Customer.cs
│   │   ├── Supply.cs
│   │   ├── Notification.cs
│   │   ├── SystemUser.cs
│   │   ├── MarketplaceConnection.cs
│   │   └── FileUpload.cs
│   ├── ValueObjects/
│   │   └── Money.cs
│   └── Interfaces/
│       ├── ICacheService.cs
│       ├── INotificationDispatcher.cs
│       └── IFileStorageService.cs
│
├── PeruShopHub.Infrastructure/              ← Data access + external services
│   ├── PeruShopHub.Infrastructure.csproj
│   ├── Persistence/
│   │   ├── PeruShopHubDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── ProductConfiguration.cs
│   │   │   ├── ProductVariantConfiguration.cs
│   │   │   ├── CategoryConfiguration.cs
│   │   │   ├── OrderConfiguration.cs
│   │   │   ├── OrderItemConfiguration.cs
│   │   │   ├── OrderCostConfiguration.cs
│   │   │   ├── CustomerConfiguration.cs
│   │   │   ├── SupplyConfiguration.cs
│   │   │   ├── NotificationConfiguration.cs
│   │   │   ├── SystemUserConfiguration.cs
│   │   │   ├── MarketplaceConnectionConfiguration.cs
│   │   │   └── FileUploadConfiguration.cs
│   │   ├── Migrations/                      ← EF Core auto-generated
│   │   └── Seeds/
│   │       └── SeedData.sql
│   ├── Cache/
│   │   └── RedisCacheService.cs
│   ├── Notifications/
│   │   └── SignalRNotificationDispatcher.cs
│   └── Storage/
│       └── LocalFileStorageService.cs
│
├── PeruShopHub.Application/                 ← DTOs, service contracts
│   ├── PeruShopHub.Application.csproj
│   ├── Common/
│   │   └── PagedResult.cs
│   └── DTOs/
│       ├── Dashboard/
│       │   ├── DashboardSummaryDto.cs
│       │   ├── KpiCardDto.cs
│       │   ├── ChartDataPointDto.cs
│       │   ├── ProductRankingDto.cs
│       │   └── PendingActionsDto.cs
│       ├── Products/
│       │   ├── ProductListDto.cs
│       │   ├── ProductDetailDto.cs
│       │   ├── ProductVariantDto.cs
│       │   ├── CreateProductDto.cs
│       │   └── UpdateProductDto.cs
│       ├── Categories/
│       │   ├── CategoryListDto.cs
│       │   ├── CategoryDetailDto.cs
│       │   ├── CreateCategoryDto.cs
│       │   └── UpdateCategoryDto.cs
│       ├── Orders/
│       │   ├── OrderListDto.cs
│       │   ├── OrderDetailDto.cs
│       │   ├── OrderItemDto.cs
│       │   ├── OrderCostDto.cs
│       │   ├── BuyerDto.cs
│       │   ├── ShippingInfoDto.cs
│       │   └── PaymentInfoDto.cs
│       ├── Customers/
│       │   ├── CustomerListDto.cs
│       │   └── CustomerDetailDto.cs
│       ├── Supplies/
│       │   ├── SupplyListDto.cs
│       │   ├── CreateSupplyDto.cs
│       │   └── UpdateSupplyDto.cs
│       ├── Finance/
│       │   ├── FinanceSummaryDto.cs
│       │   ├── SkuProfitabilityDto.cs
│       │   ├── ReconciliationDto.cs
│       │   └── AbcProductDto.cs
│       ├── Settings/
│       │   ├── SystemUserDto.cs
│       │   ├── IntegrationDto.cs
│       │   └── CostConfigDto.cs
│       ├── Notifications/
│       │   └── NotificationDto.cs
│       ├── Search/
│       │   └── SearchResultDto.cs
│       └── Files/
│           └── FileUploadDto.cs
│
├── PeruShopHub.API/                         ← Web API host
│   ├── PeruShopHub.API.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Controllers/
│   │   ├── DashboardController.cs
│   │   ├── ProductsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── OrdersController.cs
│   │   ├── CustomersController.cs
│   │   ├── SuppliesController.cs
│   │   ├── FinanceController.cs
│   │   ├── SettingsController.cs
│   │   ├── NotificationsController.cs
│   │   ├── SearchController.cs
│   │   └── FilesController.cs
│   └── Hubs/
│       └── NotificationHub.cs
│
├── PeruShopHub.Worker/                      ← Background services
│   ├── PeruShopHub.Worker.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Workers/
│   │   ├── StockAlertWorker.cs
│   │   └── NotificationCleanupWorker.cs
```

### Modified Angular Files

```
src/PeruShopHub.Web/
├── proxy.conf.json                          ← NEW: API + SignalR proxy
├── package.json                             ← MODIFY: add @microsoft/signalr
├── src/app/
│   ├── app.config.ts                        ← MODIFY: add provideHttpClient
│   ├── environments/                        ← NEW directory
│   │   ├── environment.ts
│   │   └── environment.development.ts
│   ├── services/
│   │   ├── dashboard.service.ts             ← NEW
│   │   ├── product.service.ts               ← NEW
│   │   ├── order.service.ts                 ← NEW
│   │   ├── customer.service.ts              ← NEW
│   │   ├── supply.service.ts                ← NEW
│   │   ├── finance.service.ts               ← NEW
│   │   ├── settings.service.ts              ← NEW
│   │   ├── search.service.ts                ← NEW
│   │   ├── signalr.service.ts               ← NEW
│   │   ├── file-upload.service.ts           ← NEW
│   │   ├── category.service.ts              ← MODIFY: rewire to HTTP
│   │   ├── notification.service.ts          ← MODIFY: rewire to HTTP + SignalR
│   │   └── product-variant.service.ts       ← MODIFY: rewire to HTTP
│   ├── models/
│   │   ├── api.models.ts                    ← NEW: shared API response types
│   │   ├── category.model.ts                ← KEEP (already good)
│   │   └── product-variant.model.ts         ← KEEP (already good)
│   ├── interceptors/
│   │   └── error.interceptor.ts             ← NEW
│   └── pages/
│       ├── dashboard/dashboard.component.ts ← MODIFY: remove mocks
│       ├── sales/sales-list.component.ts    ← MODIFY: remove mocks
│       ├── sales/sale-detail.component.ts   ← MODIFY: remove mocks
│       ├── products/products-list.component.ts ← MODIFY: remove mocks
│       ├── products/product-form.component.ts  ← MODIFY: remove mocks
│       ├── products/product-detail.component.ts ← MODIFY: remove mocks
│       ├── categories/categories.component.ts   ← MODIFY: use HTTP service
│       ├── customers/customers.component.ts     ← MODIFY: remove mocks
│       ├── customers/customer-detail.component.ts ← MODIFY: remove mocks
│       ├── supplies/supplies.component.ts       ← MODIFY: remove mocks
│       ├── finance/finance.component.ts         ← MODIFY: remove mocks
│       ├── settings/settings.component.ts       ← MODIFY: remove mocks
│       └── shared/components/
│           └── search-palette/search-palette.component.ts ← MODIFY: remove mocks
```

---

## Phase 1 — Foundation (Sequential, 1 Agent)

> **Agent name:** `foundation`
> **Worktree branch:** `ralph/backend-wiring/foundation`
> **Merges to:** `ralph/backend-wiring`

### Task 1.1: Create .NET Solution and Projects

**Files:**
- Create: `PeruShopHub.sln`
- Create: `src/PeruShopHub.Core/PeruShopHub.Core.csproj`
- Create: `src/PeruShopHub.Infrastructure/PeruShopHub.Infrastructure.csproj`
- Create: `src/PeruShopHub.Application/PeruShopHub.Application.csproj`
- Create: `src/PeruShopHub.API/PeruShopHub.API.csproj`
- Create: `src/PeruShopHub.Worker/PeruShopHub.Worker.csproj`

- [ ] **Step 1: Create solution and projects**

```bash
cd /workspaces/Repos/GitHub/PeruShopHub

# Create solution
dotnet new sln -n PeruShopHub

# Create projects
dotnet new classlib -n PeruShopHub.Core -o src/PeruShopHub.Core --framework net9.0
dotnet new classlib -n PeruShopHub.Infrastructure -o src/PeruShopHub.Infrastructure --framework net9.0
dotnet new classlib -n PeruShopHub.Application -o src/PeruShopHub.Application --framework net9.0
dotnet new webapi -n PeruShopHub.API -o src/PeruShopHub.API --framework net9.0 --no-https false
dotnet new worker -n PeruShopHub.Worker -o src/PeruShopHub.Worker --framework net9.0

# Add projects to solution
dotnet sln add src/PeruShopHub.Core/PeruShopHub.Core.csproj
dotnet sln add src/PeruShopHub.Infrastructure/PeruShopHub.Infrastructure.csproj
dotnet sln add src/PeruShopHub.Application/PeruShopHub.Application.csproj
dotnet sln add src/PeruShopHub.API/PeruShopHub.API.csproj
dotnet sln add src/PeruShopHub.Worker/PeruShopHub.Worker.csproj
```

- [ ] **Step 2: Add project references (dependency rule)**

```bash
# Application → Core
dotnet add src/PeruShopHub.Application reference src/PeruShopHub.Core

# Infrastructure → Core
dotnet add src/PeruShopHub.Infrastructure reference src/PeruShopHub.Core

# API → Application, Infrastructure
dotnet add src/PeruShopHub.API reference src/PeruShopHub.Application
dotnet add src/PeruShopHub.API reference src/PeruShopHub.Infrastructure

# Worker → Application, Infrastructure
dotnet add src/PeruShopHub.Worker reference src/PeruShopHub.Application
dotnet add src/PeruShopHub.Worker reference src/PeruShopHub.Infrastructure
```

- [ ] **Step 3: Add NuGet packages**

```bash
# Infrastructure: EF Core + PostgreSQL + Redis + SignalR Redis backplane
dotnet add src/PeruShopHub.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/PeruShopHub.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/PeruShopHub.Infrastructure package StackExchange.Redis
dotnet add src/PeruShopHub.Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add src/PeruShopHub.Infrastructure package Microsoft.AspNetCore.SignalR.StackExchangeRedis

# API: EF Core tools (for migrations from startup project)
dotnet add src/PeruShopHub.API package Microsoft.EntityFrameworkCore.Design

# Worker: needs EF Core for DB access
dotnet add src/PeruShopHub.Worker package Npgsql.EntityFrameworkCore.PostgreSQL
```

- [ ] **Step 4: Clean up template files**

Delete auto-generated template files:
- `src/PeruShopHub.Core/Class1.cs`
- `src/PeruShopHub.Infrastructure/Class1.cs`
- `src/PeruShopHub.Application/Class1.cs`
- `src/PeruShopHub.API/Controllers/` (template controller if any)
- `src/PeruShopHub.Worker/Worker.cs` (template worker)

- [ ] **Step 5: Verify build**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: scaffold .NET solution with 5 modular monolith projects"
```

---

### Task 1.2: Core Domain Entities

**Files:**
- Create: `src/PeruShopHub.Core/Entities/Product.cs`
- Create: `src/PeruShopHub.Core/Entities/ProductVariant.cs`
- Create: `src/PeruShopHub.Core/Entities/Category.cs`
- Create: `src/PeruShopHub.Core/Entities/Order.cs`
- Create: `src/PeruShopHub.Core/Entities/OrderItem.cs`
- Create: `src/PeruShopHub.Core/Entities/OrderCost.cs`
- Create: `src/PeruShopHub.Core/Entities/Customer.cs`
- Create: `src/PeruShopHub.Core/Entities/Supply.cs`
- Create: `src/PeruShopHub.Core/Entities/Notification.cs`
- Create: `src/PeruShopHub.Core/Entities/SystemUser.cs`
- Create: `src/PeruShopHub.Core/Entities/MarketplaceConnection.cs`
- Create: `src/PeruShopHub.Core/Entities/FileUpload.cs`
- Create: `src/PeruShopHub.Core/ValueObjects/Money.cs`

- [ ] **Step 1: Create Product entity**

```csharp
// src/PeruShopHub.Core/Entities/Product.cs
namespace PeruShopHub.Core.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Supplier { get; set; }
    public string Status { get; set; } = "Ativo"; // Ativo, Pausado, Encerrado
    public bool NeedsReview { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<FileUpload> Photos { get; set; } = new List<FileUpload>();
}
```

- [ ] **Step 2: Create ProductVariant entity**

```csharp
// src/PeruShopHub.Core/Entities/ProductVariant.cs
namespace PeruShopHub.Core.Entities;

public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Attributes { get; set; } = "{}"; // JSON: { "Cor": "Preto", "Tamanho": "M" }
    public decimal? Price { get; set; } // null = use product base price
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public bool NeedsReview { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
```

- [ ] **Step 3: Create Category entity**

```csharp
// src/PeruShopHub.Core/Entities/Category.cs
namespace PeruShopHub.Core.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
```

- [ ] **Step 4: Create Order, OrderItem, OrderCost entities**

```csharp
// src/PeruShopHub.Core/Entities/Order.cs
namespace PeruShopHub.Core.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerNickname { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Profit { get; set; }
    public string Status { get; set; } = "Pago"; // Pago, Enviado, Entregue, Cancelado, Devolvido
    public DateTime OrderDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? LogisticType { get; set; }
    public string? PaymentMethod { get; set; }
    public int? Installments { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer? Customer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderCost> Costs { get; set; } = new List<OrderCost>();
}

// src/PeruShopHub.Core/Entities/OrderItem.cs
namespace PeruShopHub.Core.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Variation { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}

// src/PeruShopHub.Core/Entities/OrderCost.cs
namespace PeruShopHub.Core.Entities;

public class OrderCost
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Category { get; set; } = string.Empty; // marketplace_commission, fixed_fee, shipping, etc.
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public string Source { get; set; } = "Manual"; // API, Manual, Calculado

    // Navigation
    public Order Order { get; set; } = null!;
}
```

- [ ] **Step 5: Create Customer entity**

```csharp
// src/PeruShopHub.Core/Entities/Customer.cs
namespace PeruShopHub.Core.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastPurchase { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

- [ ] **Step 6: Create Supply entity**

```csharp
// src/PeruShopHub.Core/Entities/Supply.cs
namespace PeruShopHub.Core.Entities;

public class Supply
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros"; // Embalagem, Etiqueta, Caixa, Fita, Proteção, Outros
    public decimal UnitCost { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public string? Supplier { get; set; }
    public string Status { get; set; } = "Ativo"; // Ativo, Inativo
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 7: Create Notification, SystemUser, MarketplaceConnection, FileUpload entities**

```csharp
// src/PeruShopHub.Core/Entities/Notification.cs
namespace PeruShopHub.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // sale, question, stock, margin, connection
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? NavigationTarget { get; set; }
}

// src/PeruShopHub.Core/Entities/SystemUser.cs
namespace PeruShopHub.Core.Entities;

public class SystemUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer"; // admin, manager, viewer
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// src/PeruShopHub.Core/Entities/MarketplaceConnection.cs
namespace PeruShopHub.Core.Entities;

public class MarketplaceConnection
{
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public bool IsConnected { get; set; }
    public string? SellerNickname { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool ComingSoon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// src/PeruShopHub.Core/Entities/FileUpload.cs
namespace PeruShopHub.Core.Entities;

public class FileUpload
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // product, invoice, etc.
    public Guid EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 8: Create Money value object**

```csharp
// src/PeruShopHub.Core/ValueObjects/Money.cs
namespace PeruShopHub.Core.ValueObjects;

public record Money(decimal Amount, string Currency = "BRL")
{
    public static Money Zero => new(0m);
    public static Money FromBrl(decimal amount) => new(amount, "BRL");
}
```

- [ ] **Step 9: Create Core interfaces**

```csharp
// src/PeruShopHub.Core/Interfaces/ICacheService.cs
namespace PeruShopHub.Core.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

// src/PeruShopHub.Core/Interfaces/INotificationDispatcher.cs
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Core.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, CancellationToken ct = default);
    Task BroadcastDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default);
}

// src/PeruShopHub.Core/Interfaces/IFileStorageService.cs
namespace PeruShopHub.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream file, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
    string GetPublicUrl(string storagePath);
}
```

- [ ] **Step 10: Verify build**

```bash
dotnet build
```

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "feat: add core domain entities, value objects, and interfaces"
```

---

### Task 1.3: EF Core DbContext and Configurations

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs`
- Create: `src/PeruShopHub.Infrastructure/Persistence/Configurations/*.cs` (12 files)

- [ ] **Step 1: Create DbContext**

```csharp
// src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence;

public class PeruShopHubDbContext : DbContext
{
    public PeruShopHubDbContext(DbContextOptions<PeruShopHubDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PeruShopHubDbContext).Assembly);
    }
}
```

- [ ] **Step 2: Create entity configurations**

Create one `IEntityTypeConfiguration<T>` per entity in `src/PeruShopHub.Infrastructure/Persistence/Configurations/`. Key rules:
- All `decimal` properties: `.HasPrecision(18, 4)`
- `Product.Sku`: unique index
- `Category.Slug`: unique index
- `Category.ParentId`: self-ref FK with `.OnDelete(DeleteBehavior.Restrict)`
- `Order.ExternalOrderId`: unique index
- `Customer.Email`: index (not unique — can be null/masked)
- `ProductVariant.Attributes`: `.HasColumnType("jsonb")`
- All string enums stored as varchar(50)
- All timestamps as `timestamp with time zone`

Write each configuration file. Example for Product:

```csharp
// src/PeruShopHub.Infrastructure/Persistence/Configurations/ProductConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeruShopHub.Core.Entities;

namespace PeruShopHub.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(5000);
        builder.Property(p => p.CategoryId).HasMaxLength(100);
        builder.Property(p => p.Price).HasPrecision(18, 4);
        builder.Property(p => p.PurchaseCost).HasPrecision(18, 4);
        builder.Property(p => p.PackagingCost).HasPrecision(18, 4);
        builder.Property(p => p.Supplier).HasMaxLength(200);
        builder.Property(p => p.Status).HasMaxLength(50);
        builder.Property(p => p.Weight).HasPrecision(10, 3);
        builder.Property(p => p.Height).HasPrecision(10, 2);
        builder.Property(p => p.Width).HasPrecision(10, 2);
        builder.Property(p => p.Length).HasPrecision(10, 2);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Create similar configurations for all 12 entities following the same pattern. Each file should be in `Configurations/` with the naming convention `{Entity}Configuration.cs`.

- [ ] **Step 3: Verify build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add EF Core DbContext with entity configurations"
```

---

### Task 1.4: API Project Setup (Program.cs)

**Files:**
- Modify: `src/PeruShopHub.API/Program.cs`
- Create: `src/PeruShopHub.API/appsettings.json`
- Create: `src/PeruShopHub.API/appsettings.Development.json`
- Create: `src/PeruShopHub.API/Properties/launchSettings.json`

- [ ] **Step 1: Write Program.cs with all service registrations**

```csharp
// src/PeruShopHub.API/Program.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// CORS (fallback — primary dev access via Angular proxy)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles(); // for uploaded files in wwwroot/uploads
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

**Note:** Phase 2 agents will add Redis, SignalR, and file storage registrations to this file. Phase 3 agents don't modify Program.cs (controllers are auto-discovered).

- [ ] **Step 2: Write appsettings files**

```json
// src/PeruShopHub.API/appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

```json
// src/PeruShopHub.API/appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=perushophub;Username=postgres;Password=dev",
    "Redis": "localhost:6379"
  },
  "FileStorage": {
    "BasePath": "wwwroot/uploads"
  },
  "Workers": {
    "StockAlert": {
      "IntervalMinutes": 15
    },
    "NotificationCleanup": {
      "IntervalHours": 24,
      "RetentionDays": 30
    }
  }
}
```

- [ ] **Step 3: Write launchSettings.json**

```json
// src/PeruShopHub.API/Properties/launchSettings.json
{
  "profiles": {
    "PeruShopHub.API": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 4: Add health check NuGet package**

```bash
dotnet add src/PeruShopHub.API package AspNetCore.HealthChecks.NpgSql
```

- [ ] **Step 5: Verify build**

```bash
dotnet build
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: configure API project with PostgreSQL, CORS, Swagger, health checks"
```

---

### Task 1.5: Application Layer DTOs and PagedResult

**Files:**
- Create: `src/PeruShopHub.Application/Common/PagedResult.cs`
- Create: All DTO files in `src/PeruShopHub.Application/DTOs/` subdirectories

- [ ] **Step 1: Create PagedResult generic wrapper**

```csharp
// src/PeruShopHub.Application/Common/PagedResult.cs
namespace PeruShopHub.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

- [ ] **Step 2: Create all DTO classes**

Create every DTO listed in the File Map above. Each DTO should be a simple `record` or `class` with properties matching what the Angular frontend expects (camelCase via JSON serialization).

Key DTOs that must match frontend interfaces exactly:

**Dashboard DTOs:**
```csharp
// src/PeruShopHub.Application/DTOs/Dashboard/KpiCardDto.cs
namespace PeruShopHub.Application.DTOs.Dashboard;
public record KpiCardDto(string Label, string Value, decimal Change, string ChangeLabel, bool InvertColors = false);

// src/PeruShopHub.Application/DTOs/Dashboard/ProductRankingDto.cs
namespace PeruShopHub.Application.DTOs.Dashboard;
public record ProductRankingDto(Guid Id, string Name, int Sales, decimal Revenue, decimal Profit, decimal Margin);

// src/PeruShopHub.Application/DTOs/Dashboard/PendingActionsDto.cs
namespace PeruShopHub.Application.DTOs.Dashboard;
public record PendingActionDto(string Label, int Count, string Variant);

// src/PeruShopHub.Application/DTOs/Dashboard/DashboardSummaryDto.cs
namespace PeruShopHub.Application.DTOs.Dashboard;
public record DashboardSummaryDto(IReadOnlyList<KpiCardDto> Kpis, IReadOnlyList<PendingActionDto> PendingActions);

// src/PeruShopHub.Application/DTOs/Dashboard/ChartDataPointDto.cs
namespace PeruShopHub.Application.DTOs.Dashboard;
public record ChartDataPointDto(string Label, decimal Value1, decimal? Value2 = null);
```

**Product DTOs:**
```csharp
// src/PeruShopHub.Application/DTOs/Products/ProductListDto.cs
namespace PeruShopHub.Application.DTOs.Products;
public record ProductListDto(
    Guid Id, string? Photo, string Name, string Sku, decimal Price,
    int Stock, string Status, decimal Margin, int VariantCount, bool NeedsReview);

// src/PeruShopHub.Application/DTOs/Products/ProductDetailDto.cs
namespace PeruShopHub.Application.DTOs.Products;
public record ProductDetailDto(
    Guid Id, string Sku, string Name, string? Description, string? CategoryId,
    decimal Price, decimal PurchaseCost, decimal PackagingCost, string? Supplier,
    string Status, bool NeedsReview, decimal Weight, decimal Height, decimal Width, decimal Length,
    IReadOnlyList<ProductVariantDto> Variants, IReadOnlyList<FileUploadDto> Photos);

// Import FileUploadDto from Files namespace or define inline
```

**Order DTOs:**
```csharp
// src/PeruShopHub.Application/DTOs/Orders/OrderListDto.cs
namespace PeruShopHub.Application.DTOs.Orders;
public record OrderListDto(
    Guid Id, string ExternalOrderId, DateTime OrderDate, string BuyerName,
    int ItemCount, decimal TotalAmount, decimal Profit, string Status);

// src/PeruShopHub.Application/DTOs/Orders/OrderDetailDto.cs
namespace PeruShopHub.Application.DTOs.Orders;
public record OrderDetailDto(
    Guid Id, string ExternalOrderId, DateTime OrderDate, string Status,
    IReadOnlyList<OrderItemDto> Items, BuyerDto Buyer, ShippingInfoDto Shipping,
    PaymentInfoDto Payment, IReadOnlyList<OrderCostDto> Costs, decimal Revenue);
```

Continue creating all remaining DTOs following this pattern. Each DTO lives in its own file within the appropriate subdirectory.

**Finance DTOs:**
```csharp
namespace PeruShopHub.Application.DTOs.Finance;
public record SkuProfitabilityDto(
    string Sku, string Product, int Sales, decimal Revenue, decimal Cmv,
    decimal Commissions, decimal Shipping, decimal Taxes, decimal Profit, decimal Margin);

public record ReconciliationDto(
    string Period, decimal ExpectedValue, decimal DepositedValue, decimal Difference, string Status);

public record AbcProductDto(
    int Rank, string Product, string Sku, decimal Profit, decimal ProfitPercent, string Classification);
```

**Search DTO:**
```csharp
namespace PeruShopHub.Application.DTOs.Search;
public record SearchResultDto(string Type, string Id, string Primary, string Secondary, string Route);
```

**FileUpload DTO:**
```csharp
namespace PeruShopHub.Application.DTOs.Files;
public record FileUploadDto(Guid Id, string Url, string FileName, string ContentType, long SizeBytes, int SortOrder);
```

- [ ] **Step 3: Verify build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add application layer DTOs and PagedResult"
```

---

### Task 1.6: Initial Migration

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Persistence/Migrations/` (auto-generated)

- [ ] **Step 1: Ensure PostgreSQL is running**

```bash
docker run -d --name perushophub-db -p 5432:5432 -e POSTGRES_PASSWORD=dev postgres:16 2>/dev/null || docker start perushophub-db
```

- [ ] **Step 2: Generate initial migration**

```bash
dotnet ef migrations add InitialCreate \
  --project src/PeruShopHub.Infrastructure \
  --startup-project src/PeruShopHub.API
```

- [ ] **Step 3: Apply migration**

```bash
dotnet ef database update \
  --project src/PeruShopHub.Infrastructure \
  --startup-project src/PeruShopHub.API
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add initial EF Core migration for all entities"
```

---

### Task 1.7: Seed Data SQL Script and Migration

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Persistence/Seeds/SeedData.sql`

- [ ] **Step 1: Write seed SQL script**

Create `SeedData.sql` with PostgreSQL INSERT statements. Use explicit UUIDs for referential integrity. Include:

- **27 categories** in hierarchy (from product form: Eletrônicos, Celulares e Telefones, Informática, Acessórios de Informática, Moda, Roupas, Calçados, Casa e Decoração, Esportes, Beleza, Automotivo, Brinquedos, Livros, Ferramentas, Saúde, Alimentos, Pet, Jardim, Escritório, Bebês, Games, Música, Filmes, Papelaria, Telefonia, Tablets, Câmeras)
- **10 products** with varied statuses and margins
- **12 product variants** across 3 products
- **15 orders** with statuses: Pago(3), Enviado(4), Entregue(5), Cancelado(2), Devolvido(1)
- **Order items** for each order (2-3 items each)
- **Order costs** per order (6-9 cost line items each: commission, fixed_fee, shipping, payment_fee, tax, product_cost, packaging, etc.)
- **10 customers** with masked emails (***@domain.com)
- **7 supplies** with stock levels (some below minimum for stock alert testing)
- **8 notifications** (mix of sale, question, stock, margin types)
- **3 system users** (admin, manager, viewer)
- **2 marketplace connections** (ML connected, Amazon coming soon)

Financial data must be internally consistent:
- Order profit = revenue - sum(costs)
- Customer totalOrders and totalSpent match their actual orders
- Product margins derivable from price vs cost

Example seed structure:
```sql
-- Categories (27 in hierarchy)
INSERT INTO "Categories" ("Id", "Name", "Slug", "ParentId", "Icon", "IsActive", "ProductCount", "Order", "CreatedAt", "UpdatedAt") VALUES
('a0000000-0000-0000-0000-000000000001', 'Eletrônicos', 'eletronicos', NULL, 'cpu', true, 5, 1, NOW(), NOW()),
('a0000000-0000-0000-0000-000000000002', 'Celulares e Telefones', 'celulares-e-telefones', 'a0000000-0000-0000-0000-000000000001', 'smartphone', true, 2, 1, NOW(), NOW()),
-- ... continue for all 27

-- Products (10)
INSERT INTO "Products" ("Id", "Sku", "Name", "Description", "CategoryId", "Price", "PurchaseCost", "PackagingCost", "Supplier", "Status", "NeedsReview", "IsActive", "Weight", "Height", "Width", "Length", "CreatedAt", "UpdatedAt") VALUES
('b0000000-0000-0000-0000-000000000001', 'FON-BT-001', 'Fone Bluetooth TWS Pro Max', 'Fone de ouvido sem fio com cancelamento de ruído ativo', 'a0000000-0000-0000-0000-000000000001', 189.90, 45.00, 2.50, 'TechSupply Ltda', 'Ativo', false, true, 0.150, 8.0, 12.0, 6.0, NOW(), NOW()),
-- ... continue for all 10

-- Customers (10)
-- Orders (15) with items and costs
-- Supplies (7)
-- Notifications (8)
-- SystemUsers (3)
-- MarketplaceConnections (2)
```

The full SQL will be ~300-500 lines. Write complete, valid SQL with all data.

- [ ] **Step 2: Create seed migration that executes the SQL**

```bash
dotnet ef migrations add SeedExampleData \
  --project src/PeruShopHub.Infrastructure \
  --startup-project src/PeruShopHub.API
```

Then modify the generated migration file to execute the SQL script:

```csharp
// In the generated migration's Up() method:
protected override void Up(MigrationBuilder migrationBuilder)
{
    var sqlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "PeruShopHub.Infrastructure", "Persistence", "Seeds", "SeedData.sql");

    // For reliability, embed the SQL or read from embedded resource
    var sql = File.ReadAllText(
        Path.Combine(Directory.GetCurrentDirectory(), "..",
            "PeruShopHub.Infrastructure", "Persistence", "Seeds", "SeedData.sql"));

    migrationBuilder.Sql(sql);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        DELETE FROM ""OrderCosts"";
        DELETE FROM ""OrderItems"";
        DELETE FROM ""Orders"";
        DELETE FROM ""ProductVariants"";
        DELETE FROM ""Products"";
        DELETE FROM ""Categories"";
        DELETE FROM ""Customers"";
        DELETE FROM ""Supplies"";
        DELETE FROM ""Notifications"";
        DELETE FROM ""SystemUsers"";
        DELETE FROM ""MarketplaceConnections"";
    ");
}
```

**Better approach:** Embed the SQL file as an embedded resource in the Infrastructure project. Add to `.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="Persistence\Seeds\SeedData.sql" />
</ItemGroup>
```

Then read it in the migration:
```csharp
var assembly = typeof(PeruShopHubDbContext).Assembly;
using var stream = assembly.GetManifestResourceStream("PeruShopHub.Infrastructure.Persistence.Seeds.SeedData.sql");
using var reader = new StreamReader(stream!);
migrationBuilder.Sql(reader.ReadToEnd());
```

- [ ] **Step 3: Apply seed migration**

```bash
dotnet ef database update \
  --project src/PeruShopHub.Infrastructure \
  --startup-project src/PeruShopHub.API
```

- [ ] **Step 4: Verify seed data**

```bash
docker exec perushophub-db psql -U postgres -d perushophub -c "SELECT COUNT(*) FROM \"Products\"; SELECT COUNT(*) FROM \"Orders\"; SELECT COUNT(*) FROM \"Categories\";"
```

Expected: Products=10, Orders=15, Categories=27

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add seed data migration with 27 categories, 10 products, 15 orders"
```

---

## Phase 2 — Infrastructure (4 Parallel Agents)

> All 4 agents start from the merged Phase 1 state on `ralph/backend-wiring`.
> Each works in its own worktree branch.
> After all complete, merge sequentially into `ralph/backend-wiring`.

### Task 2.1: Redis Cache Service (Agent: `infra-redis`)

> **Worktree branch:** `ralph/backend-wiring/infra-redis`

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Cache/RedisCacheService.cs`
- Modify: `src/PeruShopHub.API/Program.cs` (add Redis DI)

- [ ] **Step 1: Implement RedisCacheService**

```csharp
// src/PeruShopHub.Infrastructure/Cache/RedisCacheService.cs
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, ct);
            return data is null ? default : JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(key, json, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Note: IDistributedCache doesn't support prefix deletion natively.
        // For now, log a warning. Full implementation requires direct StackExchange.Redis IConnectionMultiplexer.
        _logger.LogDebug("RemoveByPrefix called for {Prefix} — not implemented with IDistributedCache", prefix);
    }
}
```

- [ ] **Step 2: Add Redis registration to Program.cs**

Add after the DbContext registration:

```csharp
// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "perushophub:";
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
```

Add the using:
```csharp
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Cache;
```

- [ ] **Step 3: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add Redis cache service with graceful fallback"
```

---

### Task 2.2: SignalR Hub (Agent: `infra-signalr`)

> **Worktree branch:** `ralph/backend-wiring/infra-signalr`

**Files:**
- Create: `src/PeruShopHub.API/Hubs/NotificationHub.cs`
- Create: `src/PeruShopHub.Infrastructure/Notifications/SignalRNotificationDispatcher.cs`
- Modify: `src/PeruShopHub.API/Program.cs` (add SignalR DI + endpoint mapping)

- [ ] **Step 1: Create NotificationHub**

```csharp
// src/PeruShopHub.API/Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;

namespace PeruShopHub.API.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
```

- [ ] **Step 2: Create SignalRNotificationDispatcher**

```csharp
// src/PeruShopHub.Infrastructure/Notifications/SignalRNotificationDispatcher.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Infrastructure.Notifications;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly PeruShopHubDbContext _db;
    private readonly IHubContext<DummyHub> _hubContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    // We can't reference API's NotificationHub from Infrastructure.
    // Instead, use IHubContext with a marker interface or accept IHubContext via DI registered in API.
    // Solution: Register from API layer using the concrete hub type.

    private readonly INotificationHubContext _notificationHub;

    public SignalRNotificationDispatcher(
        PeruShopHubDbContext db,
        INotificationHubContext notificationHub,
        ILogger<SignalRNotificationDispatcher> logger)
    {
        _db = db;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    public async Task DispatchAsync(Notification notification, CancellationToken ct = default)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _notificationHub.SendNotificationAsync(new
        {
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Description,
            notification.Timestamp,
            IsRead = false,
            notification.NavigationTarget
        }, ct);

        _logger.LogInformation("Dispatched notification {Id}: {Title}", notification.Id, notification.Title);
    }

    public async Task BroadcastDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default)
    {
        await _notificationHub.SendDataChangeAsync(entityType, action, entityId, ct);
    }
}
```

We need an abstraction so Infrastructure doesn't reference API:

```csharp
// src/PeruShopHub.Core/Interfaces/INotificationHubContext.cs
namespace PeruShopHub.Core.Interfaces;

public interface INotificationHubContext
{
    Task SendNotificationAsync(object notification, CancellationToken ct = default);
    Task SendDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default);
}
```

Then in the API project, create an adapter:

```csharp
// src/PeruShopHub.API/Hubs/NotificationHubContext.cs
using Microsoft.AspNetCore.SignalR;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Hubs;

public class NotificationHubContextAdapter : INotificationHubContext
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubContextAdapter(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(object notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.SendAsync("DataChanged", new { entityType, action, entityId }, ct);
    }
}
```

- [ ] **Step 3: Add SignalR registration to Program.cs**

```csharp
// Add to service registration section:
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("perushophub");
    });

builder.Services.AddSingleton<INotificationHubContext, NotificationHubContextAdapter>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

// Add to endpoint mapping section (after MapControllers):
app.MapHub<NotificationHub>("/hubs/notifications");
```

Add usings:
```csharp
using PeruShopHub.API.Hubs;
using PeruShopHub.Infrastructure.Notifications;
using StackExchange.Redis;
```

- [ ] **Step 4: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add SignalR notification hub with Redis backplane"
```

---

### Task 2.3: Background Workers (Agent: `infra-workers`)

> **Worktree branch:** `ralph/backend-wiring/infra-workers`

**Files:**
- Create: `src/PeruShopHub.Worker/Workers/StockAlertWorker.cs`
- Create: `src/PeruShopHub.Worker/Workers/NotificationCleanupWorker.cs`
- Modify: `src/PeruShopHub.Worker/Program.cs`
- Create: `src/PeruShopHub.Worker/appsettings.json`

- [ ] **Step 1: Write Worker Program.cs**

Note: Worker needs DbContext but does NOT use SignalR directly. It creates Notification entities in the DB. When the API project's SignalR hub is running, real-time push comes from the API side. For this phase, the Worker just writes to the DB.

```csharp
// src/PeruShopHub.Worker/Program.cs
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Database (same connection as API)
builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register workers
builder.Services.AddHostedService<StockAlertWorker>();
builder.Services.AddHostedService<NotificationCleanupWorker>();

var host = builder.Build();
host.Run();
```

- [ ] **Step 2: Write Worker appsettings.json**

```json
// src/PeruShopHub.Worker/appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=perushophub;Username=postgres;Password=dev",
    "Redis": "localhost:6379"
  },
  "Workers": {
    "StockAlert": {
      "IntervalMinutes": 15
    },
    "NotificationCleanup": {
      "IntervalHours": 24,
      "RetentionDays": 30
    }
  }
}
```

- [ ] **Step 3: Create StockAlertWorker**

```csharp
// src/PeruShopHub.Worker/Workers/StockAlertWorker.cs
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class StockAlertWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StockAlertWorker> _logger;
    private readonly TimeSpan _interval;

    public StockAlertWorker(IServiceProvider services, IConfiguration config, ILogger<StockAlertWorker> logger)
    {
        _services = services;
        _logger = logger;
        var minutes = config.GetValue("Workers:StockAlert:IntervalMinutes", 15);
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockAlertWorker started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckStockLevels(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stock levels");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckStockLevels(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var lowStockSupplies = await db.Supplies
            .Where(s => s.Status == "Ativo" && s.Stock <= s.MinimumStock)
            .ToListAsync(ct);

        if (lowStockSupplies.Count == 0)
        {
            _logger.LogDebug("No low-stock supplies found");
            return;
        }

        foreach (var supply in lowStockSupplies)
        {
            // Check if unread notification already exists for this supply
            var existingAlert = await db.Notifications
                .AnyAsync(n => n.Type == "stock"
                    && n.NavigationTarget == $"/suprimentos"
                    && n.Title.Contains(supply.Name)
                    && !n.IsRead, ct);

            if (existingAlert) continue;

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = "stock",
                Title = $"Estoque baixo: {supply.Name}",
                Description = $"{supply.Name} tem apenas {supply.Stock} unidades (mínimo: {supply.MinimumStock})",
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                NavigationTarget = "/suprimentos"
            };

            db.Notifications.Add(notification);
            _logger.LogInformation("Created stock alert for {Supply}: {Stock}/{Min}", supply.Name, supply.Stock, supply.MinimumStock);
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Create NotificationCleanupWorker**

```csharp
// src/PeruShopHub.Worker/Workers/NotificationCleanupWorker.cs
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class NotificationCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationCleanupWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public NotificationCleanupWorker(IServiceProvider services, IConfiguration config, ILogger<NotificationCleanupWorker> logger)
    {
        _services = services;
        _logger = logger;
        var hours = config.GetValue("Workers:NotificationCleanup:IntervalHours", 24);
        _interval = TimeSpan.FromHours(hours);
        _retentionDays = config.GetValue("Workers:NotificationCleanup:RetentionDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationCleanupWorker started. Interval: {Interval}, Retention: {Days} days", _interval, _retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up notifications");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupOldNotifications(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);

        var deleted = await db.Notifications
            .Where(n => n.IsRead && n.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _logger.LogInformation("Deleted {Count} read notifications older than {Days} days", deleted, _retentionDays);
    }
}
```

- [ ] **Step 5: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add StockAlert and NotificationCleanup background workers"
```

---

### Task 2.4: File Upload System (Agent: `infra-files`)

> **Worktree branch:** `ralph/backend-wiring/infra-files`

**Files:**
- Create: `src/PeruShopHub.Infrastructure/Storage/LocalFileStorageService.cs`
- Create: `src/PeruShopHub.API/Controllers/FilesController.cs`
- Modify: `src/PeruShopHub.API/Program.cs` (add file storage DI + static files)

- [ ] **Step 1: Implement LocalFileStorageService**

```csharp
// src/PeruShopHub.Infrastructure/Storage/LocalFileStorageService.cs
using Microsoft.Extensions.Configuration;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration config)
    {
        _basePath = config["FileStorage:BasePath"] ?? "wwwroot/uploads";
    }

    public async Task<string> UploadAsync(Stream file, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var safeName = $"{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";
        var relativePath = Path.Combine(folder, safeName);
        var fullPath = Path.Combine(_basePath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(fs, ct);

        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storagePath)
    {
        return $"/uploads/{storagePath}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
```

- [ ] **Step 2: Create FilesController**

```csharp
// src/PeruShopHub.API/Controllers/FilesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Files;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly IFileStorageService _storage;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public FilesController(PeruShopHubDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadDto>> Upload(
        IFormFile file,
        [FromForm] string entityType,
        [FromForm] Guid entityId,
        [FromForm] int sortOrder = 0,
        CancellationToken ct = default)
    {
        if (file.Length == 0) return BadRequest("Empty file");
        if (file.Length > MaxFileSize) return BadRequest("File exceeds 5MB limit");
        if (!AllowedTypes.Contains(file.ContentType)) return BadRequest("File type not allowed. Accepted: jpg, png, webp");

        await using var stream = file.OpenReadStream();
        var storagePath = await _storage.UploadAsync(stream, file.FileName, file.ContentType, entityType, ct);

        var upload = new FileUpload
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            FileName = file.FileName,
            StoragePath = storagePath,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.FileUploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        return Ok(new FileUploadDto(
            upload.Id,
            _storage.GetPublicUrl(upload.StoragePath),
            upload.FileName,
            upload.ContentType,
            upload.SizeBytes,
            upload.SortOrder));
    }

    [HttpGet]
    public async Task<ActionResult<List<FileUploadDto>>> GetFiles(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken ct)
    {
        var files = await _db.FileUploads
            .Where(f => f.EntityType == entityType && f.EntityId == entityId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FileUploadDto(
                f.Id,
                _storage.GetPublicUrl(f.StoragePath),
                f.FileName,
                f.ContentType,
                f.SizeBytes,
                f.SortOrder))
            .ToListAsync(ct);

        return Ok(files);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var file = await _db.FileUploads.FindAsync([id], ct);
        if (file is null) return NotFound();

        await _storage.DeleteAsync(file.StoragePath, ct);
        _db.FileUploads.Remove(file);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
```

- [ ] **Step 3: Add file storage DI to Program.cs**

```csharp
// Add to service registration:
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// Ensure static files middleware is present (should already be there from Phase 1):
app.UseStaticFiles();
```

Add using:
```csharp
using PeruShopHub.Infrastructure.Storage;
```

- [ ] **Step 4: Create wwwroot/uploads directory**

```bash
mkdir -p src/PeruShopHub.API/wwwroot/uploads
touch src/PeruShopHub.API/wwwroot/uploads/.gitkeep
```

- [ ] **Step 5: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add file upload system with local storage and controller"
```

---

## Phase 3 — API Endpoints (4 Parallel Agents)

> All 4 agents start from merged Phase 1 state (can run concurrently with Phase 2).
> Each creates one controller per domain.
> No shared file conflicts except adding `using` to files that don't exist yet in other branches.

### Task 3.1: Dashboard + Finance Controllers (Agent: `api-analytics`)

> **Worktree branch:** `ralph/backend-wiring/api-analytics`

**Files:**
- Create: `src/PeruShopHub.API/Controllers/DashboardController.cs`
- Create: `src/PeruShopHub.API/Controllers/FinanceController.cs`

- [ ] **Step 1: Create DashboardController**

Endpoints:
- `GET /api/dashboard/summary?period={hoje|7dias|30dias}` — queries orders within period, calculates KPIs
- `GET /api/dashboard/chart/revenue-profit?days=30` — daily aggregation of revenue/profit
- `GET /api/dashboard/chart/cost-breakdown?period=30dias` — sum costs by category
- `GET /api/dashboard/top-products?limit=5` — top products by profit
- `GET /api/dashboard/least-profitable?limit=5` — bottom products by margin
- `GET /api/dashboard/pending-actions` — count unread notifications by type

Each endpoint queries the DbContext directly (no service layer for this phase — keep it simple). Use LINQ to aggregate from Orders, OrderCosts, Products.

Example for summary:
```csharp
[HttpGet("summary")]
public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] string period = "7dias", CancellationToken ct = default)
{
    var (dateFrom, dateTo) = GetDateRange(period);
    var (prevFrom, prevTo) = GetPreviousDateRange(period);

    var currentOrders = await _db.Orders
        .Where(o => o.OrderDate >= dateFrom && o.OrderDate <= dateTo)
        .ToListAsync(ct);

    var previousOrders = await _db.Orders
        .Where(o => o.OrderDate >= prevFrom && o.OrderDate <= prevTo)
        .ToListAsync(ct);

    // Calculate KPIs and changes...
}
```

- [ ] **Step 2: Create FinanceController**

Endpoints:
- `GET /api/finance/summary?period=` — finance-specific KPIs (revenue, total costs, net profit, avg margin, avg ticket)
- `GET /api/finance/chart/revenue-profit?days=30` — daily bar chart data
- `GET /api/finance/chart/margin?days=30` — daily margin % line
- `GET /api/finance/sku-profitability?page=1&pageSize=20&sortBy=margin&sortDir=desc` — per-SKU cost decomposition
- `GET /api/finance/reconciliation?year=2026` — monthly expected vs deposited (from seed data)
- `GET /api/finance/abc-curve` — ABC classification with cumulative profit

SKU profitability joins Orders → OrderItems → OrderCosts grouped by SKU.

- [ ] **Step 3: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add Dashboard and Finance API controllers"
```

---

### Task 3.2: Products + Categories Controllers (Agent: `api-catalog`)

> **Worktree branch:** `ralph/backend-wiring/api-catalog`

**Files:**
- Create: `src/PeruShopHub.API/Controllers/ProductsController.cs`
- Create: `src/PeruShopHub.API/Controllers/CategoriesController.cs`

- [ ] **Step 1: Create ProductsController**

Endpoints:
- `GET /api/products?page=1&pageSize=20&search=&status=&sortBy=name&sortDir=asc`
  - Returns `PagedResult<ProductListDto>`
  - Include variant count and total stock from variants
  - Calculate margin from price vs purchaseCost
  - Populate photo URL from first FileUpload with entityType="product"
- `GET /api/products/{id}` — full detail with variants and photos
- `GET /api/products/{id}/variants` — variants for a product
- `POST /api/products` — create product from `CreateProductDto`
- `PUT /api/products/{id}` — update product from `UpdateProductDto`

Use `ICacheService` if available (inject optionally) for list caching. Invalidate on create/update. Use `INotificationDispatcher` to broadcast `DataChanged("product", "created/updated", id)`.

- [ ] **Step 2: Create CategoriesController**

Endpoints:
- `GET /api/categories?parentId=` — returns direct children (roots when null). Include `hasChildren` boolean.
- `GET /api/categories/{id}` — category detail
- `POST /api/categories` — create
- `PUT /api/categories/{id}` — update
- `DELETE /api/categories/{id}` — delete only if no children or products reference it

Lazy-loading: each call returns one level of children only. The frontend expands nodes on demand.

- [ ] **Step 3: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add Products and Categories API controllers"
```

---

### Task 3.3: Orders + Customers Controllers (Agent: `api-commercial`)

> **Worktree branch:** `ralph/backend-wiring/api-commercial`

**Files:**
- Create: `src/PeruShopHub.API/Controllers/OrdersController.cs`
- Create: `src/PeruShopHub.API/Controllers/CustomersController.cs`

- [ ] **Step 1: Create OrdersController**

Endpoints:
- `GET /api/orders?page=1&pageSize=20&search=&status=&dateFrom=&dateTo=&sortBy=orderDate&sortDir=desc`
  - Returns `PagedResult<OrderListDto>`
  - Search by externalOrderId or buyerName
  - Filter by status and date range
- `GET /api/orders/{id}` — full detail with:
  - Items (from OrderItems)
  - Buyer info (from Customer or Order fields)
  - Shipping info (tracking, carrier, logisticType, timeline derived from status)
  - Payment info (method, installments, amount, status)
  - Cost breakdown (from OrderCosts)
  - Revenue (sum of item subtotals)

For the shipping timeline, derive from order status:
```csharp
var timeline = new List<object>
{
    new { Label = "Pedido realizado", Date = order.OrderDate, Completed = true },
    new { Label = "Pagamento aprovado", Date = order.OrderDate, Completed = order.Status != "Cancelado" },
    new { Label = "Enviado", Date = (object?)null, Completed = order.Status is "Enviado" or "Entregue" },
    new { Label = "Entregue", Date = (object?)null, Completed = order.Status == "Entregue" }
};
```

- [ ] **Step 2: Create CustomersController**

Endpoints:
- `GET /api/customers?page=1&pageSize=20&search=&sortBy=totalSpent&sortDir=desc`
  - Search by name, nickname, or email
- `GET /api/customers/{id}` — customer detail with recent orders

- [ ] **Step 3: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add Orders and Customers API controllers"
```

---

### Task 3.4: Supplies + Settings + Notifications + Search Controllers (Agent: `api-support`)

> **Worktree branch:** `ralph/backend-wiring/api-support`

**Files:**
- Create: `src/PeruShopHub.API/Controllers/SuppliesController.cs`
- Create: `src/PeruShopHub.API/Controllers/SettingsController.cs`
- Create: `src/PeruShopHub.API/Controllers/NotificationsController.cs`
- Create: `src/PeruShopHub.API/Controllers/SearchController.cs`

- [ ] **Step 1: Create SuppliesController**

Endpoints:
- `GET /api/supplies?page=1&pageSize=20&search=&category=&status=&sortBy=name&sortDir=asc`
- `POST /api/supplies` — create supply
- `PUT /api/supplies/{id}` — update supply

- [ ] **Step 2: Create SettingsController**

Endpoints:
- `GET /api/settings/users` — list system users
- `GET /api/settings/integrations` — list marketplace connections
- `GET /api/settings/costs` — return cost config (from appsettings or hardcoded defaults: packaging R$2.50, ICMS 6%)

- [ ] **Step 3: Create NotificationsController**

Endpoints:
- `GET /api/notifications` — list all, newest first
- `PATCH /api/notifications/{id}/read` — mark one as read
- `PATCH /api/notifications/read-all` — mark all as read

- [ ] **Step 4: Create SearchController**

Endpoint:
- `GET /api/search?q=term&limit=10`
  - Search Products by name/SKU
  - Search Orders by externalOrderId/buyerName
  - Search Customers by name/email
  - Return mixed results as `SearchResultDto[]` with type, id, primary, secondary, route
  - Limit results: max 3 per type, 10 total

```csharp
[HttpGet]
public async Task<ActionResult<List<SearchResultDto>>> Search([FromQuery] string q, [FromQuery] int limit = 10, CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return Ok(Array.Empty<SearchResultDto>());

    var results = new List<SearchResultDto>();
    var perType = Math.Min(limit / 3, 5);
    var query = q.ToLower();

    // Products
    var products = await _db.Products
        .Where(p => p.Name.ToLower().Contains(query) || p.Sku.ToLower().Contains(query))
        .Take(perType)
        .Select(p => new SearchResultDto("produto", p.Id.ToString(), p.Name, p.Sku, $"/produtos/{p.Id}"))
        .ToListAsync(ct);
    results.AddRange(products);

    // Orders
    var orders = await _db.Orders
        .Where(o => o.ExternalOrderId.ToLower().Contains(query) || o.BuyerName.ToLower().Contains(query))
        .Take(perType)
        .Select(o => new SearchResultDto("pedido", o.Id.ToString(), $"Pedido #{o.ExternalOrderId}", o.BuyerName, $"/vendas/{o.Id}"))
        .ToListAsync(ct);
    results.AddRange(orders);

    // Customers
    var customers = await _db.Customers
        .Where(c => c.Name.ToLower().Contains(query) || (c.Email != null && c.Email.ToLower().Contains(query)))
        .Take(perType)
        .Select(c => new SearchResultDto("cliente", c.Id.ToString(), c.Name, c.Email ?? "", $"/clientes/{c.Id}"))
        .ToListAsync(ct);
    results.AddRange(customers);

    return Ok(results.Take(limit).ToList());
}
```

- [ ] **Step 5: Verify build and commit**

```bash
dotnet build
git add -A
git commit -m "feat: add Supplies, Settings, Notifications, and Search API controllers"
```

---

## Phase 2+3 Merge

After all Phase 2 and Phase 3 agents complete:

- [ ] **Merge each worktree branch into `ralph/backend-wiring` sequentially**
- [ ] **Resolve Program.cs conflicts** — combine all DI registrations additively
- [ ] **Run `dotnet build`** to verify everything compiles together
- [ ] **Run `dotnet ef database update`** to verify migrations still apply
- [ ] **Start API and verify Swagger** shows all endpoints
- [ ] **Commit merge resolution**

---

## Phase 4a — Frontend Foundation (Sequential, 1 Agent)

> **Agent name:** `frontend-foundation`
> **Worktree branch:** `ralph/backend-wiring/frontend-foundation`
> **Starts after:** Phase 2+3 merge

### Task 4a.1: Angular Proxy, Environment, HttpClient Setup

**Files:**
- Create: `src/PeruShopHub.Web/proxy.conf.json`
- Create: `src/PeruShopHub.Web/src/app/environments/environment.ts`
- Create: `src/PeruShopHub.Web/src/app/environments/environment.development.ts`
- Create: `src/PeruShopHub.Web/src/app/interceptors/error.interceptor.ts`
- Create: `src/PeruShopHub.Web/src/app/models/api.models.ts`
- Modify: `src/PeruShopHub.Web/src/app/app.config.ts`

- [ ] **Step 1: Create proxy.conf.json**

```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  },
  "/hubs": {
    "target": "http://localhost:5000",
    "secure": false,
    "ws": true
  },
  "/uploads": {
    "target": "http://localhost:5000",
    "secure": false
  }
}
```

Note: `angular.json` already has `"proxyConfig": "proxy.conf.json"` in serve options.

- [ ] **Step 2: Create environment files**

```typescript
// src/PeruShopHub.Web/src/app/environments/environment.ts
export const environment = {
  production: true,
  apiUrl: '/api',
  hubUrl: '/hubs/notifications'
};

// src/PeruShopHub.Web/src/app/environments/environment.development.ts
export const environment = {
  production: false,
  apiUrl: '/api',
  hubUrl: '/hubs/notifications'
};
```

- [ ] **Step 3: Create error interceptor**

```typescript
// src/PeruShopHub.Web/src/app/interceptors/error.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 0) {
        toast.show({ message: 'Erro de conexão com o servidor', type: 'danger' });
      } else if (error.status >= 500) {
        toast.show({ message: 'Erro interno do servidor', type: 'danger' });
      } else if (error.status === 404) {
        // Don't toast 404s — let components handle them
      } else if (error.status >= 400) {
        const msg = error.error?.message || error.error?.title || 'Erro na requisição';
        toast.show({ message: msg, type: 'warning' });
      }
      return throwError(() => error);
    })
  );
};
```

- [ ] **Step 4: Create shared API models**

```typescript
// src/PeruShopHub.Web/src/app/models/api.models.ts
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface KpiCard {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  invertColors?: boolean;
}

export interface ChartDataPoint {
  label: string;
  value1: number;
  value2?: number;
}

export interface SearchResult {
  type: 'pedido' | 'produto' | 'cliente';
  id: string;
  primary: string;
  secondary: string;
  route: string;
}

export interface FileUploadResponse {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  sortOrder: number;
}

export interface DataChangeEvent {
  entityType: string;
  action: 'created' | 'updated' | 'deleted';
  entityId: string;
}
```

- [ ] **Step 5: Update app.config.ts**

Add `provideHttpClient` with interceptors:

```typescript
// src/PeruShopHub.Web/src/app/app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideBrowserGlobalErrorListeners } from '@angular/platform-browser';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { routes } from './app.routes';
import { errorInterceptor } from './interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([errorInterceptor])),
    provideCharts(withDefaultRegisterables()),
  ],
};
```

- [ ] **Step 6: Install @microsoft/signalr**

```bash
cd src/PeruShopHub.Web && npm install @microsoft/signalr
```

- [ ] **Step 7: Verify build**

```bash
cd src/PeruShopHub.Web && npx ng build 2>&1 | tail -5
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add Angular proxy, environments, HttpClient with error interceptor"
```

---

### Task 4a.2: Create All Angular Domain Services + SignalR Service

**Files:**
- Create: `src/PeruShopHub.Web/src/app/services/signalr.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/dashboard.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/product.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/order.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/customer.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/supply.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/finance.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/settings.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/search.service.ts`
- Create: `src/PeruShopHub.Web/src/app/services/file-upload.service.ts`

- [ ] **Step 1: Create SignalRService**

```typescript
// src/PeruShopHub.Web/src/app/services/signalr.service.ts
import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../environments/environment';
import { DataChangeEvent } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly _notifications$ = new Subject<any>();
  private readonly _dataChanged$ = new Subject<DataChangeEvent>();

  readonly notifications$ = this._notifications$.asObservable();
  readonly dataChanged$ = this._dataChanged$.asObservable();
  readonly connected = signal(false);

  start(): void {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.connection.on('ReceiveNotification', (notification: any) => {
      this._notifications$.next(notification);
    });

    this.connection.on('DataChanged', (event: DataChangeEvent) => {
      this._dataChanged$.next(event);
    });

    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => this.connected.set(false));

    this.connection.start()
      .then(() => this.connected.set(true))
      .catch(err => console.warn('SignalR connection failed:', err));
  }

  stop(): void {
    this.connection?.stop();
    this.connection = null;
    this.connected.set(false);
  }
}
```

- [ ] **Step 2: Create domain services**

Each service follows the same pattern: inject `HttpClient`, expose methods that return `Observable<T>`. Use `/api/` prefix (proxy handles routing).

Example — `DashboardService`:
```typescript
// src/PeruShopHub.Web/src/app/services/dashboard.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { KpiCard, ChartDataPoint } from '../models/api.models';

export interface DashboardSummary {
  kpis: KpiCard[];
  pendingActions: { label: string; count: number; variant: string }[];
}

export interface ProductRanking {
  id: string; name: string; sales: number; revenue: number; profit: number; margin: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  getSummary(period: string): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`/api/dashboard/summary?period=${period}`);
  }

  getRevenueProfit(days: number): Observable<ChartDataPoint[]> {
    return this.http.get<ChartDataPoint[]>(`/api/dashboard/chart/revenue-profit?days=${days}`);
  }

  getCostBreakdown(period: string): Observable<ChartDataPoint[]> {
    return this.http.get<ChartDataPoint[]>(`/api/dashboard/chart/cost-breakdown?period=${period}`);
  }

  getTopProducts(limit: number): Observable<ProductRanking[]> {
    return this.http.get<ProductRanking[]>(`/api/dashboard/top-products?limit=${limit}`);
  }

  getLeastProfitable(limit: number): Observable<ProductRanking[]> {
    return this.http.get<ProductRanking[]>(`/api/dashboard/least-profitable?limit=${limit}`);
  }
}
```

Create all other services following the same pattern:
- `ProductService` — list, getById, getVariants, create, update
- `OrderService` — list, getById
- `CustomerService` — list, getById
- `SupplyService` — list, create, update
- `FinanceService` — summary, charts, skuProfitability, reconciliation, abcCurve
- `SettingsService` — getUsers, getIntegrations, getCosts
- `SearchService` — search(query)
- `FileUploadService` — upload, getFiles, delete

- [ ] **Step 3: Verify build and commit**

```bash
cd src/PeruShopHub.Web && npx ng build 2>&1 | tail -5
git add -A
git commit -m "feat: add all Angular domain services and SignalR service"
```

---

## Phase 4b — Frontend Wiring (4 Parallel Agents)

> Each agent takes a set of pages and replaces mock data with service calls.
> **Pattern for each component:**
> 1. Import the new service
> 2. Replace MOCK_* constants with service calls in `ngOnInit` or `constructor`
> 3. Update signals/computed values to work with API data
> 4. Remove MOCK_* constants
> 5. Add loading states
> 6. Subscribe to `SignalRService.dataChanged$` for auto-refresh

### Task 4b.1: Wire Dashboard + Finance (Agent: `wire-analytics`)

> **Worktree branch:** `ralph/backend-wiring/wire-analytics`

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/dashboard/dashboard.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/finance/finance.component.ts`

- [ ] **Step 1: Rewire dashboard.component.ts**

Replace all mock data with `DashboardService` calls. Key changes:
- Remove `MOCK_TOP_PROFITABLE`, `MOCK_LEAST_PROFITABLE`, `MOCK_PENDING_ACTIONS`, `MOCK_DATA`
- Remove `generateMockChartData()` function
- Inject `DashboardService` and `SignalRService`
- On period change, call `dashboardService.getSummary(period)` and update signals
- Load chart data from `dashboardService.getRevenueProfit()` and `dashboardService.getCostBreakdown()`
- Subscribe to `signalR.dataChanged$` to refresh on `order` changes

- [ ] **Step 2: Rewire finance.component.ts**

Replace all mock data with `FinanceService` calls. Key changes:
- Remove `MOCK_DATA`, `skuData`, `reconciliationData`, `abcProducts`
- Remove `generateBarChartData()`, `generateMarginChartData()` functions
- Inject `FinanceService`
- Load KPIs, chart data, SKU profitability, reconciliation, ABC curve from API
- Update computed signals to work with async data

- [ ] **Step 3: Verify build and commit**

```bash
cd src/PeruShopHub.Web && npx ng build 2>&1 | tail -5
git add -A
git commit -m "feat: wire dashboard and finance pages to backend API"
```

---

### Task 4b.2: Wire Products + Categories (Agent: `wire-catalog`)

> **Worktree branch:** `ralph/backend-wiring/wire-catalog`

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/products/products-list.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-detail.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/products/product-form.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/services/category.service.ts`
- Modify: `src/PeruShopHub.Web/src/app/services/product-variant.service.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/categories/categories.component.ts`

- [ ] **Step 1: Rewire products-list.component.ts**

- Remove `MOCK_PRODUCTS`
- Inject `ProductService`
- Replace `filteredProducts` computed with API call on search/filter/sort change
- Use `PagedResult` for pagination

- [ ] **Step 2: Rewire product-detail.component.ts**

- Remove mock product data
- Inject `ProductService`
- Call `productService.getById(id)` and `productService.getVariants(id)`

- [ ] **Step 3: Rewire product-form.component.ts**

- Remove `MOCK_PRODUCT`
- Keep `CATEGORIAS` (or load from API)
- Inject `ProductService`, `FileUploadService`
- Load product data from API in edit mode
- Add photo upload via `FileUploadService`

- [ ] **Step 4: Rewire category.service.ts to HTTP**

- Remove all mock seed data and in-memory state
- Inject `HttpClient`
- `getChildren(parentId?)` calls `GET /api/categories?parentId=`
- `getById(id)` calls `GET /api/categories/{id}`
- `create()`, `update()`, `delete()` call API endpoints
- Tree building happens in the component by expanding nodes lazily

- [ ] **Step 5: Rewire product-variant.service.ts to HTTP**

- Remove `SEED_VARIANTS` mock data
- Inject `HttpClient`
- `getByProductId(id)` calls `GET /api/products/{id}/variants`

- [ ] **Step 6: Update categories.component.ts for lazy loading**

- Load root categories on init
- On node expand, call `categoryService.getChildren(nodeId)`

- [ ] **Step 7: Verify build and commit**

```bash
cd src/PeruShopHub.Web && npx ng build 2>&1 | tail -5
git add -A
git commit -m "feat: wire products and categories pages to backend API"
```

---

### Task 4b.3: Wire Sales + Customers (Agent: `wire-commercial`)

> **Worktree branch:** `ralph/backend-wiring/wire-commercial`

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/sales/sales-list.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/sales/sale-detail.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/customers/customers.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/customers/customer-detail.component.ts`

- [ ] **Step 1: Rewire sales-list.component.ts**

- Remove `MOCK_ORDERS`
- Inject `OrderService`
- Call `orderService.list()` with pagination, search, status, date filters

- [ ] **Step 2: Rewire sale-detail.component.ts**

- Remove `MOCK_ORDER`, `MOCK_SUPPLIES`, `COST_CATEGORIES`
- Inject `OrderService`
- Call `orderService.getById(id)` for full detail

- [ ] **Step 3: Rewire customers.component.ts**

- Remove `MOCK_CUSTOMERS`
- Inject `CustomerService`

- [ ] **Step 4: Rewire customer-detail.component.ts**

- Remove mock customer data
- Inject `CustomerService`
- Call `customerService.getById(id)` for detail + order history

- [ ] **Step 5: Verify build and commit**

```bash
cd src/PeruShopHub.Web && npx ng build 2>&1 | tail -5
git add -A
git commit -m "feat: wire sales and customers pages to backend API"
```

---

### Task 4b.4: Wire Supplies + Settings + Search + Notifications (Agent: `wire-support`)

> **Worktree branch:** `ralph/backend-wiring/wire-support`

**Files:**
- Modify: `src/PeruShopHub.Web/src/app/pages/supplies/supplies.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/pages/settings/settings.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/shared/components/search-palette/search-palette.component.ts`
- Modify: `src/PeruShopHub.Web/src/app/services/notification.service.ts`

- [ ] **Step 1: Rewire supplies.component.ts**

- Remove `MOCK_SUPPLIES`
- Keep `CATEGORIES` constant (static enum values)
- Inject `SupplyService`
- CRUD operations call API

- [ ] **Step 2: Rewire settings.component.ts**

- Remove `MOCK_USERS`, `MOCK_INTEGRATIONS`, hardcoded costs
- Inject `SettingsService`

- [ ] **Step 3: Rewire search-palette.component.ts**

- Remove `MOCK_PRODUCTS`, `MOCK_ORDERS`, `MOCK_CUSTOMERS`
- Inject `SearchService`
- `debouncedQuery` triggers `searchService.search(query)`

- [ ] **Step 4: Rewire notification.service.ts**

- Remove `MOCK_NOTIFICATIONS`
- Inject `HttpClient` and `SignalRService`
- Load initial notifications from `GET /api/notifications`
- Merge with live notifications from `signalR.notifications$`
- `markAsRead()` calls `PATCH /api/notifications/{id}/read`
- `markAllAsRead()` calls `PATCH /api/notifications/read-all`
- Init `SignalRService.start()` on service construction (or in app initializer)

- [ ] **Step 5: Verify build and commit**

```bash
cd src/PeruShopHub.Web && npx ng build 2>&1 | tail -5
git add -A
git commit -m "feat: wire supplies, settings, search, and notifications to backend API"
```

---

## Phase 5 — Cleanup (Sequential, 1 Agent)

> **Agent name:** `cleanup`
> **Works directly on:** `ralph/backend-wiring` (after all Phase 4b merges)

### Task 5.1: Verify No Mock Data Remains

- [ ] **Step 1: Search for MOCK_ constants**

```bash
cd src/PeruShopHub.Web && grep -r "MOCK_" --include="*.ts" src/app/ || echo "No MOCK_ found"
```

Expected: "No MOCK_ found" (or only in test files if any)

- [ ] **Step 2: Search for hardcoded data arrays**

```bash
cd src/PeruShopHub.Web && grep -rn "const.*\[" --include="*.ts" src/app/pages/ | grep -v "import\|export\|type\|interface\|enum\|TABS\|CATEGORIES\|COST_CATEGORIES"
```

Review any remaining hardcoded arrays — static UI constants (tabs, categories enum) are OK, data arrays are not.

- [ ] **Step 3: Verify builds**

```bash
dotnet build
cd src/PeruShopHub.Web && npx ng build
```

Both must pass with zero errors.

- [ ] **Step 4: End-to-end smoke test**

```bash
# Ensure PostgreSQL and Redis are running
docker start perushophub-db 2>/dev/null
docker start perushophub-redis 2>/dev/null || docker run -d --name perushophub-redis -p 6379:6379 redis:7-alpine

# Apply migrations
dotnet ef database update --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API

# Start API (background)
dotnet run --project src/PeruShopHub.API &

# Start Worker (background)
dotnet run --project src/PeruShopHub.Worker &

# Verify health
curl http://localhost:5000/health

# Verify Swagger
curl -s http://localhost:5000/swagger/v1/swagger.json | head -20

# Start Angular
cd src/PeruShopHub.Web && npx ng serve &

# Test key endpoints
curl -s http://localhost:5000/api/dashboard/summary?period=7dias | head -5
curl -s http://localhost:5000/api/products?page=1\&pageSize=5 | head -5
curl -s http://localhost:5000/api/orders?page=1\&pageSize=5 | head -5
curl -s http://localhost:5000/api/search?q=fone | head -5
```

- [ ] **Step 5: Commit any fixes**

```bash
git add -A
git commit -m "chore: final cleanup — verify zero mock data, all builds pass"
```

---

## Phase 6 — Team Lead Review (1 Code Reviewer Agent)

> **Agent type:** `code-reviewer`
> **Works on:** `ralph/backend-wiring` (after Phase 5)

### Review Checklist

The team lead agent should verify:

**PRD Compliance:**
- [ ] Walk through every US acceptance criteria in `tasks/prd-backend-wiring.md`
- [ ] Flag any criteria that are missed or partially implemented

**Wiring Integrity:**
- [ ] Every Angular service method has a corresponding backend endpoint
- [ ] Every backend endpoint is called by the frontend (no orphans)
- [ ] Response DTO shapes match Angular interface definitions (property names, types)
- [ ] Enum values consistent between C# and TypeScript

**Data Flow:**
- [ ] Seed data → DB → API → Angular service → Component renders
- [ ] SignalR: API dispatches → hub → Angular SignalRService → UI updates
- [ ] File upload: Angular → API → disk → URL in DB → rendered in component
- [ ] Workers: timer → DB check → notification created → SignalR pushes → UI shows

**Build & Runtime:**
- [ ] `dotnet build` — zero errors and warnings
- [ ] `ng build` — zero errors
- [ ] No `MOCK_` references in frontend
- [ ] No hardcoded `localhost` URLs in Angular services
- [ ] `Program.cs` DI is complete (no missing registrations)

**Architecture:**
- [ ] Dependency rule: Core has no references; Infrastructure/Application → Core; API → Application + Infrastructure
- [ ] No circular references
- [ ] Interfaces in Core, implementations in Infrastructure
- [ ] Worker doesn't reference API

**Consistency:**
- [ ] All list endpoints return `PagedResult<T>` format
- [ ] All monetary values use `decimal` (C#) / `NUMERIC(18,4)` (SQL)
- [ ] Error interceptor handles all HTTP error codes
- [ ] Cache invalidation on every write endpoint
- [ ] SignalR DataChanged broadcast on every create/update/delete

**Report format:** Create a review document at `docs/superpowers/plans/2026-03-23-backend-wiring-review.md` with findings, organized as PASS/FAIL per checklist item, with specific file:line references for any issues found.

---

## Appendix A — Final Merged Program.cs (API)

After all Phase 2 and Phase 3 agents merge, `src/PeruShopHub.API/Program.cs` must look like this:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.API.Hubs;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Cache;
using PeruShopHub.Infrastructure.Notifications;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Infrastructure.Storage;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Redis Cache (Phase 2 — infra-redis) ──────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "perushophub:";
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ── SignalR + Redis Backplane (Phase 2 — infra-signalr) ──
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("perushophub");
    });
builder.Services.AddSingleton<INotificationHubContext, NotificationHubContextAdapter>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

// ── File Storage (Phase 2 — infra-files) ─────────────────
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// ── Controllers + JSON ───────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR WebSocket
    });
});

// ── Swagger ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Health Checks ─────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles(); // Serves wwwroot/uploads for file uploads

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();
```

Use this as the merge target. Each Phase 2 agent adds their clearly marked section. During merge, verify no duplicate registrations.

---

## Appendix B — Controller Pattern Template

All controllers follow this pattern for write operations (cache invalidation + SignalR broadcast):

```csharp
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ExampleController(
        PeruShopHubDbContext db,
        ICacheService cache,
        INotificationDispatcher notificationDispatcher)
    {
        _db = db;
        _cache = cache;
        _notificationDispatcher = notificationDispatcher;
    }

    [HttpPost]
    public async Task<ActionResult<ExampleDto>> Create(CreateExampleDto dto, CancellationToken ct)
    {
        var entity = new Example { /* map from dto */ };
        _db.Examples.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Invalidate cache
        await _cache.RemoveByPrefixAsync("example:", ct);

        // Broadcast change to connected clients
        await _notificationDispatcher.BroadcastDataChangeAsync("example", "created", entity.Id.ToString(), ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }
}
```

Read-only endpoints use caching:

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<ExampleDto>>> List(
    [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
{
    var cacheKey = $"example:list:{page}:{pageSize}";
    var cached = await _cache.GetAsync<PagedResult<ExampleDto>>(cacheKey, ct);
    if (cached is not null) return Ok(cached);

    var query = _db.Examples.AsNoTracking();
    var totalCount = await query.CountAsync(ct);
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => MapToDto(e))
        .ToListAsync(ct);

    var result = new PagedResult<ExampleDto>
    {
        Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
    };

    await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
    return Ok(result);
}
```

---

## Appendix C — HTTP Status Code Convention

All controllers use these status codes consistently:

| Code | When | Example |
|------|------|---------|
| `200 OK` | Successful GET or list | GET /api/products |
| `201 Created` | Resource created | POST /api/products |
| `204 No Content` | Successful update/delete | PUT /api/products/{id}, DELETE /api/files/{id} |
| `400 Bad Request` | Invalid input (missing field, bad format) | Empty file upload, invalid period |
| `404 Not Found` | Resource doesn't exist | GET /api/products/{nonexistent-id} |
| `409 Conflict` | Unique constraint violation | POST /api/products with duplicate SKU |
| `422 Unprocessable Entity` | Business rule violation | DELETE /api/categories/{id} when it has children |

Angular error interceptor handles:
- `0` → "Erro de conexão com o servidor" (connection error)
- `400-499` → Show `error.error.message` or `error.error.title`
- `500+` → "Erro interno do servidor"
- `404` → Silently ignored (component handles NotFound)
