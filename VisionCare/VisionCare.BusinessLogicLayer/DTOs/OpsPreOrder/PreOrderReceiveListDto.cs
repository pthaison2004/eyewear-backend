namespace VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;

public class PreOrderReceiveListDto
{
    public int CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public int TotalPaidReservations { get; set; }
    public int TotalReservedQuantity { get; set; }
    public int TotalReceivedQuantity { get; set; }
    public int PendingQuantity { get; set; }
    public bool IsReadyToFulfill { get; set; }
    public List<PreOrderReceiveItemDto> Items { get; set; } = new();
}

public class PreOrderReceiveItemDto
{
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ReservedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int PendingQuantity { get; set; }
    public decimal CampaignPrice { get; set; }
}