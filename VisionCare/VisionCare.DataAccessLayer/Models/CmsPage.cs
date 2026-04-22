using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class CmsPage
{
    public int PageId { get; set; }
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
