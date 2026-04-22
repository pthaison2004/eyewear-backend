namespace VisionCare.BusinessLogicLayer.DTOs.Order;

public class CustomerReservationDto
{
    public int ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantSku { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ShippingAddress { get; set; }
    public int? ConvertedOrderId { get; set; }
}
