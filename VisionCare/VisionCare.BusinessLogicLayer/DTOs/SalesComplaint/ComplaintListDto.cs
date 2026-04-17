namespace VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;

public class ComplaintListDto
{
    public int ComplaintId { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ComplaintType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ComplaintStatus { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
