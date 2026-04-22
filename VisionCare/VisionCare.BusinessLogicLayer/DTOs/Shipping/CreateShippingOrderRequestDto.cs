using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Shipping;

public class CreateShippingOrderRequestDto
{
    [Required]
    public int ShippingMethodId { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? DeclaredValue { get; set; }
}
