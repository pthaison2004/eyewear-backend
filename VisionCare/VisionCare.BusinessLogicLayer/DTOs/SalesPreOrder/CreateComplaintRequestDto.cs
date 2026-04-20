using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

public class CreateComplaintRequestDto
{
    [Required]
    public string ComplaintType { get; set; } = string.Empty;  // e.g. "Sản phẩm", "Giao hàng", "Dịch vụ", "Khác"

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = "normal";  // "low", "normal", "high", "urgent"
}
