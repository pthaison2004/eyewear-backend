using VisionCare.BusinessLogicLayer.DTOs.SalesReport;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISalesReportService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(SalesSummaryRequestDto request);
    Task<SalesReportDto> GetSalesReportAsync(SalesReportRequestDto request);
}