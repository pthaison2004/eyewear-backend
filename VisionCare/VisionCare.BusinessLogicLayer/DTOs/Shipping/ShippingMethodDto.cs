namespace VisionCare.BusinessLogicLayer.DTOs.Shipping;

public class ShippingMethodDto
{
    public int ShippingMethodId { get; set; }
    public string MethodCode { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal BaseFee { get; set; }
    public decimal FeePerKg { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public int? EstimatedDaysMin { get; set; }
    public int? EstimatedDaysMax { get; set; }
    public bool CodAvailable { get; set; }
    public decimal? MaxCodAmount { get; set; }
}
