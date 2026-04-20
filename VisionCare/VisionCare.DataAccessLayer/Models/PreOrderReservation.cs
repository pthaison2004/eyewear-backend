using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class PreOrderReservation
{
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public int CampaignId { get; set; }
    public int CustomerId { get; set; }
    public int VariantId { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = "reserved";
    public int? ConvertedOrderId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual PreOrderCampaign? Campaign { get; set; }
    public virtual User? Customer { get; set; }
    public virtual ProductVariant? Variant { get; set; }
    public virtual Order? ConvertedOrder { get; set; }
}