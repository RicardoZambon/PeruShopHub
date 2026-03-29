using PeruShopHub.Application.DTOs.ResponseTemplates;

namespace PeruShopHub.Application.Services;

public interface IResponseTemplateService
{
    Task<IReadOnlyList<ResponseTemplateListDto>> GetTemplatesAsync(string? category = null, CancellationToken ct = default);
    Task<ResponseTemplateDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ResponseTemplateDetailDto> CreateAsync(CreateResponseTemplateDto dto, CancellationToken ct = default);
    Task<ResponseTemplateDetailDto> UpdateAsync(Guid id, UpdateResponseTemplateDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task IncrementUsageAsync(Guid id, CancellationToken ct = default);
}
