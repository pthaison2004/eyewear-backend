using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int OrderId { get; set; }

    public string FromStatus { get; set; } = string.Empty;

    public string ToStatus { get; set; } = string.Empty;

    public string? Note { get; set; }

    public int ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Order? Order { get; set; }

    public virtual User? ChangedByUser { get; set; }
}