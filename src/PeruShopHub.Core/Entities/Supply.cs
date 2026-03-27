using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class Supply : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Category { get; set; } = "Outros";
    public decimal UnitCost { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public string? Supplier { get; set; }
    public string Status { get; set; } = "Ativo";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; }
}
