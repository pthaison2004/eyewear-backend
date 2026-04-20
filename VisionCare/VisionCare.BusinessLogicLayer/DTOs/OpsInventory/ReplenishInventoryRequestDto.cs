namespace VisionCare.BusinessLogicLayer.DTOs.OpsInventory;

public class ReplenishInventoryRequestDto
{
    public int VariantId { get; set; }
    public int? WarehouseId { get; set; }
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public string? Note { get; set; }
}