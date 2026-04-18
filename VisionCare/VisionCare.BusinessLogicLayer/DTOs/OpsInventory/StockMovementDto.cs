namespace VisionCare.BusinessLogicLayer.DTOs.OpsInventory;

public class StockMovementDto
{
    public int MovementId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int QuantityBefore { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public string? StaffNote { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; }
}