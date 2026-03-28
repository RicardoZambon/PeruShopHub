using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Email;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, NoOpEmailService>();

builder.Services.AddHostedService<StockAlertWorker>();
builder.Services.AddHostedService<NotificationCleanupWorker>();
builder.Services.AddHostedService<SkuProfitabilityRefreshWorker>();
builder.Services.AddHostedService<ReportEmailWorker>();

var host = builder.Build();
host.Run();
