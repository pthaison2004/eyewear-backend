namespace VisionCare.DataAccessLayer.Models;

public class ShippingMethod
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
    public bool CodAvailable { get; set; } = true;
    public decimal? MaxCodAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public virtual ICollection<ShippingOrder> ShippingOrders { get; set; } = new List<ShippingOrder>();
}
