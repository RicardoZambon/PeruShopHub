using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class ReportEmailWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReportEmailWorker> _logger;
    private readonly TimeSpan _interval;

    public ReportEmailWorker(IServiceProvider services, IConfiguration config, ILogger<ReportEmailWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromHours(config.GetValue("Workers:ReportEmail:IntervalHours", 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReportEmailWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessDueReports(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error processing scheduled reports"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessDueReports(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var today = now.DayOfWeek;

        var schedules = await db.ReportSchedules
            .IgnoreQueryFilters()
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        foreach (var schedule in schedules)
        {
            if (!IsDue(schedule, now, today)) continue;

            var (dateFrom, dateTo) = GetPeriod(schedule.Frequency, now);
            var periodLabel = schedule.Frequency == "weekly"
                ? $"{dateFrom:dd/MM/yyyy} a {dateTo:dd/MM/yyyy}"
                : $"{dateFrom:MMMM yyyy}";

            try
            {
                var kpis = await BuildKpiSummary(db, schedule.TenantId, dateFrom, dateTo, ct);
                var html = BuildEmailHtml(kpis, periodLabel, schedule.Frequency);
                var recipients = schedule.Recipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (recipients.Length == 0) continue;

                var subject = $"PeruShopHub — Relatório {(schedule.Frequency == "weekly" ? "Semanal" : "Mensal")} ({periodLabel})";
                await emailService.SendAsync(recipients, subject, html, textBody: null, ct);

                schedule.LastSentAt = now;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Sent {Frequency} report for tenant {TenantId} to {Recipients}",
                    schedule.Frequency, schedule.TenantId, schedule.Recipients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {Frequency} report for tenant {TenantId}",
                    schedule.Frequency, schedule.TenantId);
            }
        }
    }

    private static bool IsDue(ReportSchedule schedule, DateTime now, DayOfWeek today)
    {
        if (schedule.Frequency == "weekly")
        {
            // Weekly: send on Monday
            if (today != DayOfWeek.Monday) return false;
            if (schedule.LastSentAt.HasValue && schedule.LastSentAt.Value.Date >= now.Date)
                return false;
            return true;
        }

        if (schedule.Frequency == "monthly")
        {
            // Monthly: send on 1st of the month
            if (now.Day != 1) return false;
            if (schedule.LastSentAt.HasValue && schedule.LastSentAt.Value.Date >= now.Date)
                return false;
            return true;
        }

        return false;
    }

    private static (DateTime dateFrom, DateTime dateTo) GetPeriod(string frequency, DateTime now)
    {
        if (frequency == "weekly")
        {
            // Previous Mon-Sun
            var lastMonday = now.Date.AddDays(-(int)now.DayOfWeek - 6);
            if (now.DayOfWeek == DayOfWeek.Monday)
                lastMonday = now.Date.AddDays(-7);
            var lastSunday = lastMonday.AddDays(6);
            return (lastMonday, lastSunday);
        }

        // Previous month
        var firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = firstOfThisMonth.AddMonths(-1);
        var lastDayOfPrevMonth = firstOfThisMonth.AddDays(-1);
        return (lastMonth, lastDayOfPrevMonth);
    }

    private static async Task<ReportKpis> BuildKpiSummary(
        PeruShopHubDbContext db, Guid tenantId, DateTime dateFrom, DateTime dateTo, CancellationToken ct)
    {
        var dateFromUtc = DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc);
        var dateToUtc = DateTime.SpecifyKind(dateTo.Date.AddDays(1), DateTimeKind.Utc);

        var orders = await db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.CreatedAt >= dateFromUtc && o.CreatedAt < dateToUtc)
            .ToListAsync(ct);

        var revenue = orders.Sum(o => o.TotalAmount);
        var profit = orders.Sum(o => o.Profit);
        var costs = revenue - profit;
        var margin = revenue > 0 ? (profit / revenue * 100m) : 0m;
        var orderCount = orders.Count;
        var avgTicket = orderCount > 0 ? revenue / orderCount : 0m;

        // Top/bottom products by revenue
        var orderIds = orders.Select(o => o.Id).ToList();
        var items = await db.OrderItems
            .IgnoreQueryFilters()
            .Where(oi => orderIds.Contains(oi.OrderId))
            .ToListAsync(ct);

        var productProfits = items
            .GroupBy(i => i.Name)
            .Select(g => new ProductProfit(
                g.Key,
                g.Sum(i => i.Subtotal),
                0m))
            .OrderByDescending(p => p.Revenue)
            .ToList();

        return new ReportKpis(revenue, costs, profit, margin, orderCount, avgTicket,
            productProfits.Take(5).ToList(),
            productProfits.TakeLast(5).Reverse().ToList());
    }

    private static string BuildEmailHtml(ReportKpis kpis, string periodLabel, string frequency)
    {
        var freqLabel = frequency == "weekly" ? "Semanal" : "Mensal";
        var topProducts = string.Join("", kpis.TopProducts.Select(p =>
            $"<tr><td style='padding:6px 12px;border-bottom:1px solid #eee'>{p.Name}</td>" +
            $"<td style='padding:6px 12px;border-bottom:1px solid #eee;text-align:right'>R$ {p.Revenue:N2}</td></tr>"));
        var bottomProducts = string.Join("", kpis.BottomProducts.Select(p =>
            $"<tr><td style='padding:6px 12px;border-bottom:1px solid #eee'>{p.Name}</td>" +
            $"<td style='padding:6px 12px;border-bottom:1px solid #eee;text-align:right'>R$ {p.Revenue:N2}</td></tr>"));

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /></head>
<body style='font-family:Inter,Arial,sans-serif;color:#1a1a2e;max-width:600px;margin:0 auto;padding:20px'>
  <div style='background:#1A237E;color:white;padding:24px;border-radius:8px 8px 0 0;text-align:center'>
    <h1 style='margin:0;font-size:20px'>PeruShopHub — Relatório {freqLabel}</h1>
    <p style='margin:8px 0 0;opacity:0.8'>{periodLabel}</p>
  </div>
  <div style='background:#f8f9fa;padding:24px;border-radius:0 0 8px 8px'>
    <table style='width:100%;border-collapse:collapse;margin-bottom:24px'>
      <tr>
        <td style='padding:12px;text-align:center;background:white;border-radius:8px;margin:4px'>
          <div style='font-size:12px;color:#666'>Faturamento</div>
          <div style='font-size:20px;font-weight:700;color:#1A237E'>R$ {kpis.Revenue:N2}</div>
        </td>
        <td style='padding:12px;text-align:center;background:white;border-radius:8px;margin:4px'>
          <div style='font-size:12px;color:#666'>Lucro</div>
          <div style='font-size:20px;font-weight:700;color:{(kpis.Profit >= 0 ? "#2e7d32" : "#c62828")}'>R$ {kpis.Profit:N2}</div>
        </td>
      </tr>
      <tr>
        <td style='padding:12px;text-align:center;background:white;border-radius:8px;margin:4px'>
          <div style='font-size:12px;color:#666'>Margem</div>
          <div style='font-size:20px;font-weight:700'>{kpis.Margin:N1}%</div>
        </td>
        <td style='padding:12px;text-align:center;background:white;border-radius:8px;margin:4px'>
          <div style='font-size:12px;color:#666'>Pedidos</div>
          <div style='font-size:20px;font-weight:700'>{kpis.OrderCount}</div>
        </td>
      </tr>
      <tr>
        <td colspan='2' style='padding:12px;text-align:center;background:white;border-radius:8px;margin:4px'>
          <div style='font-size:12px;color:#666'>Ticket Médio</div>
          <div style='font-size:20px;font-weight:700'>R$ {kpis.AvgTicket:N2}</div>
        </td>
      </tr>
    </table>

    <h3 style='margin:0 0 8px;font-size:14px'>Top 5 Produtos (Faturamento)</h3>
    <table style='width:100%;border-collapse:collapse;background:white;border-radius:8px;margin-bottom:16px'>
      {topProducts}
    </table>

    <h3 style='margin:0 0 8px;font-size:14px'>5 Menores Faturamentos</h3>
    <table style='width:100%;border-collapse:collapse;background:white;border-radius:8px;margin-bottom:16px'>
      {bottomProducts}
    </table>

    <div style='text-align:center;margin-top:24px'>
      <p style='font-size:12px;color:#999'>Este email foi gerado automaticamente pelo PeruShopHub.</p>
    </div>
  </div>
</body>
</html>";
    }

    private record ReportKpis(
        decimal Revenue, decimal Costs, decimal Profit, decimal Margin,
        int OrderCount, decimal AvgTicket,
        List<ProductProfit> TopProducts, List<ProductProfit> BottomProducts);

    private record ProductProfit(string Name, decimal Revenue, decimal Profit);
}
