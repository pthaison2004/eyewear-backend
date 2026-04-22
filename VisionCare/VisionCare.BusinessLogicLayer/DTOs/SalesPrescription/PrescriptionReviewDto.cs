namespace VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;

public class PrescriptionReviewDto
{
    public int PrescriptionId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // OD (Right Eye)
    public decimal? OdSphere { get; set; }
    public decimal? OdCylinder { get; set; }
    public int? OdAxis { get; set; }

    // OS (Left Eye)
    public decimal? OsSphere { get; set; }
    public decimal? OsCylinder { get; set; }
    public int? OsAxis { get; set; }

    public decimal? Pd { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    // Verification status
    public bool IsVerified { get; set; }
    public bool IsRejected { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedByName { get; set; }
    public string? RejectionReason { get; set; }

    // Linked orders
    public List<LinkedOrderDto> LinkedOrders { get; set; } = new();

    // Validation results (run rules against this prescription)
    public List<ValidationResultDto> ValidationResults { get; set; } = new();

    // Age info
    public int? AgeInMonths { get; set; }
    public bool IsExpired { get; set; }
    public bool IsOlderThan12Months { get; set; }
}

public class LinkedOrderDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public class ValidationResultDto
{
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public string Message { get; set; } = string.Empty;
}
