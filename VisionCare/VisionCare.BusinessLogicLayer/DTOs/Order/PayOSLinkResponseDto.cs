namespace VisionCare.BusinessLogicLayer.DTOs.Order;

public class PayOSLinkResponseDto
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
}
