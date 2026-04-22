using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Cart;

public class AddCartComboRequestDto
{
    [Required]
    public int FrameVariantId { get; set; }

    [Required]
    public int LensVariantId { get; set; }

    [Required]
    public CartPrescriptionRequestDto Prescription { get; set; } = new();
}

public class CartPrescriptionRequestDto
{
    public decimal? OdSphere { get; set; }

    public decimal? OdCylinder { get; set; }

    public int? OdAxis { get; set; }

    public decimal? OsSphere { get; set; }

    public decimal? OsCylinder { get; set; }

    public int? OsAxis { get; set; }

    public decimal? Pd { get; set; }

    public string? Note { get; set; }
}
