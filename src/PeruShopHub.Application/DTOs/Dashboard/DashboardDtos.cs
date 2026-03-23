namespace PeruShopHub.Application.DTOs.Dashboard;

public record KpiCardDto(
    string Title,
    string Value,
    string? PreviousValue = null,
    decimal? ChangePercent = null,
    string? ChangeDirection = null,
    string? Icon = null);

public record ProductRankingDto(
    Guid Id,
    string Name,
    string Sku,
    int QuantitySold,
    decimal Revenue,
    decimal Profit,
    decimal Margin);

public record PendingActionDto(
    string Type,
    string Title,
    string Description,
    string? NavigationTarget = null,
    int Count = 1);

public record ChartDataPointDto(
    string Label,
    decimal Value,
    decimal? SecondaryValue = null);

public record DashboardSummaryDto(
    IReadOnlyList<KpiCardDto> Kpis,
    IReadOnlyList<ProductRankingDto> TopProducts,
    IReadOnlyList<PendingActionDto> PendingActions,
    IReadOnlyList<ChartDataPointDto> RevenueChart,
    IReadOnlyList<ChartDataPointDto> OrdersChart);
