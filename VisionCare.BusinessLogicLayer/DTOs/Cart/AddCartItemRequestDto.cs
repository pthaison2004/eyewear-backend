using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Cart;

public class AddCartItemRequestDto
{
    [Required]
    public int VariantId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    public int? PrescriptionId { get; set; }
}