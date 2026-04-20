namespace VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

public class FulfillPreOrderResponseDto
{
    public int CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ReservationsFulfilled { get; set; }
    public int ReservationsPending { get; set; }
    public DateTime FulfilledAt { get; set; }
    public int FulfilledBy { get; set; }
    public string FulfilledByName { get; set; } = string.Empty;
    public string? Note { get; set; }
}
