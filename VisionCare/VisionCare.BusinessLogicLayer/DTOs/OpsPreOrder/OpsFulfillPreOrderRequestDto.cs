namespace VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;

public class OpsFulfillPreOrderRequestDto
{
    public int ShippingMethodId { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Note { get; set; }
}