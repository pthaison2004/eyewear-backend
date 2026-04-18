namespace VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

public class LensWorkDetailDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public List<LensWorkItemDto> Items { get; set; } = new();
}

public class LensWorkItemDto
{
    public int OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantInfo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? PrescriptionId { get; set; }
    public decimal? OdSphere { get; set; }
    public decimal? OdCylinder { get; set; }
    public int? OdAxis { get; set; }
    public decimal? OsSphere { get; set; }
    public decimal? OsCylinder { get; set; }
    public int? OsAxis { get; set; }
    public decimal? Pd { get; set; }
    public string? LensNote { get; set; }
    public int? AssignedLensMakerId { get; set; }
    public string? AssignedLensMakerName { get; set; }
    public DateTime? LensCutCompletedAt { get; set; }
    public string? LensCutNote { get; set; }
    public bool IsLensCutComplete { get; set; }
}
