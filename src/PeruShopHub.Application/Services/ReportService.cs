using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PeruShopHub.Application.Services;

public class ReportService : IReportService
{
    private readonly PeruShopHubDbContext _db;

    private static readonly string PrimaryColor = "#1A237E";
    private static readonly string AccentColor = "#FF6F00";
    private static readonly string SuccessColor = "#2E7D32";
    private static readonly string DangerColor = "#C62828";

    public ReportService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateProfitabilityReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var start = dateFrom ?? DateTime.UtcNow.AddDays(-30);
        var end = dateTo ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalProfit = orders.Sum(o => o.Profit);
        var totalCosts = totalRevenue - totalProfit;
        var avgMargin = totalRevenue != 0 ? Math.Round(totalProfit / totalRevenue * 100, 2) : 0m;
        var orderCount = orders.Count;
        var avgTicket = orderCount > 0 ? Math.Round(totalRevenue / orderCount, 2) : 0m;

        // Group by SKU
        var skuData = orders
            .SelectMany(o => o.Items.Select(i => new { Item = i, Order = o }))
            .GroupBy(x => x.Item.Sku ?? "N/A")
            .Select(g =>
            {
                var revenue = g.Sum(x => x.Item.Quantity * x.Item.UnitPrice);
                var unitsSold = g.Sum(x => x.Item.Quantity);
                var productName = g.First().Item.Name ?? "N/A";
                var totalOrderRevenue = g.Select(x => x.Order).Distinct().Sum(o => o.TotalAmount);
                var totalOrderCosts = g.Select(x => x.Order).Distinct().Sum(o => o.Costs.Sum(c => c.Value));
                var proportion = totalOrderRevenue != 0 ? revenue / totalOrderRevenue : 0;
                var costs = totalOrderCosts * proportion;
                var profit = revenue - costs;
                var margin = revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0m;
                return new { Sku = g.Key, Name = productName, UnitsSold = unitsSold, Revenue = revenue, Costs = costs, Profit = profit, Margin = margin };
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);

                page.Header().Element(c => ComposeHeader(c, "Relatório de Lucratividade", start, end));

                page.Content().Element(content =>
                {
                    content.PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Element(c => ComposeKpiRow(c, new[]
                        {
                            ("Receita Total", FormatBrl(totalRevenue)),
                            ("Custos Totais", FormatBrl(totalCosts)),
                            ("Lucro Total", FormatBrl(totalProfit)),
                            ("Margem Média", $"{avgMargin:F2}%"),
                            ("Pedidos", orderCount.ToString()),
                            ("Ticket Médio", FormatBrl(avgTicket)),
                        }));

                        col.Item().Element(c => ComposeSectionTitle(c, "Lucratividade por SKU"));

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "SKU", "Produto", "Qtd", "Receita", "Custos", "Lucro", "Margem" })
                                    header.Cell().Background(PrimaryColor).Padding(5)
                                        .Text(h).FontSize(8).Bold().FontColor(Colors.White);
                            });

                            foreach (var sku in skuData)
                            {
                                ComposeTableCell(table, sku.Sku, false);
                                ComposeTableCell(table, sku.Name, false);
                                ComposeTableCell(table, sku.UnitsSold.ToString(), true);
                                ComposeTableCell(table, FormatBrl(sku.Revenue), true);
                                ComposeTableCell(table, FormatBrl(sku.Costs), true);
                                ComposeTableCell(table, FormatBrl(sku.Profit), true, sku.Profit >= 0 ? SuccessColor : DangerColor);
                                ComposeTableCell(table, $"{sku.Margin:F1}%", true, sku.Margin >= 20 ? SuccessColor : sku.Margin >= 10 ? AccentColor : DangerColor);
                            }
                        });
                    });
                });

                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateOrderReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var start = dateFrom ?? DateTime.UtcNow.AddDays(-30);
        var end = dateTo ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalProfit = orders.Sum(o => o.Profit);
        var totalCosts = totalRevenue - totalProfit;
        var orderCount = orders.Count;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(30);
                page.MarginVertical(25);

                page.Header().Element(c => ComposeHeader(c, "Relatório de Vendas", start, end));

                page.Content().Element(content =>
                {
                    content.PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Element(c => ComposeKpiRow(c, new[]
                        {
                            ("Total Pedidos", orderCount.ToString()),
                            ("Receita", FormatBrl(totalRevenue)),
                            ("Custos", FormatBrl(totalCosts)),
                            ("Lucro", FormatBrl(totalProfit)),
                        }));

                        col.Item().Element(c => ComposeSectionTitle(c, "Detalhamento de Vendas"));

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(0.6f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(1f);
                                columns.RelativeColumn(0.7f);
                                columns.RelativeColumn(0.8f);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Pedido", "Data", "Comprador", "Itens", "Valor", "Lucro", "Margem", "Status" })
                                    header.Cell().Background(PrimaryColor).Padding(5)
                                        .Text(h).FontSize(8).Bold().FontColor(Colors.White);
                            });

                            foreach (var order in orders)
                            {
                                var margin = order.TotalAmount != 0 ? Math.Round(order.Profit / order.TotalAmount * 100, 1) : 0m;
                                var buyerName = order.BuyerName ?? "N/A";

                                ComposeTableCell(table, order.ExternalOrderId ?? order.Id.ToString()[..8], false);
                                ComposeTableCell(table, order.OrderDate.ToString("dd/MM/yyyy"), false);
                                ComposeTableCell(table, buyerName.Length > 25 ? buyerName[..22] + "..." : buyerName, false);
                                ComposeTableCell(table, order.Items.Sum(i => i.Quantity).ToString(), true);
                                ComposeTableCell(table, FormatBrl(order.TotalAmount), true);
                                ComposeTableCell(table, FormatBrl(order.Profit), true, order.Profit >= 0 ? SuccessColor : DangerColor);
                                ComposeTableCell(table, $"{margin:F1}%", true, margin >= 20 ? SuccessColor : margin >= 10 ? AccentColor : DangerColor);
                                ComposeTableCell(table, order.Status ?? "N/A", true);
                            }
                        });
                    });
                });

                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateInventoryReportAsync(CancellationToken ct = default)
    {
        var products = await _db.Products
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var items = products.Select(p =>
        {
            var totalStock = p.Variants.Sum(v => v.Stock);
            var unitCost = p.PurchaseCost;
            var stockValue = totalStock * unitCost;
            return new { p.Sku, p.Name, TotalStock = totalStock, UnitCost = unitCost, StockValue = stockValue, p.MinStock, p.MaxStock };
        }).ToList();

        var totalItems = items.Count;
        var totalUnits = items.Sum(i => i.TotalStock);
        var totalValue = items.Sum(i => i.StockValue);
        var belowMin = items.Count(i => i.MinStock.HasValue && i.TotalStock <= i.MinStock.Value);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(30);

                page.Header().Element(c => ComposeHeader(c, "Relatório de Estoque", null, null));

                page.Content().Element(content =>
                {
                    content.PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Element(c => ComposeKpiRow(c, new[]
                        {
                            ("Produtos", totalItems.ToString()),
                            ("Unidades", totalUnits.ToString()),
                            ("Valor Total", FormatBrl(totalValue)),
                            ("Abaixo Mínimo", belowMin.ToString()),
                        }));

                        col.Item().Element(c => ComposeSectionTitle(c, "Posição de Estoque"));

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(1.2f);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "SKU", "Produto", "Estoque", "Mín", "Máx", "Custo Unit.", "Valor" })
                                    header.Cell().Background(PrimaryColor).Padding(5)
                                        .Text(h).FontSize(8).Bold().FontColor(Colors.White);
                            });

                            foreach (var item in items)
                            {
                                var isLow = item.MinStock.HasValue && item.TotalStock <= item.MinStock.Value;

                                ComposeTableCell(table, item.Sku, false);
                                ComposeTableCell(table, item.Name.Length > 35 ? item.Name[..32] + "..." : item.Name, false);
                                ComposeTableCell(table, item.TotalStock.ToString(), true, isLow ? DangerColor : null);
                                ComposeTableCell(table, item.MinStock?.ToString() ?? "-", true);
                                ComposeTableCell(table, item.MaxStock?.ToString() ?? "-", true);
                                ComposeTableCell(table, FormatBrl(item.UnitCost), true);
                                ComposeTableCell(table, FormatBrl(item.StockValue), true);
                            }
                        });
                    });
                });

                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    // --- Excel Export Methods ---

    public async Task<byte[]> ExportProfitabilityToExcelAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var start = dateFrom ?? DateTime.UtcNow.AddDays(-30);
        var end = dateTo ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        var skuData = orders
            .SelectMany(o => o.Items.Select(i => new { Item = i, Order = o }))
            .GroupBy(x => x.Item.Sku ?? "N/A")
            .Select(g =>
            {
                var revenue = g.Sum(x => x.Item.Quantity * x.Item.UnitPrice);
                var unitsSold = g.Sum(x => x.Item.Quantity);
                var productName = g.First().Item.Name ?? "N/A";
                var totalOrderRevenue = g.Select(x => x.Order).Distinct().Sum(o => o.TotalAmount);
                var totalOrderCosts = g.Select(x => x.Order).Distinct().Sum(o => o.Costs.Sum(c => c.Value));
                var proportion = totalOrderRevenue != 0 ? revenue / totalOrderRevenue : 0;
                var costs = totalOrderCosts * proportion;
                var profit = revenue - costs;
                var margin = revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0m;
                return new { Sku = g.Key, Name = productName, UnitsSold = unitsSold, Revenue = revenue, Costs = costs, Profit = profit, Margin = margin };
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Lucratividade");

        string[] headers = ["SKU", "Produto", "Qtd Vendida", "Receita (R$)", "Custos (R$)", "Lucro (R$)", "Margem (%)"];
        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        StyleHeaders(ws, headers.Length);

        for (int i = 0; i < skuData.Count; i++)
        {
            var s = skuData[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = s.Sku;
            ws.Cell(row, 2).Value = s.Name;
            ws.Cell(row, 3).Value = s.UnitsSold;
            ws.Cell(row, 4).Value = s.Revenue;
            ws.Cell(row, 4).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 5).Value = s.Costs;
            ws.Cell(row, 5).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 6).Value = s.Profit;
            ws.Cell(row, 6).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 7).Value = s.Margin;
            ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
        }

        ws.Columns().AdjustToContents();
        if (skuData.Count > 0)
            ws.Range(1, 1, skuData.Count + 1, headers.Length).SetAutoFilter();

        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportOrdersToExcelAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var start = dateFrom ?? DateTime.UtcNow.AddDays(-30);
        var end = dateTo ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Vendas");

        string[] headers = ["Pedido", "Data", "Comprador", "Itens", "Valor (R$)", "Custos (R$)", "Lucro (R$)", "Margem (%)", "Status"];
        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        StyleHeaders(ws, headers.Length);

        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i];
            var totalCosts = o.Costs.Sum(c => c.Value);
            var margin = o.TotalAmount != 0 ? Math.Round(o.Profit / o.TotalAmount * 100, 2) : 0m;
            var row = i + 2;

            ws.Cell(row, 1).Value = o.ExternalOrderId ?? o.Id.ToString()[..8];
            ws.Cell(row, 2).Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 3).Value = o.BuyerName ?? "N/A";
            ws.Cell(row, 4).Value = o.Items.Sum(item => item.Quantity);
            ws.Cell(row, 5).Value = o.TotalAmount;
            ws.Cell(row, 5).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 6).Value = totalCosts;
            ws.Cell(row, 6).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 7).Value = o.Profit;
            ws.Cell(row, 7).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 8).Value = margin;
            ws.Cell(row, 8).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 9).Value = o.Status ?? "N/A";
        }

        ws.Columns().AdjustToContents();
        if (orders.Count > 0)
            ws.Range(1, 1, orders.Count + 1, headers.Length).SetAutoFilter();

        return WorkbookToBytes(workbook);
    }

    public async Task<byte[]> ExportInventoryToExcelAsync(CancellationToken ct = default)
    {
        var products = await _db.Products
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Estoque");

        string[] headers = ["SKU", "Produto", "Estoque", "Mín", "Máx", "Custo Unit. (R$)", "Valor Estoque (R$)"];
        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        StyleHeaders(ws, headers.Length);

        var items = products.Select(p =>
        {
            var totalStock = p.Variants.Sum(v => v.Stock);
            return new { p.Sku, p.Name, TotalStock = totalStock, UnitCost = p.PurchaseCost, StockValue = totalStock * p.PurchaseCost, p.MinStock, p.MaxStock };
        }).ToList();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = item.Sku;
            ws.Cell(row, 2).Value = item.Name;
            ws.Cell(row, 3).Value = item.TotalStock;
            ws.Cell(row, 4).Value = item.MinStock?.ToString() ?? "";
            ws.Cell(row, 5).Value = item.MaxStock?.ToString() ?? "";
            ws.Cell(row, 6).Value = item.UnitCost;
            ws.Cell(row, 6).Style.NumberFormat.Format = BrlNumberFormat;
            ws.Cell(row, 7).Value = item.StockValue;
            ws.Cell(row, 7).Style.NumberFormat.Format = BrlNumberFormat;
        }

        ws.Columns().AdjustToContents();
        if (items.Count > 0)
            ws.Range(1, 1, items.Count + 1, headers.Length).SetAutoFilter();

        return WorkbookToBytes(workbook);
    }

    // --- Accounting Export (Bling / Tiny CSV) ---

    public async Task<byte[]> ExportAccountingAsync(string format, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var start = dateFrom ?? DateTime.UtcNow.AddDays(-30);
        var end = dateTo ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Costs)
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        return format.ToLowerInvariant() switch
        {
            "tiny" => GenerateTinyCsv(orders),
            _ => GenerateBlingCsv(orders), // default to bling
        };
    }

    private static byte[] GenerateBlingCsv(List<Core.Entities.Order> orders)
    {
        // Bling expected columns: Pedido;Data;Cliente;Email;Item;SKU;Qtd;Valor Unit.;Subtotal;
        // Comissão;Taxa Fixa;Frete Vendedor;Taxa Pagamento;Custo Produto;Embalagem;Imposto;Total Custos;Lucro
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Pedido;Data;Cliente;Email;Item;SKU;Qtd;Valor Unit.;Subtotal;Comissao;Taxa Fixa;Frete Vendedor;Taxa Pagamento;Custo Produto;Embalagem;Imposto;Total Custos;Lucro");

        foreach (var order in orders)
        {
            var costs = BuildCostMap(order.Costs);

            foreach (var item in order.Items)
            {
                sb.Append(CsvField(order.ExternalOrderId ?? order.Id.ToString()[..8])).Append(';');
                sb.Append(order.OrderDate.ToString("dd/MM/yyyy")).Append(';');
                sb.Append(CsvField(order.BuyerName ?? "")).Append(';');
                sb.Append(CsvField(order.BuyerEmail ?? "")).Append(';');
                sb.Append(CsvField(item.Name)).Append(';');
                sb.Append(CsvField(item.Sku)).Append(';');
                sb.Append(item.Quantity).Append(';');
                sb.Append(FormatDecimalCsv(item.UnitPrice)).Append(';');
                sb.Append(FormatDecimalCsv(item.Subtotal)).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("marketplace_commission"))).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("fixed_fee"))).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("shipping_seller"))).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("payment_fee"))).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("product_cost"))).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("packaging"))).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("tax"))).Append(';');
                sb.Append(FormatDecimalCsv(order.Costs.Sum(c => c.Value))).Append(';');
                sb.AppendLine(FormatDecimalCsv(order.Profit));
            }
        }

        return System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static byte[] GenerateTinyCsv(List<Core.Entities.Order> orders)
    {
        // Tiny expected columns: Numero Pedido;Data Pedido;Nome Cliente;Email Cliente;
        // Descricao Item;Codigo Item;Quantidade;Valor Unitario;Valor Total Item;
        // Valor Total Pedido;Total Impostos;Total Custos;Lucro Liquido
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero Pedido;Data Pedido;Nome Cliente;Email Cliente;Descricao Item;Codigo Item;Quantidade;Valor Unitario;Valor Total Item;Valor Total Pedido;Total Impostos;Total Custos;Lucro Liquido");

        foreach (var order in orders)
        {
            var costs = BuildCostMap(order.Costs);
            var totalCosts = order.Costs.Sum(c => c.Value);

            foreach (var item in order.Items)
            {
                sb.Append(CsvField(order.ExternalOrderId ?? order.Id.ToString()[..8])).Append(';');
                sb.Append(order.OrderDate.ToString("dd/MM/yyyy HH:mm")).Append(';');
                sb.Append(CsvField(order.BuyerName ?? "")).Append(';');
                sb.Append(CsvField(order.BuyerEmail ?? "")).Append(';');
                sb.Append(CsvField(item.Name)).Append(';');
                sb.Append(CsvField(item.Sku)).Append(';');
                sb.Append(item.Quantity).Append(';');
                sb.Append(FormatDecimalCsv(item.UnitPrice)).Append(';');
                sb.Append(FormatDecimalCsv(item.Subtotal)).Append(';');
                sb.Append(FormatDecimalCsv(order.TotalAmount)).Append(';');
                sb.Append(FormatDecimalCsv(costs.GetValueOrDefault("tax"))).Append(';');
                sb.Append(FormatDecimalCsv(totalCosts)).Append(';');
                sb.AppendLine(FormatDecimalCsv(order.Profit));
            }
        }

        return System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static Dictionary<string, decimal> BuildCostMap(ICollection<Core.Entities.OrderCost> costs)
    {
        return costs
            .GroupBy(c => c.Category)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
    }

    private static string CsvField(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string FormatDecimalCsv(decimal value)
    {
        return value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    // --- Shared Excel Helpers ---

    private static readonly string BrlNumberFormat = "#,##0.00";

    private static void StyleHeaders(IXLWorksheet ws, int columnCount)
    {
        var headerRange = ws.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A237E");
        headerRange.Style.Font.FontColor = XLColor.White;
    }

    private static byte[] WorkbookToBytes(XLWorkbook workbook)
    {
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // --- Shared PDF Composition Helpers ---

    private static void ComposeHeader(IContainer container, string title, DateTime? dateFrom, DateTime? dateTo)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("PeruShopHub")
                        .FontSize(20).Bold().FontColor(PrimaryColor);
                    c.Item().Text(title)
                        .FontSize(14).FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(200).AlignRight().Column(c =>
                {
                    if (dateFrom.HasValue && dateTo.HasValue)
                    {
                        c.Item().AlignRight().Text($"{dateFrom:dd/MM/yyyy} — {dateTo:dd/MM/yyyy}")
                            .FontSize(10).FontColor(Colors.Grey.Darken1);
                    }
                    c.Item().AlignRight().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(PrimaryColor);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("PeruShopHub — Gestão de Marketplaces")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });
    }

    private static void ComposeKpiRow(IContainer container, (string Label, string Value)[] kpis)
    {
        container.Row(row =>
        {
            foreach (var kpi in kpis)
            {
                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .Padding(8).Column(col =>
                    {
                        col.Item().Text(kpi.Label).FontSize(8).FontColor(Colors.Grey.Darken1);
                        col.Item().Text(kpi.Value).FontSize(12).Bold().FontColor(PrimaryColor);
                    });
            }
        });
    }

    private static void ComposeSectionTitle(IContainer container, string title)
    {
        container.PaddingTop(5).Text(title).FontSize(12).SemiBold().FontColor(PrimaryColor);
    }

    private static void ComposeTableCell(TableDescriptor table, string text, bool alignRight, string? fontColor = null)
    {
        var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
        var textBlock = alignRight
            ? cell.AlignRight().Text(text)
            : cell.Text(text);
        textBlock.FontSize(8);
        if (fontColor != null)
            textBlock.FontColor(fontColor);
    }

    private static string FormatBrl(decimal value)
    {
        return $"R$ {value:N2}".Replace(",", "X").Replace(".", ",").Replace("X", ".");
    }
}
