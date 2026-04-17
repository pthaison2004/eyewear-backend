namespace VisionCare.BusinessLogicLayer.DTOs.Shipping;

public class ShippingOrderDto
{
    public int ShippingOrderId { get; set; }
    public string ShippingOrderCode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int ShippingMethodId { get; set; }
    public string ShippingMethodName { get; set; } = string.Empty;
    public string? CarrierTrackingNo { get; set; }
    public string? CarrierOrderNo { get; set; }
    public string? CarrierStatus { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public decimal CodFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public decimal TotalShippingCost { get; set; }
    public int ShippingStatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}
