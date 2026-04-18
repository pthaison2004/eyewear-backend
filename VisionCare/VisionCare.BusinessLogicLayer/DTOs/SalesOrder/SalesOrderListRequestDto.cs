namespace VisionCare.BusinessLogicLayer.DTOs.SalesOrder;

public class SalesOrderListRequestDto
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
