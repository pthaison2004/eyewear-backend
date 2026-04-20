namespace VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;

public class PreOrderReceiveResultDto
{
    public int CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public int ReceivedQuantity { get; set; }
    public int TotalReceivedNow { get; set; }
    public int TotalReceived { get; set; }
    public int RemainingQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}