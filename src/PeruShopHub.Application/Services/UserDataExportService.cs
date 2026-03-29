using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeruShopHub.Application.DTOs.Profile;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class UserDataExportService : IUserDataExportService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<UserDataExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public UserDataExportService(
        PeruShopHubDbContext db,
        IFileStorageService fileStorage,
        INotificationDispatcher notificationDispatcher,
        ILogger<UserDataExportService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<UserDataExportDto> RequestExportAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        // Check for existing pending/processing export
        var existing = await _db.UserDataExports
            .Where(e => e.UserId == userId && (e.Status == "Pending" || e.Status == "Processing"))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return MapToDto(existing);

        var export = new UserDataExport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };

        _db.UserDataExports.Add(export);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User data export requested: {ExportId} for user {UserId}", export.Id, userId);

        return MapToDto(export);
    }

    public async Task<UserDataExportDto?> GetExportStatusAsync(Guid exportId, Guid userId, CancellationToken ct = default)
    {
        var export = await _db.UserDataExports
            .Where(e => e.Id == exportId && e.UserId == userId)
            .FirstOrDefaultAsync(ct);

        return export is null ? null : MapToDto(export);
    }

    public async Task<(byte[] Data, string FileName)?> DownloadExportAsync(Guid exportId, Guid userId, CancellationToken ct = default)
    {
        var export = await _db.UserDataExports
            .Where(e => e.Id == exportId && e.UserId == userId && e.Status == "Completed")
            .FirstOrDefaultAsync(ct);

        if (export is null || export.FilePath is null)
            return null;

        if (export.ExpiresAt.HasValue && export.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        // Read from local file storage
        var fullPath = Path.Combine("uploads", export.FilePath);
        if (!File.Exists(fullPath))
            return null;

        var data = await File.ReadAllBytesAsync(fullPath, ct);
        var fileName = $"dados_pessoais_{export.CreatedAt:yyyyMMdd_HHmmss}.zip";
        return (data, fileName);
    }

    public async Task ProcessPendingExportsAsync(CancellationToken ct = default)
    {
        var pendingExports = await _db.UserDataExports
            .IgnoreQueryFilters()
            .Where(e => e.Status == "Pending")
            .OrderBy(e => e.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var export in pendingExports)
        {
            try
            {
                export.Status = "Processing";
                await _db.SaveChangesAsync(ct);

                var zipBytes = await GenerateExportAsync(export.UserId, export.TenantId, ct);

                // Store the file
                using var stream = new MemoryStream(zipBytes);
                var storagePath = await _fileStorage.UploadAsync(
                    stream,
                    $"export_{export.Id}.zip",
                    "application/zip",
                    "exports",
                    ct);

                export.Status = "Completed";
                export.FilePath = storagePath;
                export.CompletedAt = DateTime.UtcNow;
                export.ExpiresAt = DateTime.UtcNow.AddHours(24);
                await _db.SaveChangesAsync(ct);

                // Send notification
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = export.TenantId,
                    Type = "DataExportReady",
                    Title = "Exportação de dados concluída",
                    Description = "Seus dados estão prontos para download. O link expira em 24 horas.",
                    Timestamp = DateTime.UtcNow,
                    NavigationTarget = "/profile",
                };
                await _notificationDispatcher.DispatchAsync(notification, ct);

                _logger.LogInformation("User data export completed: {ExportId}", export.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process user data export: {ExportId}", export.Id);
                export.Status = "Failed";
                export.ErrorMessage = ex.Message;
                await _db.SaveChangesAsync(ct);
            }
        }

        // Clean up expired exports
        var expired = await _db.UserDataExports
            .IgnoreQueryFilters()
            .Where(e => e.Status == "Completed" && e.ExpiresAt != null && e.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var exp in expired)
        {
            if (exp.FilePath is not null)
            {
                try { await _fileStorage.DeleteAsync(exp.FilePath, ct); } catch { /* ignore */ }
            }
            exp.Status = "Expired";
            exp.FilePath = null;
        }

        if (expired.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private async Task<byte[]> GenerateExportAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        // Gather all user data
        var user = await _db.SystemUsers
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.AvatarUrl,
                u.LastLogin,
                u.CreatedAt,
                u.TermsAcceptedAt,
                u.PrivacyAcceptedAt,
            })
            .FirstOrDefaultAsync(ct);

        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Id, t.Name, t.Slug, t.CreatedAt })
            .FirstOrDefaultAsync(ct);

        var products = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Sku,
                p.Description,
                p.Supplier,
                p.PurchaseCost,
                p.Price,
                p.CreatedAt,
                p.UpdatedAt,
            })
            .ToListAsync(ct);

        var orders = await _db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId)
            .Select(o => new
            {
                o.Id,
                o.ExternalOrderId,
                o.Status,
                o.TotalAmount,
                o.OrderDate,
                o.CreatedAt,
            })
            .ToListAsync(ct);

        var orderCosts = await _db.OrderCosts
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .Select(c => new
            {
                c.Id,
                c.OrderId,
                c.Category,
                c.Value,
                c.Source,
            })
            .ToListAsync(ct);

        var customers = await _db.Customers
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Email,
                c.Nickname,
                c.CreatedAt,
            })
            .ToListAsync(ct);

        var stockMovements = await _db.StockMovements
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId)
            .Select(s => new
            {
                s.Id,
                s.ProductId,
                s.VariantId,
                s.Type,
                s.Quantity,
                s.Reason,
                s.CreatedAt,
            })
            .ToListAsync(ct);

        var questions = await _db.MarketplaceQuestions
            .IgnoreQueryFilters()
            .Where(q => q.TenantId == tenantId)
            .Select(q => new
            {
                q.Id,
                q.ExternalId,
                q.ExternalItemId,
                q.QuestionText,
                q.AnswerText,
                q.Status,
                q.CreatedAt,
            })
            .ToListAsync(ct);

        var messages = await _db.MarketplaceMessages
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == tenantId)
            .Select(m => new
            {
                m.Id,
                m.ExternalPackId,
                m.OrderId,
                m.Text,
                m.SenderType,
                m.CreatedAt,
            })
            .ToListAsync(ct);

        var notifications = await _db.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.Timestamp)
            .Take(500)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Description,
                n.Timestamp,
                n.IsRead,
            })
            .ToListAsync(ct);

        var exportData = new
        {
            ExportedAt = DateTime.UtcNow,
            Profile = user,
            Tenant = tenant,
            Products = products,
            Orders = orders,
            OrderCosts = orderCosts,
            Customers = customers,
            StockMovements = stockMovements,
            Questions = questions,
            Messages = messages,
            Notifications = notifications,
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);

        // Create ZIP
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("dados_pessoais.json", CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(json);
        }

        return zipStream.ToArray();
    }

    private static UserDataExportDto MapToDto(UserDataExport export) =>
        new(export.Id, export.Status, export.CreatedAt, export.CompletedAt, export.ExpiresAt);
}
