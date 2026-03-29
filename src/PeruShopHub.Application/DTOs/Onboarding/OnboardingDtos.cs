namespace PeruShopHub.Application.DTOs.Onboarding;

public record OnboardingProgressDto(
    List<string> StepsCompleted,
    bool IsCompleted,
    DateTime? CompletedAt,
    List<OnboardingStepDto> Steps);

public record OnboardingStepDto(
    string Key,
    string Label,
    bool Completed);

public record CompleteStepRequest(string Step);
