namespace PeruShopHub.Application.DTOs.Questions;

public record QuestionListDto(
    Guid Id,
    string ExternalId,
    string ExternalItemId,
    Guid? ProductId,
    string BuyerName,
    string QuestionText,
    string? AnswerText,
    string Status,
    DateTime QuestionDate,
    DateTime? AnswerDate);

public record PostAnswerRequest(string Answer);
