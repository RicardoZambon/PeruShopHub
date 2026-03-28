using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Listings;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IMarketplaceListingService _listingService;

    public ProductsController(IProductService productService, IMarketplaceListingService listingService)
    {
        _productService = productService;
        _listingService = listingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        var result = await _productService.GetListAsync(page, pageSize, search, status, categoryId, sortBy, sortDir, ct);
        return Ok(result);
    }

    [HttpGet("next-sku")]
    public async Task<ActionResult> GetNextSku([FromQuery] Guid? categoryId)
    {
        var suggestedSku = await _productService.GetNextSkuAsync(categoryId);
        return Ok(new { suggestedSku });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(Guid id, CancellationToken ct = default)
    {
        var result = await _productService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/variants")]
    public async Task<ActionResult<IReadOnlyList<ProductVariantDto>>> GetVariants(Guid id, CancellationToken ct = default)
    {
        var result = await _productService.GetVariantsAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(CreateProductDto dto, CancellationToken ct = default)
    {
        var result = await _productService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}/cost-history")]
    public async Task<ActionResult<PagedResult<ProductCostHistoryDto>>> GetCostHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _productService.GetCostHistoryAsync(id, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<ActionResult<ProductAnalyticsDto>> GetAnalytics(
        Guid id,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var result = await _productService.GetAnalyticsAsync(id, days, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/recent-orders")]
    public async Task<ActionResult<PagedResult<ProductRecentOrderDto>>> GetRecentOrders(
        Guid id,
        [FromQuery] int days = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _productService.GetRecentOrdersAsync(id, days, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var result = await _productService.UpdateAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/variants")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(Guid id, CreateProductVariantDto dto, CancellationToken ct = default)
    {
        var result = await _productService.CreateVariantAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(Guid id, Guid variantId, UpdateProductVariantDto dto, CancellationToken ct = default)
    {
        var result = await _productService.UpdateVariantAsync(id, variantId, dto, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId, CancellationToken ct = default)
    {
        await _productService.DeleteVariantAsync(id, variantId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct = default)
    {
        await _productService.DeleteAsync(id, ct);
        return NoContent();
    }

    // --- Marketplace Linking ---

    [HttpGet("{id:guid}/listings")]
    public async Task<ActionResult<IReadOnlyList<ProductListingDto>>> GetProductListings(Guid id, CancellationToken ct = default)
    {
        var result = await _listingService.GetProductListingsAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/link-marketplace")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ProductListingDto>> LinkMarketplace(Guid id, LinkMarketplaceDto dto, CancellationToken ct = default)
    {
        var result = await _listingService.LinkListingToProductAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/link-marketplace/{marketplaceId}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<IActionResult> UnlinkMarketplace(Guid id, string marketplaceId, CancellationToken ct = default)
    {
        await _listingService.UnlinkListingFromProductAsync(id, marketplaceId, ct);
        return NoContent();
    }
}
