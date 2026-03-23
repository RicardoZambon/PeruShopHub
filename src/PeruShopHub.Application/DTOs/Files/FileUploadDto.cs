namespace PeruShopHub.Application.DTOs.Files;
public record FileUploadDto(Guid Id, string Url, string FileName, string ContentType, long SizeBytes, int SortOrder);
