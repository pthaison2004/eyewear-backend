using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public class StockMovement
{
    public int MovementId { get; set; }
    public int VariantId { get; set; }
    public int WarehouseId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int QuantityBefore { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public string? StaffNote { get; set; }
    public int? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }

    public virtual ProductVariant? Variant { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
    public virtual User? PerformedByUser { get; set; }
}