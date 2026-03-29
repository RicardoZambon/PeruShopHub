using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Onboarding;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("progress")]
    public async Task<ActionResult<OnboardingProgressDto>> GetProgress(CancellationToken ct)
    {
        var result = await _onboardingService.GetProgressAsync(ct);
        return Ok(result);
    }

    [HttpPost("complete-step")]
    public async Task<ActionResult<OnboardingProgressDto>> CompleteStep(CompleteStepRequest request, CancellationToken ct)
    {
        var result = await _onboardingService.CompleteStepAsync(request.Step, ct);
        return Ok(result);
    }
}
