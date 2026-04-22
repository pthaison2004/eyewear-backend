namespace VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

public class OpsOrderListRequestDto
{
    public string? Status { get; set; }
    public string? OrderType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
