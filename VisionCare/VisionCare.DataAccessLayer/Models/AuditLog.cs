using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class AuditLog
{
    public int AuditId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}
