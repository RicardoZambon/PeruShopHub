using PeruShopHub.Application.DTOs.Search;

namespace PeruShopHub.Application.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string? query, int limit = 10, CancellationToken ct = default);
}
