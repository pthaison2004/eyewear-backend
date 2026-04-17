using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class PreOrderCampaign
{
    public int CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? MaxQuantity { get; set; }
    public int MaxPerCustomer { get; set; }
    public int CurrentReserved { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PreOrderCampaignProduct> CampaignProducts { get; set; } = new List<PreOrderCampaignProduct>();
    public virtual ICollection<PreOrderReservation> Reservations { get; set; } = new List<PreOrderReservation>();
}