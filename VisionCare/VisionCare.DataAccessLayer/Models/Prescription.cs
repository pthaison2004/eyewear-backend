using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int? CustomerId { get; set; }

    public decimal? OdSphere { get; set; }

    public decimal? OdCylinder { get; set; }

    public int? OdAxis { get; set; }

    public decimal? OsSphere { get; set; }

    public decimal? OsCylinder { get; set; }

    public int? OsAxis { get; set; }

    public decimal? Pd { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
