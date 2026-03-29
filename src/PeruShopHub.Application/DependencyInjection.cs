using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Application.Services;

namespace PeruShopHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplyService, SupplyService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IFulfillmentCompareService, FulfillmentCompareService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IMlListingImportService, MlListingImportService>();
        services.AddScoped<IMarketplaceListingService, MarketplaceListingService>();
        services.AddScoped<IMlPhotoSyncService, MlPhotoSyncService>();
        services.AddScoped<IProductSyncService, ProductSyncService>();
        services.AddScoped<IOrderSyncService, OrderSyncService>();
        services.AddScoped<IStockSyncService, StockSyncService>();
        services.AddSingleton<IMlOrderMapper, MlOrderMapper>();
        services.AddScoped<IStockReconciliationService, StockReconciliationService>();

        return services;
    }
}
