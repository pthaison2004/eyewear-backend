using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? OrderStatus { get; set; }

    public string? PaymentStatus { get; set; }

    public string? OrderType { get; set; }

    public string? ShippingAddress { get; set; }

    public string? TrackingNumber { get; set; }

    public string? StaffNote { get; set; }

    public DateTime? PackedAt { get; set; }

    public int? PackedBy { get; set; }

    public virtual User? Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
