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
