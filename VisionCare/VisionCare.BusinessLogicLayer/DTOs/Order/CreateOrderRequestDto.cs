using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Order;

public class CreateOrderRequestDto
{
    [Required]
    public string ShippingAddress { get; set; } = null!;

    public string OrderType { get; set; } = "Online"; // "Direct" | "Online" | "AtStore"
    public decimal ShippingFee { get; set; }
}
