using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class BlacklistedIp
{
    public int Id { get; set; }
    public string IpAddress { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
