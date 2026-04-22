using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class PreOrderCampaignProduct
{
    public int CampaignProductId { get; set; }
    public int CampaignId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public decimal CampaignPrice { get; set; }
    public int ReservedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual PreOrderCampaign? Campaign { get; set; }
    public virtual Product? Product { get; set; }
    public virtual ProductVariant? Variant { get; set; }
}