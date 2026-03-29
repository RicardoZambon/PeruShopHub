using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Questions;

namespace PeruShopHub.Application.Services;

public interface IMarketplaceQuestionService
{
    Task<PagedResult<QuestionListDto>> GetListAsync(
        string? status, Guid? productId, int page = 1, int pageSize = 20, CancellationToken ct = default);

    Task<QuestionListDto> AnswerAsync(Guid id, PostAnswerRequest request, CancellationToken ct = default);

    Task SyncQuestionsAsync(Guid tenantId, CancellationToken ct = default);

    Task SyncSingleQuestionAsync(string externalQuestionId, Guid tenantId, CancellationToken ct = default);
}
