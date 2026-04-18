namespace VisionCare.BusinessLogicLayer.DTOs.OpsInventory;

public class AdjustInventoryRequestDto
{
    public int VariantId { get; set; }
    public int? WarehouseId { get; set; }
    public int Adjustment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
}