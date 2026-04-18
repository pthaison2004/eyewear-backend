namespace VisionCare.BusinessLogicLayer.DTOs.OpsInventory;

public class LowStockItemDto
{
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantInfo { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int LowStockThreshold { get; set; }
    public string? WarehouseName { get; set; }
}