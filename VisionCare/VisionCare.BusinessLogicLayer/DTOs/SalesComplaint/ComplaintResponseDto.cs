namespace VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;

public class ComplaintResponseDto
{
    public int ComplaintId { get; set; }
    public string ComplaintStatus { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
