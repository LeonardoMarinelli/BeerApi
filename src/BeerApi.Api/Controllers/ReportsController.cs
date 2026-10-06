using System.Globalization;
using System.Text;
using BeerApi.Api.Authorization;
using BeerApi.Application.DTOs;
using BeerApi.Application.Reports;
using BeerApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(ReportService reportService) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] SalesReportQueryDto query, CancellationToken ct)
    {
        if (!TryGetBreweryScope(query.BreweryId, out var breweryId))
            return Forbid();

        var rows = await reportService.GetSalesAsync(query.From, query.To, query.GroupBy, breweryId, ct);
        return Format(
            query.Format,
            $"sales-{query.From?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "30d"}.csv",
            rows,
            ["period_start_utc", "sales_count", "quantity", "revenue"],
            row => [row.PeriodStartUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CsvReportFormatter.Number(row.SalesCount), CsvReportFormatter.Number(row.Quantity),
                CsvReportFormatter.Number(row.Revenue)]);
    }

    [HttpGet("top-beers")]
    public async Task<IActionResult> TopBeers([FromQuery] TopBeersReportQueryDto query, CancellationToken ct)
    {
        if (!TryGetBreweryScope(query.BreweryId, out var breweryId))
            return Forbid();

        var rows = await reportService.GetTopBeersAsync(query.From, query.To, query.Limit, breweryId, ct);
        return Format(
            query.Format,
            "top-beers.csv",
            rows,
            ["beer_id", "beer_name", "quantity", "revenue"],
            row => [CsvReportFormatter.Number(row.BeerId), row.BeerName,
                CsvReportFormatter.Number(row.Quantity), CsvReportFormatter.Number(row.Revenue)]);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] StockReportQueryDto query, CancellationToken ct)
    {
        if (!TryGetWholesalerScope(query.WholesalerId, out var wholesalerId))
            return Forbid();

        var rows = await reportService.GetStockAsync(wholesalerId, query.LowOnly, ct);
        return Format(
            query.Format,
            "stock.csv",
            rows,
            ["wholesaler_id", "wholesaler_name", "beer_id", "beer_name", "quantity", "is_low"],
            row => [CsvReportFormatter.Number(row.WholesalerId), row.WholesalerName,
                CsvReportFormatter.Number(row.BeerId), row.BeerName,
                CsvReportFormatter.Number(row.Quantity), row.IsLow ? "true" : "false"]);
    }

    [HttpGet("orders-by-status")]
    public async Task<IActionResult> OrdersByStatus([FromQuery] OrdersByStatusReportQueryDto query, CancellationToken ct)
    {
        int? breweryId = query.BreweryId;
        int? wholesalerId = query.WholesalerId;
        if (!User.IsInRole("Admin"))
        {
            if (User.IsInRole("Brewer"))
            {
                if (!TryGetBreweryScope(query.BreweryId, out breweryId))
                    return Forbid();
                wholesalerId = null;
            }
            else if (User.IsInRole("Wholesaler"))
            {
                if (!TryGetWholesalerScope(query.WholesalerId, out wholesalerId))
                    return Forbid();
                breweryId = null;
            }
            else
            {
                return Forbid();
            }
        }

        var rows = await reportService.GetOrdersByStatusAsync(breweryId, wholesalerId, ct);
        return Format(
            query.Format,
            "orders-by-status.csv",
            rows,
            ["status", "count"],
            row => [row.Status.ToString(), CsvReportFormatter.Number(row.Count)]);
    }

    private IActionResult Format<T>(
        string format,
        string fileName,
        IReadOnlyList<T> rows,
        IReadOnlyList<string> headers,
        Func<T, IReadOnlyList<string>> values)
    {
        switch (format.Trim().ToLowerInvariant())
        {
            case "json":
                return Ok(rows);
            case "csv":
                var csvRows = new List<IReadOnlyList<string>> { headers };
                csvRows.AddRange(rows.Select(values));
                var content = Encoding.UTF8.GetBytes(CsvReportFormatter.Write(csvRows));
                return File(content, "text/csv; charset=utf-8", fileName);
            default:
                throw new BeerApi.Domain.Exceptions.BusinessException("format deve ser json ou csv.");
        }
    }

    private bool TryGetBreweryScope(int? requestedBreweryId, out int? breweryId)
    {
        breweryId = requestedBreweryId;
        if (User.IsInRole("Admin"))
            return true;
        if (!User.IsInRole("Brewer") || !int.TryParse(User.FindFirst("BreweryId")?.Value, out var ownBreweryId) ||
            requestedBreweryId.HasValue && requestedBreweryId.Value != ownBreweryId)
            return false;

        breweryId = ownBreweryId;
        return true;
    }

    private bool TryGetWholesalerScope(int? requestedWholesalerId, out int? wholesalerId)
    {
        wholesalerId = requestedWholesalerId;
        if (User.IsInRole("Admin"))
            return true;
        if (!User.IsInRole("Wholesaler") || !int.TryParse(User.FindFirst("WholesalerId")?.Value, out var ownWholesalerId) ||
            requestedWholesalerId.HasValue && requestedWholesalerId.Value != ownWholesalerId)
            return false;

        wholesalerId = ownWholesalerId;
        return true;
    }
}