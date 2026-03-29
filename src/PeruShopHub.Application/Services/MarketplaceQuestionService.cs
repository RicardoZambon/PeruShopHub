using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Questions;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class MarketplaceQuestionService : IMarketplaceQuestionService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketplaceQuestionService> _logger;

    public MarketplaceQuestionService(
        PeruShopHubDbContext db,
        IServiceProvider serviceProvider,
        ILogger<MarketplaceQuestionService> logger)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<PagedResult<QuestionListDto>> GetListAsync(
        string? status, Guid? productId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _db.MarketplaceQuestions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(q => q.Status == status);

        if (productId.HasValue)
            query = query.Where(q => q.ProductId == productId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(q => q.QuestionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuestionListDto(
                q.Id,
                q.ExternalId,
                q.ExternalItemId,
                q.ProductId,
                q.BuyerName,
                q.QuestionText,
                q.AnswerText,
                q.Status,
                q.QuestionDate,
                q.AnswerDate))
            .ToListAsync(ct);

        return new PagedResult<QuestionListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<QuestionListDto> AnswerAsync(Guid id, PostAnswerRequest request, CancellationToken ct = default)
    {
        var question = await _db.MarketplaceQuestions.FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new NotFoundException("Pergunta não encontrada.");

        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre")
            ?? throw new InvalidOperationException("Adaptador do Mercado Livre não disponível.");

        await adapter.PostAnswerAsync(question.ExternalId, request.Answer, ct);

        question.AnswerText = request.Answer;
        question.AnswerDate = DateTime.UtcNow;
        question.Status = "ANSWERED";
        question.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new QuestionListDto(
            question.Id,
            question.ExternalId,
            question.ExternalItemId,
            question.ProductId,
            question.BuyerName,
            question.QuestionText,
            question.AnswerText,
            question.Status,
            question.QuestionDate,
            question.AnswerDate);
    }

    public async Task SyncQuestionsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId
                && c.MarketplaceId == "mercadolivre"
                && c.IsConnected
                && c.Status == "Active", ct);

        if (connection is null)
        {
            _logger.LogDebug("No active ML connection for tenant {TenantId}. Skipping question sync", tenantId);
            return;
        }

        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
        if (adapter is null)
        {
            _logger.LogWarning("ML adapter not available for question sync");
            return;
        }

        // Sync unanswered questions
        await SyncQuestionsByStatusAsync(adapter, tenantId, "UNANSWERED", ct);

        // Also sync recently answered to keep state up to date
        await SyncQuestionsByStatusAsync(adapter, tenantId, "ANSWERED", ct);
    }

    public async Task SyncSingleQuestionAsync(string externalQuestionId, Guid tenantId, CancellationToken ct = default)
    {
        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
        if (adapter is null)
        {
            _logger.LogWarning("ML adapter not available for single question sync");
            return;
        }

        var detail = await adapter.GetQuestionAsync(externalQuestionId, ct);
        await UpsertQuestionAsync(detail, tenantId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Synced question {QuestionId} for tenant {TenantId}", externalQuestionId, tenantId);
    }

    private async Task SyncQuestionsByStatusAsync(
        IMarketplaceAdapter adapter, Guid tenantId, string status, CancellationToken ct)
    {
        var offset = 0;
        const int limit = 50;

        do
        {
            ct.ThrowIfCancellationRequested();

            var result = await adapter.SearchQuestionsAsync(status, offset, limit, ct);

            foreach (var q in result.Questions)
            {
                await UpsertQuestionAsync(q, tenantId, ct);
            }

            await _db.SaveChangesAsync(ct);

            if (result.Questions.Count < limit)
                break;

            offset += limit;
        }
        while (true);
    }

    private async Task UpsertQuestionAsync(MarketplaceQuestionDetail detail, Guid tenantId, CancellationToken ct)
    {
        var existing = await _db.MarketplaceQuestions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.ExternalId == detail.ExternalId, ct);

        // Try to link to a product via MarketplaceListing
        var productId = await _db.MarketplaceListings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId && l.ExternalId == detail.ItemId)
            .Select(l => l.ProductId)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            var question = new MarketplaceQuestion
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExternalId = detail.ExternalId,
                ExternalItemId = detail.ItemId,
                ProductId = productId,
                BuyerName = detail.BuyerNickname,
                QuestionText = detail.QuestionText,
                AnswerText = detail.AnswerText,
                Status = detail.Status,
                QuestionDate = detail.QuestionDate.UtcDateTime,
                AnswerDate = detail.AnswerDate?.UtcDateTime,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.MarketplaceQuestions.Add(question);
        }
        else
        {
            existing.AnswerText = detail.AnswerText;
            existing.Status = detail.Status;
            existing.AnswerDate = detail.AnswerDate?.UtcDateTime;
            existing.ProductId = productId;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
