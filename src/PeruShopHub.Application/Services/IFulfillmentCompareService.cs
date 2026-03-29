using PeruShopHub.Application.DTOs.Pricing;

namespace PeruShopHub.Application.Services;

public interface IFulfillmentCompareService
{
    Task<FulfillmentCompareResult> CompareAsync(FulfillmentCompareRequest request, CancellationToken ct = default);
}
