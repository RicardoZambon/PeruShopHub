using PeruShopHub.Application.DTOs.Categories;

namespace PeruShopHub.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListDto>> GetCategoriesAsync(Guid? parentId, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryListDto>> SearchCategoriesAsync(string query, CancellationToken ct = default);
    Task<CategoryDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
    Task<CategoryDetailDto> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // Variation fields
    Task<IReadOnlyList<VariationFieldDto>> GetVariationFieldsAsync(Guid categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<InheritedVariationFieldDto>> GetInheritedVariationFieldsAsync(Guid categoryId, CancellationToken ct = default);
    Task<VariationFieldDto> CreateVariationFieldAsync(Guid categoryId, CreateVariationFieldDto dto, CancellationToken ct = default);
    Task<VariationFieldDto> UpdateVariationFieldAsync(Guid categoryId, Guid fieldId, UpdateVariationFieldDto dto, CancellationToken ct = default);
    Task DeleteVariationFieldAsync(Guid categoryId, Guid fieldId, CancellationToken ct = default);
}
