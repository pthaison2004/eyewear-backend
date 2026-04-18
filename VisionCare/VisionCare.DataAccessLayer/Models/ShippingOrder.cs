namespace VisionCare.DataAccessLayer.Models;

public class ShippingOrder
{
    public int ShippingOrderId { get; set; }
    public string ShippingOrderCode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public int ShippingMethodId { get; set; }

    public string? CarrierTrackingNo { get; set; }
    public string? CarrierOrderNo { get; set; }
    public string? CarrierStatus { get; set; }
    public DateTime? EstimatedDelivery { get; set; }

    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
    public string DistrictCode { get; set; } = string.Empty;
    public string WardCode { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string? DeliveryInstruction { get; set; }

    public decimal ShippingFee { get; set; }
    public decimal CodFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public decimal TotalShippingCost { get; set; }

    public int ShippingStatusId { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public virtual Order? Order { get; set; }
    public virtual ShippingMethod? ShippingMethod { get; set; }
    public virtual ICollection<ShippingStatusHistory> StatusHistories { get; set; } = new List<ShippingStatusHistory>();
}
