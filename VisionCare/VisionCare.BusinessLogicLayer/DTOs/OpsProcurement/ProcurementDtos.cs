using System;
using System.Collections.Generic;

namespace VisionCare.BusinessLogicLayer.DTOs.OpsProcurement;

public class CreatePurchaseRequestDto
{
    public int WarehouseId { get; set; } = 1;
    public string? Note { get; set; }
    public List<ProcurementItemDto> Items { get; set; } = new();
}

public class ProcurementItemDto
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class GoodsReceiptDto
{
    public int GoodsReceiptId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProofImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public List<GoodsReceiptDetailDto> Items { get; set; } = new();
}

public class GoodsReceiptDetailDto
{
    public int DetailId { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string VariantInfo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal SubTotal => (UnitPrice ?? 0) * Quantity;
}

public class SubmitEvidenceDto
{
    public string ProofImage { get; set; } = string.Empty;
    public string? Note { get; set; }
}
