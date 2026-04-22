namespace VisionCare.BusinessLogicLayer.DTOs.Cart;

public class CartItemDto
{
    public int CartItemId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? VariantColor { get; set; }
    public string? VariantSize { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? CampaignPrice { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsPreOrder { get; set; }
    public int? PrescriptionId { get; set; }
}
