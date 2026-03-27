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

        return services;
    }
}
