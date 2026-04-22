namespace VisionCare.BusinessLogicLayer.DTOs.SalesOrder;

public class SalesOrderDetailDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? PackedAt { get; set; }
    public string? StaffNote { get; set; }
    public bool HasPrescription { get; set; }
    public bool IsPrescriptionVerified { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}

public class SalesOrderItemDto
{
    public int OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantInfo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }

    public int? PrescriptionId { get; set; }
    public decimal? OdSphere { get; set; }
    public decimal? OdCylinder { get; set; }
    public int? OdAxis { get; set; }
    public decimal? OsSphere { get; set; }
    public decimal? OsCylinder { get; set; }
    public int? OsAxis { get; set; }
    public decimal? Pd { get; set; }
    public string? PrescriptionNote { get; set; }
    public bool IsPrescriptionVerified { get; set; }
    public bool IsPrescriptionRejected { get; set; }
    public bool IsPrescriptionExpired { get; set; }
}
