using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PeruShopHub.Application.DTOs.Pricing;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class PricingService : IPricingService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IConfiguration _configuration;

    public PricingService(PeruShopHubDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<PriceCalculationResult> CalculateAsync(PriceCalculationRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException($"Produto {request.ProductId} não encontrado.");

        var productCost = product.PurchaseCost;
        var packagingCost = product.PackagingCost;
        var totalBaseCost = productCost + packagingCost;

        // Resolve commission rate
        var commissionRate = await ResolveCommissionRateAsync(request.MarketplaceId, request.ListingType, ct);

        // Resolve tax rate from TaxProfile or config
        var taxRate = await ResolveTaxRateAsync(ct);

        // Resolve payment fee rate (default installment = 1)
        var paymentFeeRate = await ResolvePaymentFeeRateAsync(ct);

        var targetMargin = request.TargetMarginPercent / 100m;

        // Backwards calculation: Price = TotalBaseCost / (1 - targetMargin - commission - tax - paymentFee)
        var denominator = 1m - targetMargin - commissionRate - taxRate - paymentFeeRate;

        if (denominator <= 0)
        {
            throw new AppValidationException(new Dictionary<string, List<string>>
            {
                ["TargetMarginPercent"] = new() { "A margem alvo combinada com taxas excede 100%. Reduza a margem alvo." }
            });
        }

        var suggestedPrice = Math.Round(totalBaseCost / denominator, 2);

        // Calculate amounts at suggested price
        var commissionAmount = Math.Round(suggestedPrice * commissionRate, 2);
        var taxAmount = Math.Round(suggestedPrice * taxRate, 2);
        var paymentFeeAmount = Math.Round(suggestedPrice * paymentFeeRate, 2);
        var totalCosts = totalBaseCost + commissionAmount + taxAmount + paymentFeeAmount;
        var profitAmount = suggestedPrice - totalCosts;
        var actualMargin = suggestedPrice > 0 ? Math.Round((profitAmount / suggestedPrice) * 100m, 2) : 0m;

        var breakdown = new List<CostComponentDto>
        {
            new("Custo do Produto", productCost, suggestedPrice > 0 ? Math.Round(productCost / suggestedPrice * 100m, 1) : 0, "#4CAF50"),
            new("Embalagem", packagingCost, suggestedPrice > 0 ? Math.Round(packagingCost / suggestedPrice * 100m, 1) : 0, "#8BC34A"),
            new("Comissão", commissionAmount, Math.Round(commissionRate * 100m, 1), "#FF9800"),
            new("Impostos", taxAmount, Math.Round(taxRate * 100m, 1), "#F44336"),
            new("Taxa de Pagamento", paymentFeeAmount, Math.Round(paymentFeeRate * 100m, 1), "#9C27B0"),
            new("Lucro", profitAmount, suggestedPrice > 0 ? Math.Round(profitAmount / suggestedPrice * 100m, 1) : 0, "#1A237E"),
        };

        return new PriceCalculationResult(
            SuggestedPrice: suggestedPrice,
            ProductCost: productCost,
            PackagingCost: packagingCost,
            CommissionAmount: commissionAmount,
            CommissionRate: Math.Round(commissionRate * 100m, 2),
            TaxAmount: taxAmount,
            TaxRate: Math.Round(taxRate * 100m, 2),
            PaymentFeeAmount: paymentFeeAmount,
            PaymentFeeRate: Math.Round(paymentFeeRate * 100m, 2),
            TotalCosts: totalCosts,
            ProfitAmount: profitAmount,
            ActualMarginPercent: actualMargin,
            CostBreakdown: breakdown);
    }

    public async Task<IReadOnlyList<PricingRuleDto>> GetRulesAsync(Guid? productId = null, string? marketplaceId = null, CancellationToken ct = default)
    {
        var query = _db.PricingRules.Include(r => r.Product).AsQueryable();

        if (productId.HasValue)
            query = query.Where(r => r.ProductId == productId.Value);
        if (!string.IsNullOrEmpty(marketplaceId))
            query = query.Where(r => r.MarketplaceId == marketplaceId);

        var rules = await query.OrderByDescending(r => r.UpdatedAt).ToListAsync(ct);

        return rules.Select(r => new PricingRuleDto(
            r.Id, r.ProductId, r.Product.Name, r.Product.Sku,
            r.MarketplaceId, r.ListingType, r.TargetMarginPercent,
            r.SuggestedPrice, r.CreatedAt, r.UpdatedAt)).ToList();
    }

    public async Task<PricingRuleDto> CreateRuleAsync(CreatePricingRuleDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId, ct)
            ?? throw new NotFoundException($"Produto {dto.ProductId} não encontrado.");

        // Check for existing rule
        var existing = await _db.PricingRules.FirstOrDefaultAsync(
            r => r.ProductId == dto.ProductId && r.MarketplaceId == dto.MarketplaceId, ct);
        if (existing != null)
            throw new ConflictException("Já existe uma regra de precificação para este produto neste marketplace.");

        // Calculate suggested price
        var calcResult = await CalculateAsync(new PriceCalculationRequest(
            dto.ProductId, dto.TargetMarginPercent, dto.MarketplaceId, dto.ListingType), ct);

        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            MarketplaceId = dto.MarketplaceId,
            ListingType = dto.ListingType,
            TargetMarginPercent = dto.TargetMarginPercent,
            SuggestedPrice = calcResult.SuggestedPrice,
        };

        _db.PricingRules.Add(rule);
        await _db.SaveChangesAsync(ct);

        return new PricingRuleDto(rule.Id, rule.ProductId, product.Name, product.Sku,
            rule.MarketplaceId, rule.ListingType, rule.TargetMarginPercent,
            rule.SuggestedPrice, rule.CreatedAt, rule.UpdatedAt);
    }

    public async Task<PricingRuleDto> UpdateRuleAsync(Guid id, UpdatePricingRuleDto dto, CancellationToken ct = default)
    {
        var rule = await _db.PricingRules.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Regra de precificação {id} não encontrada.");

        // Recalculate with new margin
        var calcResult = await CalculateAsync(new PriceCalculationRequest(
            rule.ProductId, dto.TargetMarginPercent, rule.MarketplaceId, rule.ListingType), ct);

        rule.TargetMarginPercent = dto.TargetMarginPercent;
        rule.SuggestedPrice = calcResult.SuggestedPrice;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new PricingRuleDto(rule.Id, rule.ProductId, rule.Product.Name, rule.Product.Sku,
            rule.MarketplaceId, rule.ListingType, rule.TargetMarginPercent,
            rule.SuggestedPrice, rule.CreatedAt, rule.UpdatedAt);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await _db.PricingRules.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Regra de precificação {id} não encontrada.");

        _db.PricingRules.Remove(rule);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<decimal> ResolveCommissionRateAsync(string marketplaceId, string? listingType, CancellationToken ct)
    {
        // Try specific rule first, then default
        var rule = await _db.CommissionRules
            .Where(r => r.MarketplaceId == marketplaceId)
            .OrderByDescending(r => r.ListingType != null ? 1 : 0) // specific first
            .FirstOrDefaultAsync(r =>
                (r.ListingType == listingType || r.ListingType == null),
                ct);

        return rule?.Rate ?? 0.11m; // Default 11% ML commission
    }

    private async Task<decimal> ResolveTaxRateAsync(CancellationToken ct)
    {
        var profile = await _db.TaxProfiles.FirstOrDefaultAsync(ct);
        if (profile != null)
            return profile.AliquotPercentage / 100m;

        // Fallback to config
        var configRate = _configuration.GetValue<decimal>("FinancialSettings:DefaultTaxRate", 6m);
        return configRate / 100m;
    }

    private async Task<decimal> ResolvePaymentFeeRateAsync(CancellationToken ct)
    {
        // Default: single payment (1 installment)
        var rule = await _db.PaymentFeeRules
            .Where(r => r.InstallmentMin <= 1 && r.InstallmentMax >= 1)
            .FirstOrDefaultAsync(ct);

        return rule != null ? rule.FeePercentage / 100m : 0.0499m; // Default ML 4.99%
    }
}
