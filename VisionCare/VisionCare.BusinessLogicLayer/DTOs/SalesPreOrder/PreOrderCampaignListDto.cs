namespace VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

public class PreOrderCampaignListDto
{
    public int CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public int? MaxQuantity { get; set; }
    public int CurrentReserved { get; set; }
    public int TotalReservations { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int ProductsCount { get; set; }
    public int AvailableQuantity { get; set; }
}
