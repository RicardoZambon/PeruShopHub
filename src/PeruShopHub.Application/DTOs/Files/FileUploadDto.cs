namespace PeruShopHub.Application.DTOs.Files;

public record FileUploadDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int SortOrder,
    string Url,
    DateTime CreatedAt);
