namespace VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

public class PreOrderStatusDto
{
    public int CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int TotalReservations { get; set; }
    public int PaidReservations { get; set; }
    public int FulfilledReservations { get; set; }
    public int CancelledReservations { get; set; }
    public int TotalProducts { get; set; }
    public int ProductsWithFullStock { get; set; }
    public int ProductsAwaitingStock { get; set; }
    public List<ReservationSummaryDto> RecentReservations { get; set; } = new();
}

public class ReservationSummaryDto
{
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? VariantSku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}
