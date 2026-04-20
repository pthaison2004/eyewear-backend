using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;

public class AdjustPrescriptionRequestDto
{
    [Required]
    public string ContactReason { get; set; } = string.Empty;

    public string? SuggestedCorrection { get; set; }

    public string? StaffNote { get; set; }
}
