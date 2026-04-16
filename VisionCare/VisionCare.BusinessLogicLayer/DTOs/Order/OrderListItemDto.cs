namespace VisionCare.BusinessLogicLayer.DTOs.Order;

public class OrderListItemDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public string OrderType { get; set; } = null!;
    public int ItemCount { get; set; }
}
