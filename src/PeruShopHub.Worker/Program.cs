using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Cache;
using PeruShopHub.Infrastructure.Email;
using PeruShopHub.Infrastructure.Marketplace;
using PeruShopHub.Infrastructure.Notifications;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Infrastructure.Security;
using PeruShopHub.Infrastructure.Services;
using PeruShopHub.Infrastructure.Storage;
using PeruShopHub.Worker.Workers;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEmailService(builder.Configuration);

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

// Redis Connection (needed for webhook queue operations)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IWebhookQueueService, RedisWebhookQueueService>();

// Notification dispatcher (DB-only, no SignalR in Worker)
builder.Services.AddScoped<INotificationDispatcher, DbOnlyNotificationDispatcher>();
builder.Services.AddScoped<INotificationEmailService, NotificationEmailService>();

// Cost calculation service (needed for webhook order processing)
builder.Services.AddScoped<ICostCalculationService, CostCalculationService>();

// File Storage (needed for photo sync during import)
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// ML Listing Import & Product Sync
builder.Services.AddScoped<IMlPhotoSyncService, MlPhotoSyncService>();
builder.Services.AddScoped<IMlListingImportService, MlListingImportService>();
builder.Services.AddScoped<IProductSyncService, ProductSyncService>();
builder.Services.AddScoped<IOrderSyncService, OrderSyncService>();
builder.Services.AddScoped<IStockSyncService, StockSyncService>();
builder.Services.AddSingleton<IMlOrderMapper, MlOrderMapper>();

builder.Services.AddHostedService<MlListingImportWorker>();
builder.Services.AddHostedService<OrderSyncWorker>();
builder.Services.AddHostedService<StockAlertWorker>();
builder.Services.AddHostedService<NotificationCleanupWorker>();
builder.Services.AddHostedService<SkuProfitabilityRefreshWorker>();
builder.Services.AddHostedService<ReportEmailWorker>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<TokenRenewalWorker>();
builder.Services.AddHostedService<ProductSyncWorker>();
builder.Services.AddHostedService<WebhookProcessingWorker>();
builder.Services.AddHostedService<StockSyncWorker>();
builder.Services.AddHostedService<BillingReconciliationWorker>();
builder.Services.AddScoped<IStockReconciliationService, StockReconciliationService>();
builder.Services.AddHostedService<StockReconciliationWorker>();
builder.Services.AddHostedService<StorageCostWorker>();
builder.Services.AddScoped<IMarketplaceQuestionService, MarketplaceQuestionService>();
builder.Services.AddHostedService<QuestionSyncWorker>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddHostedService<ClaimSyncWorker>();
builder.Services.AddHostedService<ResponseTimeAlertWorker>();
builder.Services.AddScoped<IUserDataExportService, UserDataExportService>();
builder.Services.AddHostedService<UserDataExportWorker>();

var host = builder.Build();
host.Run();
