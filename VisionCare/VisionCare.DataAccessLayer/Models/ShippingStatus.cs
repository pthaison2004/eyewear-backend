namespace VisionCare.DataAccessLayer.Models;

public class ShippingStatus
{
    public int ShippingStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int StatusOrder { get; set; }
    public string? Description { get; set; }

    public virtual ICollection<ShippingOrder> ShippingOrders { get; set; } = new List<ShippingOrder>();
}
