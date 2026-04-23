using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class Brand
{
    public int BrandId { get; set; }

    public string BrandName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EvidenceUrl { get; set; }

    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, PendingDeletion

    public string? RequestType { get; set; } // Add, Delete

    public string? Reason { get; set; }

    public int? RequestedBy { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? ManagerNote { get; set; }

    public virtual User? Requester { get; set; }

    public virtual User? Approver { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
