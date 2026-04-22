namespace VisionCare.BusinessLogicLayer.DTOs.OpsInventory;

public class InventoryDetailDto
{
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantInfo { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityDefective { get; set; }
    public int QuantityTransit { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? LastReplenishAt { get; set; }
    public List<WarehouseStockDto> Warehouses { get; set; } = new();
}

public class WarehouseStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityDefective { get; set; }
    public int QuantityTransit { get; set; }
}