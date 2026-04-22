namespace VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

public class UpdateOrderStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}
