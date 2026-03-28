using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Cache;
using PeruShopHub.Infrastructure.Email;
using PeruShopHub.Infrastructure.Marketplace;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Infrastructure.Security;
using PeruShopHub.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, NoOpEmailService>();

// Data Protection + Token Encryption (needed for token renewal)
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("PeruShopHub");
builder.Services.AddSingleton<ITokenEncryptionService, TokenEncryptionService>();

// Marketplace adapters (needed for token refresh)
builder.Services.AddHttpClient("MercadoLivre", client =>
{
    client.BaseAddress = new Uri("https://api.mercadolibre.com");
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
}).AddMercadoLivreResilience();
builder.Services.AddKeyedScoped<IMarketplaceAdapter, MercadoLivreAdapter>("mercadolivre");

// Redis Cache (needed for import job queue)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "perushophub:";
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ML Listing Import
builder.Services.AddScoped<IMlListingImportService, MlListingImportService>();

builder.Services.AddHostedService<MlListingImportWorker>();
builder.Services.AddHostedService<StockAlertWorker>();
builder.Services.AddHostedService<NotificationCleanupWorker>();
builder.Services.AddHostedService<SkuProfitabilityRefreshWorker>();
builder.Services.AddHostedService<ReportEmailWorker>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<TokenRenewalWorker>();

var host = builder.Build();
host.Run();
