using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.ResponseTemplates;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class ResponseTemplateService : IResponseTemplateService
{
    private readonly PeruShopHubDbContext _db;

    public ResponseTemplateService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ResponseTemplateListDto>> GetTemplatesAsync(string? category = null, CancellationToken ct = default)
    {
        IQueryable<ResponseTemplate> query = _db.ResponseTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(rt => rt.Category == category);

        return await query
            .OrderBy(rt => rt.Order)
            .ThenBy(rt => rt.Name)
            .Select(rt => new ResponseTemplateListDto(
                rt.Id,
                rt.Name,
                rt.Category,
                rt.Body,
                rt.Placeholders,
                rt.UsageCount,
                rt.Order,
                rt.IsActive))
            .ToListAsync(ct);
    }

    public async Task<ResponseTemplateDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var rt = await _db.ResponseTemplates.AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == id, ct);

        if (rt is null)
            throw new NotFoundException("Template de resposta", id);

        return MapToDetail(rt);
    }

    public async Task<ResponseTemplateDetailDto> CreateAsync(CreateResponseTemplateDto dto, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors["Name"] = ["Nome é obrigatório"];
        else if (await _db.ResponseTemplates.AnyAsync(rt => rt.Name == dto.Name, ct))
            errors["Name"] = [$"Já existe um template com o nome \"{dto.Name}\""];

        if (string.IsNullOrWhiteSpace(dto.Category))
            errors["Category"] = ["Categoria é obrigatória"];

        if (string.IsNullOrWhiteSpace(dto.Body))
            errors["Body"] = ["Corpo do template é obrigatório"];

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var template = new ResponseTemplate
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Category = dto.Category,
            Body = dto.Body,
            Placeholders = dto.Placeholders,
            Order = dto.Order,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ResponseTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return MapToDetail(template);
    }

    public async Task<ResponseTemplateDetailDto> UpdateAsync(Guid id, UpdateResponseTemplateDto dto, CancellationToken ct = default)
    {
        var template = await _db.ResponseTemplates.FirstOrDefaultAsync(rt => rt.Id == id, ct);
        if (template is null)
            throw new NotFoundException("Template de resposta", id);

        var errors = new Dictionary<string, List<string>>();

        if (dto.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                errors["Name"] = ["Nome é obrigatório"];
            else if (await _db.ResponseTemplates.AnyAsync(rt => rt.Name == dto.Name && rt.Id != id, ct))
                errors["Name"] = [$"Já existe um template com o nome \"{dto.Name}\""];
        }

        if (dto.Category is not null && string.IsNullOrWhiteSpace(dto.Category))
            errors["Category"] = ["Categoria é obrigatória"];

        if (dto.Body is not null && string.IsNullOrWhiteSpace(dto.Body))
            errors["Body"] = ["Corpo do template é obrigatório"];

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        _db.Entry(template).Property(rt => rt.Version).OriginalValue = dto.Version;

        if (dto.Name is not null) template.Name = dto.Name;
        if (dto.Category is not null) template.Category = dto.Category;
        if (dto.Body is not null) template.Body = dto.Body;
        if (dto.Placeholders is not null) template.Placeholders = dto.Placeholders;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        if (dto.Order.HasValue) template.Order = dto.Order.Value;

        template.UpdatedAt = DateTime.UtcNow;
        template.Version++;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException();
        }

        return MapToDetail(template);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _db.ResponseTemplates.FirstOrDefaultAsync(rt => rt.Id == id, ct);
        if (template is null)
            throw new NotFoundException("Template de resposta", id);

        _db.ResponseTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementUsageAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _db.ResponseTemplates.FirstOrDefaultAsync(rt => rt.Id == id, ct);
        if (template is null)
            throw new NotFoundException("Template de resposta", id);

        template.UsageCount++;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static ResponseTemplateDetailDto MapToDetail(ResponseTemplate rt) =>
        new(rt.Id, rt.Name, rt.Category, rt.Body, rt.Placeholders,
            rt.UsageCount, rt.Order, rt.IsActive,
            rt.CreatedAt, rt.UpdatedAt, rt.Version);
}
