using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;

public class VerifyPrescriptionRequestDto
{
    [Required]
    public bool IsVerified { get; set; }

    public string? Notes { get; set; }
}
