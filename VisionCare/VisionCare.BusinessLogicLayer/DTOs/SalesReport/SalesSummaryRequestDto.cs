namespace VisionCare.BusinessLogicLayer.DTOs.SalesReport;

public class SalesSummaryRequestDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Period { get; set; }
}