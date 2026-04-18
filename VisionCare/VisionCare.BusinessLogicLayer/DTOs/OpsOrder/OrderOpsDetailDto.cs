namespace VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

public class OrderOpsDetailDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? PackedAt { get; set; }
    public int? PackedBy { get; set; }
    public string StaffNote { get; set; } = string.Empty;
    public List<OrderItemOpsDto> Items { get; set; } = new();
}

public class OrderItemOpsDto
{
    public int OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantInfo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? PrescriptionId { get; set; }
}
