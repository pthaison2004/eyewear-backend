namespace VisionCare.BusinessLogicLayer.DTOs.Order;

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public string OrderType { get; set; } = null!;
    public string? ShippingAddress { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateTime? PreOrderDeadline { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
