using PeruShopHub.Application.DTOs.Onboarding;

namespace PeruShopHub.Application.Services;

public interface IOnboardingService
{
    Task<OnboardingProgressDto> GetProgressAsync(CancellationToken ct = default);
    Task<OnboardingProgressDto> CompleteStepAsync(string step, CancellationToken ct = default);
}
