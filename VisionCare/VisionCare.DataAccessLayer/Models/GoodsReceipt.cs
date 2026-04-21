using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class GoodsReceipt
{
    public int GoodsReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int? CampaignId { get; set; }
    public int CreatedBy { get; set; }
    public int? ManagerId { get; set; }
    public int WarehouseId { get; set; }
    public string Status { get; set; } = "draft"; // draft, completed, cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }

    public virtual PreOrderCampaign? Campaign { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual User? ManagerUser { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
    public virtual ICollection<GoodsReceiptDetail> Details { get; set; } = new List<GoodsReceiptDetail>();
}
