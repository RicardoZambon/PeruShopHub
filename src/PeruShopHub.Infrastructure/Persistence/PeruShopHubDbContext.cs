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
    public DbSet<StockAllocation> StockAllocations => Set<StockAllocation>();
    public DbSet<TaxProfile> TaxProfiles => Set<TaxProfile>();
    public DbSet<PaymentFeeRule> PaymentFeeRules => Set<PaymentFeeRule>();
    public DbSet<SkuProfitabilityView> SkuProfitabilityViews => Set<SkuProfitabilityView>();
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();
    public DbSet<StockReconciliationReport> StockReconciliationReports => Set<StockReconciliationReport>();
    public DbSet<StockReconciliationReportItem> StockReconciliationReportItems => Set<StockReconciliationReportItem>();
    public DbSet<StorageCostAccumulation> StorageCostAccumulations => Set<StorageCostAccumulation>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<OnboardingProgress> OnboardingProgresses => Set<OnboardingProgress>();
    public DbSet<MarketplaceQuestion> MarketplaceQuestions => Set<MarketplaceQuestion>();

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
