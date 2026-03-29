using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Enums;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Owner,Admin")]
public class SettingsController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly IAuditService _auditService;

    private static decimal _taxRate = 6.0m;

    public SettingsController(PeruShopHubDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    // --- Integrations ---

    [HttpGet("integrations")]
    public async Task<ActionResult<IReadOnlyList<IntegrationDto>>> GetIntegrations()
    {
        var integrations = await _db.MarketplaceConnections
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new IntegrationDto(
                m.Id, m.MarketplaceId, m.Name, m.Logo,
                m.IsConnected, m.SellerNickname, m.LastSyncAt, m.ComingSoon,
                m.Status, m.ExternalUserId, m.TokenExpiresAt, m.RefreshErrorCount))
            .ToListAsync();

        return Ok(integrations);
    }

    // --- Costs ---

    [HttpGet("costs")]
    public ActionResult<object> GetCosts()
    {
        var costs = new
        {
            defaultPackagingCost = 2.50m,
            icmsRate = 6.0m,
            taxRate = _taxRate,
            fixedCosts = new[]
            {
                new { id = "1", name = "Internet/Telefone", value = 150.00m },
                new { id = "2", name = "Software/Ferramentas", value = 89.90m }
            }
        };

        return Ok(costs);
    }

    [HttpPut("costs")]
    public ActionResult<object> UpdateCosts([FromBody] UpdateCostsDto dto)
    {
        if (dto.TaxRate.HasValue)
            _taxRate = dto.TaxRate.Value;

        return GetCosts();
    }

    // --- Commission Rules ---

    [HttpGet("commission-rules")]
    public async Task<ActionResult<IReadOnlyList<CommissionRuleDto>>> GetCommissionRules()
    {
        var rules = await _db.CommissionRules
            .AsNoTracking()
            .OrderBy(r => r.MarketplaceId)
            .ThenBy(r => r.CategoryPattern)
            .ThenBy(r => r.ListingType)
            .Select(r => new CommissionRuleDto(
                r.Id, r.MarketplaceId, r.CategoryPattern, r.ListingType, r.Rate, r.IsDefault))
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("commission-rules")]
    public async Task<ActionResult<CommissionRuleDto>> CreateCommissionRule([FromBody] CreateCommissionRuleDto dto)
    {
        var rule = new CommissionRule
        {
            Id = Guid.NewGuid(),
            MarketplaceId = dto.MarketplaceId,
            CategoryPattern = dto.CategoryPattern,
            ListingType = dto.ListingType,
            Rate = dto.Rate,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Regra de comissão criada", "CommissionRule", rule.Id,
            null, new { rule.MarketplaceId, rule.CategoryPattern, rule.ListingType, rule.Rate });

        var result = new CommissionRuleDto(
            rule.Id, rule.MarketplaceId, rule.CategoryPattern, rule.ListingType, rule.Rate, rule.IsDefault);

        return CreatedAtAction(nameof(GetCommissionRules), result);
    }

    [HttpPut("commission-rules/{id:guid}")]
    public async Task<ActionResult<CommissionRuleDto>> UpdateCommissionRule(Guid id, [FromBody] UpdateCommissionRuleDto dto)
    {
        var rule = await _db.CommissionRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        var oldRate = rule.Rate;
        rule.Rate = dto.Rate;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Regra de comissão atualizada", "CommissionRule", rule.Id,
            new { Rate = oldRate }, new { Rate = rule.Rate });

        var result = new CommissionRuleDto(
            rule.Id, rule.MarketplaceId, rule.CategoryPattern, rule.ListingType, rule.Rate, rule.IsDefault);

        return Ok(result);
    }

    [HttpDelete("commission-rules/{id:guid}")]
    public async Task<IActionResult> DeleteCommissionRule(Guid id)
    {
        var rule = await _db.CommissionRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        if (rule.IsDefault)
            return Conflict(new { message = "Cannot delete a default commission rule." });

        await _auditService.LogAsync("Regra de comissão removida", "CommissionRule", rule.Id,
            new { rule.MarketplaceId, rule.CategoryPattern, rule.ListingType, rule.Rate }, null);

        _db.CommissionRules.Remove(rule);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // --- Payment Fee Rules ---

    [HttpGet("payment-fee-rules")]
    public async Task<ActionResult<IReadOnlyList<PaymentFeeRuleDto>>> GetPaymentFeeRules()
    {
        var rules = await _db.PaymentFeeRules
            .AsNoTracking()
            .OrderBy(r => r.InstallmentMin)
            .ThenBy(r => r.InstallmentMax)
            .Select(r => new PaymentFeeRuleDto(
                r.Id, r.InstallmentMin, r.InstallmentMax, r.FeePercentage, r.IsDefault))
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("payment-fee-rules")]
    public async Task<ActionResult<PaymentFeeRuleDto>> CreatePaymentFeeRule([FromBody] CreatePaymentFeeRuleDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.InstallmentMin < 1)
            errors["InstallmentMin"] = new[] { "Parcela mínima deve ser pelo menos 1" };
        if (dto.InstallmentMax < dto.InstallmentMin)
            errors["InstallmentMax"] = new[] { "Parcela máxima deve ser maior ou igual à mínima" };
        if (dto.FeePercentage < 0 || dto.FeePercentage > 100)
            errors["FeePercentage"] = new[] { "Taxa deve estar entre 0 e 100" };

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var rule = new PaymentFeeRule
        {
            Id = Guid.NewGuid(),
            InstallmentMin = dto.InstallmentMin,
            InstallmentMax = dto.InstallmentMax,
            FeePercentage = dto.FeePercentage,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.PaymentFeeRules.Add(rule);
        await _db.SaveChangesAsync();

        var result = new PaymentFeeRuleDto(
            rule.Id, rule.InstallmentMin, rule.InstallmentMax, rule.FeePercentage, rule.IsDefault);

        return CreatedAtAction(nameof(GetPaymentFeeRules), result);
    }

    [HttpPut("payment-fee-rules/{id:guid}")]
    public async Task<ActionResult<PaymentFeeRuleDto>> UpdatePaymentFeeRule(Guid id, [FromBody] UpdatePaymentFeeRuleDto dto)
    {
        var rule = await _db.PaymentFeeRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        var errors = new Dictionary<string, string[]>();

        if (dto.InstallmentMin < 1)
            errors["InstallmentMin"] = new[] { "Parcela mínima deve ser pelo menos 1" };
        if (dto.InstallmentMax < dto.InstallmentMin)
            errors["InstallmentMax"] = new[] { "Parcela máxima deve ser maior ou igual à mínima" };
        if (dto.FeePercentage < 0 || dto.FeePercentage > 100)
            errors["FeePercentage"] = new[] { "Taxa deve estar entre 0 e 100" };

        if (errors.Count > 0)
            return BadRequest(new { errors });

        rule.InstallmentMin = dto.InstallmentMin;
        rule.InstallmentMax = dto.InstallmentMax;
        rule.FeePercentage = dto.FeePercentage;
        await _db.SaveChangesAsync();

        var result = new PaymentFeeRuleDto(
            rule.Id, rule.InstallmentMin, rule.InstallmentMax, rule.FeePercentage, rule.IsDefault);

        return Ok(result);
    }

    [HttpDelete("payment-fee-rules/{id:guid}")]
    public async Task<IActionResult> DeletePaymentFeeRule(Guid id)
    {
        var rule = await _db.PaymentFeeRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        if (rule.IsDefault)
            return Conflict(new { message = "Não é possível excluir a regra padrão." });

        _db.PaymentFeeRules.Remove(rule);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // --- Tax Profile ---

    [HttpGet("tax-profile")]
    public async Task<ActionResult<TaxProfileDto>> GetTaxProfile()
    {
        var profile = await _db.TaxProfiles.FirstOrDefaultAsync();

        if (profile is null)
        {
            // Return default values if no profile exists yet
            return Ok(new TaxProfileDto(Guid.Empty, "SimplesNacional", 6.0m, null));
        }

        return Ok(new TaxProfileDto(profile.Id, profile.TaxRegime, profile.AliquotPercentage, profile.State));
    }

    [HttpPut("tax-profile")]
    public async Task<ActionResult<TaxProfileDto>> UpdateTaxProfile([FromBody] UpdateTaxProfileDto dto)
    {
        var validRegimes = new[] { "SimplesNacional", "LucroPresumido", "MEI" };
        if (!validRegimes.Contains(dto.TaxRegime))
            return BadRequest(new { errors = new Dictionary<string, string[]> { ["TaxRegime"] = new[] { "Regime tributário inválido. Valores aceitos: SimplesNacional, LucroPresumido, MEI" } } });

        if (dto.AliquotPercentage < 0 || dto.AliquotPercentage > 100)
            return BadRequest(new { errors = new Dictionary<string, string[]> { ["AliquotPercentage"] = new[] { "Alíquota deve estar entre 0 e 100" } } });

        var profile = await _db.TaxProfiles.FirstOrDefaultAsync();

        if (profile is null)
        {
            profile = new TaxProfile
            {
                Id = Guid.NewGuid(),
                TaxRegime = dto.TaxRegime,
                AliquotPercentage = dto.AliquotPercentage,
                State = dto.State,
            };
            _db.TaxProfiles.Add(profile);
        }
        else
        {
            profile.TaxRegime = dto.TaxRegime;
            profile.AliquotPercentage = dto.AliquotPercentage;
            profile.State = dto.State;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new TaxProfileDto(profile.Id, profile.TaxRegime, profile.AliquotPercentage, profile.State));
    }
    // --- Report Schedules ---

    [HttpGet("report-schedules")]
    public async Task<ActionResult<IReadOnlyList<ReportScheduleDto>>> GetReportSchedules()
    {
        var schedules = await _db.ReportSchedules
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ReportScheduleDto(
                s.Id, s.Frequency, s.Recipients, s.IsActive, s.LastSentAt, s.CreatedAt))
            .ToListAsync();

        return Ok(schedules);
    }

    [HttpPost("report-schedules")]
    public async Task<ActionResult<ReportScheduleDto>> CreateReportSchedule([FromBody] CreateReportScheduleDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        var validFrequencies = new[] { "weekly", "monthly" };
        if (!validFrequencies.Contains(dto.Frequency))
            errors["Frequency"] = new[] { "Frequência inválida. Valores aceitos: weekly, monthly" };

        if (string.IsNullOrWhiteSpace(dto.Recipients))
            errors["Recipients"] = new[] { "Pelo menos um destinatário é obrigatório" };

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var schedule = new ReportSchedule
        {
            Id = Guid.NewGuid(),
            Frequency = dto.Frequency,
            Recipients = dto.Recipients.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.ReportSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        var result = new ReportScheduleDto(
            schedule.Id, schedule.Frequency, schedule.Recipients, schedule.IsActive, schedule.LastSentAt, schedule.CreatedAt);

        return CreatedAtAction(nameof(GetReportSchedules), result);
    }

    [HttpPut("report-schedules/{id:guid}")]
    public async Task<ActionResult<ReportScheduleDto>> UpdateReportSchedule(Guid id, [FromBody] UpdateReportScheduleDto dto)
    {
        var schedule = await _db.ReportSchedules.FindAsync(id);
        if (schedule is null)
            return NotFound();

        var errors = new Dictionary<string, string[]>();

        var validFrequencies = new[] { "weekly", "monthly" };
        if (!validFrequencies.Contains(dto.Frequency))
            errors["Frequency"] = new[] { "Frequência inválida. Valores aceitos: weekly, monthly" };

        if (string.IsNullOrWhiteSpace(dto.Recipients))
            errors["Recipients"] = new[] { "Pelo menos um destinatário é obrigatório" };

        if (errors.Count > 0)
            return BadRequest(new { errors });

        schedule.Frequency = dto.Frequency;
        schedule.Recipients = dto.Recipients.Trim();
        schedule.IsActive = dto.IsActive;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var result = new ReportScheduleDto(
            schedule.Id, schedule.Frequency, schedule.Recipients, schedule.IsActive, schedule.LastSentAt, schedule.CreatedAt);

        return Ok(result);
    }

    [HttpDelete("report-schedules/{id:guid}")]
    public async Task<IActionResult> DeleteReportSchedule(Guid id)
    {
        var schedule = await _db.ReportSchedules.FindAsync(id);
        if (schedule is null)
            return NotFound();

        _db.ReportSchedules.Remove(schedule);
        await _db.SaveChangesAsync();

        return NoContent();
    }
    // --- Alert Rules ---

    [HttpGet("alert-rules")]
    public async Task<ActionResult<IReadOnlyList<AlertRuleDto>>> GetAlertRules()
    {
        var rules = await _db.AlertRules
            .AsNoTracking()
            .Include(a => a.Product)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new AlertRuleDto(
                a.Id, a.Type, a.Threshold, a.IsActive, a.ProductId,
                a.Product != null ? a.Product.Name : null, a.CreatedAt))
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("alert-rules")]
    public async Task<ActionResult<AlertRuleDto>> CreateAlertRule([FromBody] CreateAlertRuleDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        var validTypes = new[] { "MarginBelow", "CostIncrease", "StockLow" };

        if (!validTypes.Contains(dto.Type))
            errors["Type"] = new[] { "Tipo inválido. Valores aceitos: MarginBelow, CostIncrease, StockLow" };

        if (dto.Threshold < 0)
            errors["Threshold"] = new[] { "Limite deve ser maior ou igual a 0" };

        if (dto.ProductId.HasValue)
        {
            var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId.Value);
            if (!productExists)
                errors["ProductId"] = new[] { "Produto não encontrado" };
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            Type = dto.Type,
            Threshold = dto.Threshold,
            IsActive = true,
            ProductId = dto.ProductId,
            CreatedAt = DateTime.UtcNow
        };

        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Regra de alerta criada", "AlertRule", rule.Id,
            null, new { rule.Type, rule.Threshold, rule.ProductId });

        string? productName = null;
        if (rule.ProductId.HasValue)
            productName = await _db.Products.Where(p => p.Id == rule.ProductId).Select(p => p.Name).FirstOrDefaultAsync();

        return CreatedAtAction(nameof(GetAlertRules),
            new AlertRuleDto(rule.Id, rule.Type, rule.Threshold, rule.IsActive, rule.ProductId, productName, rule.CreatedAt));
    }

    [HttpPut("alert-rules/{id:guid}")]
    public async Task<ActionResult<AlertRuleDto>> UpdateAlertRule(Guid id, [FromBody] UpdateAlertRuleDto dto)
    {
        var rule = await _db.AlertRules.Include(a => a.Product).FirstOrDefaultAsync(a => a.Id == id);
        if (rule is null)
            return NotFound();

        var errors = new Dictionary<string, string[]>();

        if (dto.Threshold < 0)
            errors["Threshold"] = new[] { "Limite deve ser maior ou igual a 0" };

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var oldThreshold = rule.Threshold;
        var oldIsActive = rule.IsActive;

        rule.Threshold = dto.Threshold;
        rule.IsActive = dto.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Regra de alerta atualizada", "AlertRule", rule.Id,
            new { Threshold = oldThreshold, IsActive = oldIsActive },
            new { Threshold = rule.Threshold, IsActive = rule.IsActive });

        return Ok(new AlertRuleDto(rule.Id, rule.Type, rule.Threshold, rule.IsActive, rule.ProductId,
            rule.Product?.Name, rule.CreatedAt));
    }

    [HttpDelete("alert-rules/{id:guid}")]
    public async Task<IActionResult> DeleteAlertRule(Guid id)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        await _auditService.LogAsync("Regra de alerta removida", "AlertRule", rule.Id,
            new { rule.Type, rule.Threshold, rule.ProductId }, null);

        _db.AlertRules.Remove(rule);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // --- Response Time Settings ---

    [HttpGet("response-time-settings")]
    public async Task<ActionResult<ResponseTimeSettingsDto>> GetResponseTimeSettings()
    {
        var settings = await _db.ResponseTimeSettings.FirstOrDefaultAsync();

        if (settings is null)
        {
            return Ok(new ResponseTimeSettingsDto(Guid.Empty, 4, 12));
        }

        return Ok(new ResponseTimeSettingsDto(settings.Id, settings.QuestionThresholdHours, settings.MessageThresholdHours));
    }

    [HttpPut("response-time-settings")]
    public async Task<ActionResult<ResponseTimeSettingsDto>> UpdateResponseTimeSettings([FromBody] UpdateResponseTimeSettingsDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.QuestionThresholdHours < 1 || dto.QuestionThresholdHours > 168)
            errors["QuestionThresholdHours"] = new[] { "Limite de perguntas deve estar entre 1 e 168 horas" };

        if (dto.MessageThresholdHours < 1 || dto.MessageThresholdHours > 168)
            errors["MessageThresholdHours"] = new[] { "Limite de mensagens deve estar entre 1 e 168 horas" };

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var settings = await _db.ResponseTimeSettings.FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new ResponseTimeSettings
            {
                Id = Guid.NewGuid(),
                QuestionThresholdHours = dto.QuestionThresholdHours,
                MessageThresholdHours = dto.MessageThresholdHours,
            };
            _db.ResponseTimeSettings.Add(settings);
        }
        else
        {
            settings.QuestionThresholdHours = dto.QuestionThresholdHours;
            settings.MessageThresholdHours = dto.MessageThresholdHours;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Configurações de tempo de resposta atualizadas", "ResponseTimeSettings", settings.Id,
            null, new { settings.QuestionThresholdHours, settings.MessageThresholdHours });

        return Ok(new ResponseTimeSettingsDto(settings.Id, settings.QuestionThresholdHours, settings.MessageThresholdHours));
    }

    // --- Notification Preferences ---

    [HttpGet("notification-preferences")]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceDto>>> GetNotificationPreferences()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var prefs = await _db.NotificationPreferences
            .AsNoTracking()
            .Where(np => np.UserId == userId)
            .OrderBy(np => np.Type)
            .Select(np => new NotificationPreferenceDto(np.Id, np.Type.ToString(), np.EmailEnabled, np.InAppEnabled))
            .ToListAsync();

        // If user has no preferences yet, return defaults (all enabled)
        if (prefs.Count == 0)
        {
            prefs = Enum.GetValues<NotificationType>()
                .OrderBy(t => t)
                .Select(t => new NotificationPreferenceDto(Guid.Empty, t.ToString(), true, true))
                .ToList();
        }

        return Ok(prefs);
    }

    [HttpPut("notification-preferences")]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceDto>>> UpdateNotificationPreferences(
        [FromBody] IReadOnlyList<UpdateNotificationPreferenceDto> dtos)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tenantUser = await _db.TenantUsers.AsNoTracking()
            .FirstOrDefaultAsync(tu => tu.UserId == userId);
        if (tenantUser is null)
            return BadRequest(new { errors = new Dictionary<string, string[]> { ["User"] = new[] { "Usuário não pertence a nenhum tenant" } } });

        var errors = new Dictionary<string, string[]>();
        var validTypes = Enum.GetNames<NotificationType>();

        foreach (var dto in dtos)
        {
            if (!validTypes.Contains(dto.Type))
                errors[dto.Type] = new[] { $"Tipo de notificação inválido: {dto.Type}" };
        }

        if (errors.Count > 0)
            return BadRequest(new { errors });

        var existing = await _db.NotificationPreferences
            .Where(np => np.UserId == userId)
            .ToListAsync();

        var result = new List<NotificationPreferenceDto>();

        foreach (var dto in dtos)
        {
            var type = Enum.Parse<NotificationType>(dto.Type);
            var pref = existing.FirstOrDefault(p => p.Type == type);

            if (pref is null)
            {
                pref = new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantUser.TenantId,
                    UserId = userId,
                    Type = type,
                    EmailEnabled = dto.EmailEnabled,
                    InAppEnabled = dto.InAppEnabled,
                    CreatedAt = DateTime.UtcNow
                };
                _db.NotificationPreferences.Add(pref);
            }
            else
            {
                pref.EmailEnabled = dto.EmailEnabled;
                pref.InAppEnabled = dto.InAppEnabled;
                pref.UpdatedAt = DateTime.UtcNow;
            }

            result.Add(new NotificationPreferenceDto(pref.Id, pref.Type.ToString(), pref.EmailEnabled, pref.InAppEnabled));
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Preferências de notificação atualizadas", "NotificationPreference", userId,
            null, new { count = dtos.Count });

        return Ok(result);
    }
}

public record AlertRuleDto(Guid Id, string Type, decimal Threshold, bool IsActive, Guid? ProductId, string? ProductName, DateTime CreatedAt);
public record CreateAlertRuleDto(string Type, decimal Threshold, Guid? ProductId);
public record UpdateAlertRuleDto(decimal Threshold, bool IsActive);
public record UpdateCostsDto(decimal? TaxRate);
public record UpdateCommissionRuleDto(decimal Rate);
public record TaxProfileDto(Guid Id, string TaxRegime, decimal AliquotPercentage, string? State);
public record UpdateTaxProfileDto(string TaxRegime, decimal AliquotPercentage, string? State);
public record PaymentFeeRuleDto(Guid Id, int InstallmentMin, int InstallmentMax, decimal FeePercentage, bool IsDefault);
public record CreatePaymentFeeRuleDto(int InstallmentMin, int InstallmentMax, decimal FeePercentage);
public record UpdatePaymentFeeRuleDto(int InstallmentMin, int InstallmentMax, decimal FeePercentage);
public record ReportScheduleDto(Guid Id, string Frequency, string Recipients, bool IsActive, DateTime? LastSentAt, DateTime CreatedAt);
public record CreateReportScheduleDto(string Frequency, string Recipients, bool IsActive);
public record UpdateReportScheduleDto(string Frequency, string Recipients, bool IsActive);
public record NotificationPreferenceDto(Guid Id, string Type, bool EmailEnabled, bool InAppEnabled);
public record UpdateNotificationPreferenceDto(string Type, bool EmailEnabled, bool InAppEnabled);
public record ResponseTimeSettingsDto(Guid Id, int QuestionThresholdHours, int MessageThresholdHours);
public record UpdateResponseTimeSettingsDto(int QuestionThresholdHours, int MessageThresholdHours);
