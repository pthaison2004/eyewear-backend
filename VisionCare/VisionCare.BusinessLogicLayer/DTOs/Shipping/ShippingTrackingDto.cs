namespace VisionCare.BusinessLogicLayer.DTOs.Shipping;

public class ShippingTrackingDto
{
    public int ShippingOrderId { get; set; }
    public string ShippingOrderCode { get; set; } = string.Empty;
    public string? CarrierTrackingNo { get; set; }
    public string? CarrierOrderNo { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public List<ShippingStatusHistoryDto> History { get; set; } = new();
}
