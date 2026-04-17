using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class Complaint
{
    public int ComplaintId { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string ComplaintType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ComplaintStatus { get; set; } = "open";
    public string Priority { get; set; } = "normal";
    public int? AssignedTo { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int? ProcessedBy { get; set; }
    public string? ProcessedNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public string? Resolution { get; set; }
    public int? CustomerSatisfaction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }
    public virtual User? Customer { get; set; }
    public virtual User? AssignedToUser { get; set; }
    public virtual User? ProcessedByUser { get; set; }
    public virtual User? ResolvedByUser { get; set; }
}