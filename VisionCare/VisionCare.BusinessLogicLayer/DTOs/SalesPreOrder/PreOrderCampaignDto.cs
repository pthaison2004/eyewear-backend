namespace VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

public class PreOrderCampaignDto
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

    // Deposit configuration
    public decimal? DepositRatio { get; set; }  // e.g., 0.3 for 30%
    public decimal? MinDepositAmount { get; set; }  // VND minimum deposit

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePreOrderCampaignRequestDto
{
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? MaxQuantity { get; set; }
    public int MaxPerCustomer { get; set; } = 3;
    public bool IsFeatured { get; set; }

    // Deposit
    public decimal? DepositRatio { get; set; }  // e.g., 0.3 for 30%
    public decimal? MinDepositAmount { get; set; }
}

public class UpdatePreOrderCampaignRequestDto
{
    public string? CampaignName { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int? MaxQuantity { get; set; }
    public int? MaxPerCustomer { get; set; }
    public string? Status { get; set; }
    public bool? IsFeatured { get; set; }

    // Deposit - Admin adjustable
    public decimal? DepositRatio { get; set; }
    public decimal? MinDepositAmount { get; set; }
}