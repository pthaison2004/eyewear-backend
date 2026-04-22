namespace VisionCare.DataAccessLayer.Models;

public class ShippingStatusHistory
{
    public int HistoryId { get; set; }
    public int ShippingOrderId { get; set; }
    public int? FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public string? CarrierStatusText { get; set; }
    public string? Location { get; set; }
    public DateTime? EstimatedDeliveryUpdated { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ShippingOrder? ShippingOrder { get; set; }
    public virtual ShippingStatus? FromStatus { get; set; }
    public virtual ShippingStatus? ToStatus { get; set; }
}
