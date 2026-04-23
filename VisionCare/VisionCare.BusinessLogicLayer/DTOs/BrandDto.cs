using System;

namespace VisionCare.BusinessLogicLayer.DTOs;

public class BrandDto
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = null!;
    public string? Description { get; set; }
    public string? EvidenceUrl { get; set; }
    public string Status { get; set; } = null!;
    public string? RequestType { get; set; }
    public string? Reason { get; set; }
    public int? RequestedBy { get; set; }
    public string? RequesterName { get; set; }
    public DateTime RequestDate { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ManagerNote { get; set; }
}

public class CreateBrandRequestDto
{
    public string BrandName { get; set; } = null!;
    public string? Description { get; set; }
    public string? EvidenceUrl { get; set; }
}

public class BrandActionDto
{
    public string Status { get; set; } = null!; // Approved, Rejected
    public string? ManagerNote { get; set; }
}

public class DeleteBrandRequestDto
{
    public string Reason { get; set; } = null!;
    public string? EvidenceUrl { get; set; }
}
