namespace VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;

public class FulfillPreOrderRequestDto
{
    public int ShippingMethodId { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Note { get; set; }
}