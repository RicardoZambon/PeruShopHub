# Multi-Tenancy

Guia tecnico sobre a arquitetura de multi-tenancy do PeruShopHub.

## Decisao Arquitetural

O PeruShopHub usa **banco de dados compartilhado com query filters do EF Core** (nao bancos separados). Essa abordagem foi escolhida por:

- Simplicidade operacional (um unico banco para gerenciar)
- Custo reduzido (nao precisa de N instancias de banco)
- Migrations unificadas
- Adequada para o volume esperado de tenants

Cada registro tenant-scoped possui uma coluna `TenantId` (GUID) que identifica a qual loja pertence.

---

## Entidades Base

### Tenant

```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; }       // Nome da loja
    public string Slug { get; set; }       // Auto-gerado a partir do nome (URL-safe)
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<TenantUser> Members { get; set; }
}
```

O `Slug` e gerado automaticamente no registro, com tratamento de acentos e caracteres especiais. Se o slug ja existir, um sufixo numerico e adicionado (ex: `minha-loja-2`).

### TenantUser

Tabela de juncao entre `SystemUser` e `Tenant`, com papel (role):

```csharp
public class TenantUser
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; }       // "Owner", "Admin", "Manager", "Viewer"
    public DateTime CreatedAt { get; set; }
}
```

Roles disponiveis:

| Role | Permissoes |
|------|-----------|
| **Owner** | Tudo + gestao de tenant (mesmo que Admin, mas proprietario) |
| **Admin** | Configuracoes, gestao de usuarios, todos os dados |
| **Manager** | CRUD de produtos, categorias, pedidos, estoque |
| **Viewer** | Somente leitura em todos os dados |

Um usuario pode pertencer a multiplos tenants (com roles diferentes em cada um).

---

## Interface ITenantScoped

Marker interface que identifica entidades filtradas por tenant:

```csharp
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
```

**18 entidades** implementam essa interface:

- Product, ProductVariant, ProductCostHistory
- Category, VariationField
- Order, OrderItem, OrderCost
- Customer
- StockMovement
- Supply
- PurchaseOrder, PurchaseOrderItem, PurchaseOrderCost
- CommissionRule
- MarketplaceConnection
- Notification
- FileUpload

Entidades que **nao** sao tenant-scoped:
- `Tenant` (a propria entidade)
- `TenantUser` (tabela de juncao)
- `SystemUser` (compartilhado entre tenants)

---

## ITenantContext

Interface injetada no `PeruShopHubDbContext` para fornecer contexto do tenant atual:

```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsSuperAdmin { get; }
    void Set(Guid? tenantId, bool isSuperAdmin);
}
```

Implementacao: `TenantContext` (registrado como scoped no DI).

---

## TenantMiddleware

Middleware que extrai informacoes do tenant a partir do JWT e configura o `ITenantContext`:

### Fluxo

1. Verifica se a rota deve ser ignorada (login, register, refresh, health, hubs, swagger)
2. Se usuario nao autenticado → passa adiante sem tenant
3. Se **super-admin**:
   - Verifica header `X-Tenant-Id` (permite impersonar qualquer tenant)
   - Fallback: usa claim `tenant_id` do JWT
4. Se **usuario normal**:
   - Extrai `tenant_id` do JWT
   - Se nao tiver tenant e rota nao e `/api/auth/*` → retorna 403
5. Chama `tenantContext.Set(tenantId, isSuperAdmin)`

```csharp
// Super-admin pode impersonar via header
var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
```

---

## Global Query Filters

O `PeruShopHubDbContext` aplica filtros automaticos a todas as entidades `ITenantScoped`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
        {
            // Aplica filtro via reflection para cada entidade
            ApplyTenantFilter<TEntity>(modelBuilder);
        }
    }
}

private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
    where TEntity : class, ITenantScoped
{
    modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
        _tenantContext == null ||
        _tenantContext.IsSuperAdmin ||
        e.TenantId == _tenantContext.TenantId);
}
```

O filtro garante que:
- Se `_tenantContext` e nulo → sem filtro (cenario de migration/seed)
- Se `IsSuperAdmin = true` → ve todos os dados (bypass)
- Caso contrario → so ve dados do proprio tenant

### Auto-Assign de TenantId

No `SaveChangesAsync`, entidades novas com `TenantId = Guid.Empty` recebem automaticamente o `TenantId` do contexto:

```csharp
foreach (var entry in ChangeTracker.Entries<ITenantScoped>()
    .Where(e => e.State == EntityState.Added && e.Entity.TenantId == Guid.Empty))
{
    entry.Entity.TenantId = _tenantContext.TenantId.Value;
}
```

---

## Indices Unicos Tenant-Scoped

Campos que devem ser unicos **dentro de um tenant** (nao globalmente):

| Entidade | Campo | Indice |
|----------|-------|--------|
| Product | Sku | `IX_Products_TenantId_Sku` (unique) |
| Category | Slug | `IX_Categories_TenantId_Slug` (unique) |
| Order | ExternalOrderId | `IX_Orders_TenantId_ExternalOrderId` (unique) |
| MarketplaceConnection | MarketplaceId | `IX_MarketplaceConnections_TenantId_MarketplaceId` (unique) |

Isso permite que dois tenants tenham o mesmo SKU "ABC-001" sem conflito.

A migration `FixTenantScopedUniqueIndexes` converteu indices globais para tenant-scoped.

---

## Self-Service Signup

O endpoint `POST /api/auth/register` cria tudo em uma unica transacao:

1. Valida dados (nome, email, senha)
2. Gera slug a partir do nome da loja
3. Cria `Tenant` (nome, slug, ativo)
4. Cria `SystemUser` (nome, email, senha hashada com BCrypt)
5. Cria `TenantUser` (role = "Owner")
6. Gera tokens JWT
7. Retorna `AuthResponse` com token e dados do usuario

```csharp
var tenant = new Tenant { Name = "Minha Loja", Slug = "minha-loja" };
var user = new SystemUser { Email = "user@email.com", PasswordHash = BCrypt.HashPassword(...) };
var membership = new TenantUser { TenantId = tenant.Id, UserId = user.Id, Role = "Owner" };
```

---

## Switch Tenant

Usuarios com acesso a multiplos tenants podem trocar de contexto:

### Fluxo

1. `GET /api/auth/tenants` — lista tenants do usuario (com role em cada)
2. `POST /api/auth/switch-tenant` — recebe `TenantId`, gera novo JWT com claims do novo tenant
3. Frontend atualiza token e recarrega dados

Para super-admins sem membership no tenant alvo, o sistema cria um contexto virtual de "Admin".

---

## Migracao de Dados Existentes

A migration `AddMultiTenancy` incluiu migracao de dados:

1. Criou tenant "demo" padrao
2. Associou todos os registros existentes ao tenant demo
3. Criou `TenantUser` para usuarios existentes como "Owner" do tenant demo

Isso garantiu que dados pre-existentes continuaram acessiveis apos a implementacao de multi-tenancy.

---

## Frontend

### TenantGuard

Guard Angular que garante que o usuario tem um tenant ativo selecionado. Redireciona para a pagina de selecao de tenant se nenhum estiver ativo.

### TenantService

Servico Angular que gerencia o estado do tenant:
- Armazena tenant ativo
- Fornece metodos para listar e trocar tenants
- Atualiza token no `AuthService` apos troca

### Admin Page (Super-Admin)

Pagina exclusiva para super-admins que permite:
- Listar todos os tenants do sistema
- Ativar/desativar tenants
- Ver membros de cada tenant
- Impersonar qualquer tenant

Protegida pelo `SuperAdminGuard`.

---

## Arquivos Relevantes

| Arquivo | Descricao |
|---------|-----------|
| `src/PeruShopHub.Core/Entities/Tenant.cs` | Entidade Tenant |
| `src/PeruShopHub.Core/Entities/TenantUser.cs` | Tabela de juncao |
| `src/PeruShopHub.Core/Interfaces/ITenantScoped.cs` | Marker interface |
| `src/PeruShopHub.Core/Interfaces/ITenantContext.cs` | Interface de contexto |
| `src/PeruShopHub.Infrastructure/Persistence/TenantContext.cs` | Implementacao do contexto |
| `src/PeruShopHub.Infrastructure/Persistence/PeruShopHubDbContext.cs` | Query filters e auto-assign |
| `src/PeruShopHub.API/Middleware/TenantMiddleware.cs` | Middleware de resolucao |
| `src/PeruShopHub.Web/src/app/guards/tenant.guard.ts` | Guard Angular |
| `src/PeruShopHub.Web/src/app/services/tenant.service.ts` | Servico Angular |
| `src/PeruShopHub.Web/src/app/pages/admin/admin-tenants.component.ts` | Pagina admin |
