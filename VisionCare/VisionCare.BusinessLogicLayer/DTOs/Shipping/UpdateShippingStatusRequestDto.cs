using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Shipping;

public class UpdateShippingStatusRequestDto
{
    [Required]
    public int StatusId { get; set; }
    public string? Note { get; set; }
    public string? Location { get; set; }
}
