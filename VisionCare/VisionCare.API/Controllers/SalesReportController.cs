using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs.SalesReport;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1")]
[ApiController]
[Authorize(Roles = "Sales,Manager,Admin")]
public class SalesReportController : ControllerBase
{
    private readonly ISalesReportService _salesReportService;

    public SalesReportController(ISalesReportService salesReportService)
    {
        _salesReportService = salesReportService;
    }

    /// <summary>
    /// Tom tat bao cao Sales
    /// </summary>
    [HttpGet("dashboard/sales-summary")]
    public async Task<IActionResult> GetSalesSummary([FromQuery] SalesSummaryRequestDto request)
    {
        var result = await _salesReportService.GetSalesSummaryAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Bao cao chi tiet don hang Sales
    /// </summary>
    [HttpGet("reports/sales")]
    public async Task<IActionResult> GetSalesReport([FromQuery] SalesReportRequestDto request)
    {
        var result = await _salesReportService.GetSalesReportAsync(request);
        return Ok(result);
    }
}