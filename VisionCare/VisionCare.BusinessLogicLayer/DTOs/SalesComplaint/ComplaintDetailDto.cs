namespace VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;

public class ComplaintDetailDto
{
    public int ComplaintId { get; set; }
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ComplaintType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ComplaintStatus { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public string? ProcessedNote { get; set; }
    public string? Resolution { get; set; }
    public int? CustomerSatisfaction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
