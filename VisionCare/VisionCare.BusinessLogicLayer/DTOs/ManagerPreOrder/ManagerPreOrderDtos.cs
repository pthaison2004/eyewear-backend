using System;
using System.Collections.Generic;

namespace VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;

public class CreateGoodsReceiptDto
{
    public int? CampaignId { get; set; }
    public int WarehouseId { get; set; }
    public string? Note { get; set; }
    public List<CreateGoodsReceiptDetailDto> Details { get; set; } = new();
}

public class CreateGoodsReceiptDetailDto
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class CompleteGoodsReceiptDto
{
    public string? Note { get; set; }
}

public class ConvertPreOrdersDto
{
    public string? Note { get; set; }
}

public class GoodsReceiptDto
{
    public int GoodsReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int? CampaignId { get; set; }
    public int CreatedBy { get; set; }
    public int? ManagerId { get; set; }
    public int WarehouseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }
    public List<GoodsReceiptDetailDto> Details { get; set; } = new();
}

public class GoodsReceiptDetailDto
{
    public int DetailId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class ConvertPreOrderResultDto
{
    public int TotalConverted { get; set; }
    public int CampaignId { get; set; }
    public string Message { get; set; } = string.Empty;
}
