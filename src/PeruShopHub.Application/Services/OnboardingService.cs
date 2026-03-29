using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Onboarding;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class OnboardingService : IOnboardingService
{
    private readonly PeruShopHubDbContext _db;

    private static readonly List<(string Key, string Label)> AllSteps = new()
    {
        ("profile", "Completar perfil"),
        ("connect_ml", "Conectar Mercado Livre"),
        ("import_products", "Importar produtos"),
        ("set_costs", "Definir custos"),
        ("view_profitability", "Ver lucratividade"),
    };

    public OnboardingService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<OnboardingProgressDto> GetProgressAsync(CancellationToken ct = default)
    {
        var progress = await _db.OnboardingProgresses
            .FirstOrDefaultAsync(ct);

        if (progress == null)
        {
            progress = new OnboardingProgress
            {
                Id = Guid.NewGuid(),
            };
            _db.OnboardingProgresses.Add(progress);
            await _db.SaveChangesAsync(ct);
        }

        return MapToDto(progress);
    }

    public async Task<OnboardingProgressDto> CompleteStepAsync(string step, CancellationToken ct = default)
    {
        var validKeys = AllSteps.Select(s => s.Key).ToHashSet();
        if (!validKeys.Contains(step))
        {
            throw new AppValidationException(new Dictionary<string, List<string>>
            {
                ["Step"] = new() { $"Etapa inválida: '{step}'. Etapas válidas: {string.Join(", ", validKeys)}" }
            });
        }

        var progress = await _db.OnboardingProgresses
            .FirstOrDefaultAsync(ct);

        if (progress == null)
        {
            progress = new OnboardingProgress
            {
                Id = Guid.NewGuid(),
            };
            _db.OnboardingProgresses.Add(progress);
        }

        if (!progress.StepsCompleted.Contains(step))
        {
            progress.StepsCompleted.Add(step);
            progress.UpdatedAt = DateTime.UtcNow;
        }

        if (progress.StepsCompleted.Count == AllSteps.Count && !progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return MapToDto(progress);
    }

    private static OnboardingProgressDto MapToDto(OnboardingProgress progress)
    {
        var steps = AllSteps.Select(s => new OnboardingStepDto(
            s.Key,
            s.Label,
            progress.StepsCompleted.Contains(s.Key)
        )).ToList();

        return new OnboardingProgressDto(
            progress.StepsCompleted,
            progress.IsCompleted,
            progress.CompletedAt,
            steps);
    }
}
