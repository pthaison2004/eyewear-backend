using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class PrescriptionValidationRule
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;          // e.g. "Max Sphere Value", "Min Age"
    public string RuleType { get; set; } = string.Empty;           // "sphere_max", "cylinder_max", "pd_range", "min_age", "expiry_months"
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
