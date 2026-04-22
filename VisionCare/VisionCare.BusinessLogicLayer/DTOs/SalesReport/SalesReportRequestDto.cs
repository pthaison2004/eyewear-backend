namespace VisionCare.BusinessLogicLayer.DTOs.SalesReport;

public class SalesReportRequestDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Status { get; set; }
    public string? OrderType { get; set; }
}