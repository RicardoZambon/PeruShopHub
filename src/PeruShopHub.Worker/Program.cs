using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<StockAlertWorker>();
builder.Services.AddHostedService<NotificationCleanupWorker>();
builder.Services.AddHostedService<SkuProfitabilityRefreshWorker>();

var host = builder.Build();
host.Run();
