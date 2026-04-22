using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;

public class ResolveComplaintRequestDto
{
    [Required]
    public string Resolution { get; set; } = string.Empty;

    public string? ProcessedNote { get; set; }  // additional notes
}
