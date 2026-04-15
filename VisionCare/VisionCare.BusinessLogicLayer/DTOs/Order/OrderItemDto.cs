namespace VisionCare.BusinessLogicLayer.DTOs.Order;

public class OrderItemDto
{
    public int OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? VariantColor { get; set; }
    public string? VariantSize { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public int? PrescriptionId { get; set; }
}
