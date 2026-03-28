using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PeruShopHub.API.Filters;
using PeruShopHub.API.Hubs;
using PeruShopHub.API.Middleware;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Cache;
using PeruShopHub.Infrastructure.Notifications;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Infrastructure.Services;
using PeruShopHub.Application;
using PeruShopHub.Infrastructure.Marketplace;
using Microsoft.AspNetCore.DataProtection;
using PeruShopHub.Infrastructure.Security;
using PeruShopHub.Infrastructure.Storage;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

// ── Bootstrap Logger (captures startup errors) ──────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

// ── QuestPDF Community License ────────────────────────────
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ── Sentry ───────────────────────────────────────────────────
var sentryDsn = builder.Configuration["SENTRY_DSN"]
    ?? Environment.GetEnvironmentVariable("SENTRY_DSN");

if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName.ToLowerInvariant();
        options.SendDefaultPii = false;
        options.MaxBreadcrumbs = 50;
        options.TracesSampleRate = builder.Environment.IsProduction() ? 0.2 : 1.0;
        options.SetBeforeSend((sentryEvent, _) =>
        {
            // Sanitize headers — remove sensitive ones
            if (sentryEvent.Request?.Headers != null)
            {
                var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-Api-Key" };
                foreach (var header in sensitiveHeaders)
                {
                    if (sentryEvent.Request.Headers.ContainsKey(header))
                    {
                        sentryEvent.Request.Headers[header] = "[Redacted]";
                    }
                }
            }
            return sentryEvent;
        });
    });
}

// ── Serilog ──────────────────────────────────────────────────
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "PeruShopHub")
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.File(
        new RenderedCompactJsonFormatter(),
        path: "logs/perushophub-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000));

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Data Protection (token encryption at rest) ──────────
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("PeruShopHub");
builder.Services.AddSingleton<ITokenEncryptionService, TokenEncryptionService>();

// ── Redis Cache ───────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "perushophub:";
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ── Redis Connection (for webhook queue list operations) ─
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IWebhookQueueService, RedisWebhookQueueService>();

// ── SignalR + Redis Backplane ─────────────────────────────
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("perushophub");
    });
builder.Services.AddSingleton<INotificationHubContext, NotificationHubContextAdapter>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
builder.Services.AddScoped<IEmailService, PeruShopHub.Infrastructure.Email.NoOpEmailService>();

// ── File Storage ──────────────────────────────────────────
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// ── Cost Calculation ─────────────────────────────────────
builder.Services.AddScoped<ICostCalculationService, CostCalculationService>();

// ── Marketplace Adapters ─────────────────────────────────
builder.Services.AddHttpClient("MercadoLivre", client =>
{
    client.BaseAddress = new Uri("https://api.mercadolibre.com");
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
}).AddMercadoLivreResilience();
builder.Services.AddKeyedScoped<IMarketplaceAdapter, MercadoLivreAdapter>("mercadolivre");

// ── Application Services ─────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices();

// -- Tenant Context --
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ── Authentication ──────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var secret = builder.Configuration["Jwt:Secret"]!;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero,
    };

    // Allow JWT in SignalR query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

// ── Controllers + JSON ────────────────────────────────────
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    })
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
            .AllowCredentials();
    });
});

// ── Swagger ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Health Checks ─────────────────────────────────────────
var healthCheckInterval = builder.Configuration.GetValue("HealthChecks:EvaluationIntervalSeconds", 30);

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(5))
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
        name: "redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(5))
    .AddDiskStorageHealthCheck(
        setup => setup.AddDrive("/", minimumFreeMegabytes: 512),
        name: "disk-space",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(5));

builder.Services
    .AddHealthChecksUI(setup =>
    {
        setup.SetEvaluationTimeInSeconds(healthCheckInterval);
        setup.MaximumHistoryEntriesPerEndpoint(50);
        setup.AddHealthCheckEndpoint("PeruShopHub API", "/health");
    })
    .AddInMemoryStorage();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<TenantRateLimitMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// ── Health Check Endpoints ──────────────────────────────
// Aggregated status: returns Healthy/Degraded/Unhealthy with individual check results
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Readiness probe: checks PostgreSQL, Redis, disk space
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Liveness probe: returns 200 if process is running (no dependency checks)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Health Check UI
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class accessible for WebApplicationFactory<Program> in integration tests
public partial class Program { }
