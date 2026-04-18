namespace VisionCare.BusinessLogicLayer.DTOs.Shipping;

public class ShippingStatusHistoryDto
{
    public int HistoryId { get; set; }
    public int ShippingOrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? CarrierStatusText { get; set; }
    public string? Location { get; set; }
    public DateTime UpdatedAt { get; set; }
}
