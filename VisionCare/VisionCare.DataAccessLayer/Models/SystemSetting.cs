using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class SystemSetting
{
    public int SettingId { get; set; }
    public string SettingKey { get; set; } = null!;
    public string SettingValue { get; set; } = null!;
    public string? Description { get; set; }
    public string GroupName { get; set; } = "General";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
