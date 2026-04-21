namespace VisionCare.BusinessLogicLayer.DTOs.SalesOrder;

public class SalesOrderListItemDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public bool HasPrescription { get; set; }
    public bool IsPrescriptionVerified { get; set; }
    public DateTime OrderDate { get; set; }
}
