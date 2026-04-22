namespace VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;

public class AdjustPrescriptionResponseDto
{
    public int PrescriptionId { get; set; }
    public string ContactReason { get; set; } = string.Empty;
    public string? SuggestedCorrection { get; set; }
    public string? StaffNote { get; set; }
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public DateTime ContactedAt { get; set; }
}
