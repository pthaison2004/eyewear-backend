namespace VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;

public class SalesPrescriptionListDto
{
    public int PrescriptionId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal? OdSphere { get; set; }
    public decimal? OdCylinder { get; set; }
    public decimal? OsSphere { get; set; }
    public decimal? OsCylinder { get; set; }
    public decimal? Pd { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVerified { get; set; }
    public bool IsRejected { get; set; }
    public int? LinkedOrderId { get; set; }
    public string? LinkedOrderCode { get; set; }
    public int? AgeInMonths { get; set; }
}
