using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Cart;

public class AddCartComboRequestDto
{
    [Required]
    public int FrameVariantId { get; set; }

    [Required]
    public int LensVariantId { get; set; }

    [Required]
    public CreatePrescriptionDto Prescription { get; set; } = null!;
}

public class CreatePrescriptionDto
{
    public decimal? ODSphere { get; set; }
    public decimal? ODCylinder { get; set; }
    public int? ODAxis { get; set; }

    public decimal? OSSphere { get; set; }
    public decimal? OSCylinder { get; set; }
    public int? OSAxis { get; set; }

    public decimal? PD { get; set; }
    public string? Note { get; set; }
}
