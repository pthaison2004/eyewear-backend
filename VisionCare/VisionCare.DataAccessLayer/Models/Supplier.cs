using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
