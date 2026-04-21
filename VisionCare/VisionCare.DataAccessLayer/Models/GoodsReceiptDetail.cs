using System;

namespace VisionCare.DataAccessLayer.Models;

public partial class GoodsReceiptDetail
{
    public int DetailId { get; set; }
    public int GoodsReceiptId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    
    public virtual GoodsReceipt? GoodsReceipt { get; set; }
    public virtual ProductVariant? Variant { get; set; }
}
