using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.API.Hubs;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Cache;
using PeruShopHub.Infrastructure.Notifications;
using PeruShopHub.Infrastructure.Persistence;
using PeruShopHub.Infrastructure.Storage;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<PeruShopHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Redis Cache ───────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "perushophub:";
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ── SignalR + Redis Backplane ─────────────────────────────
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("perushophub");
    });
builder.Services.AddSingleton<INotificationHubContext, NotificationHubContextAdapter>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

// ── File Storage ──────────────────────────────────────────
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// ── Controllers + JSON ────────────────────────────────────
builder.Services.AddControllers()
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
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();
