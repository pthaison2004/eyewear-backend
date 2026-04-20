using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public class Inventory
{
    public int InventoryId { get; set; }
    public int VariantId { get; set; }
    public int WarehouseId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int QuantityDefective { get; set; }
    public int QuantityTransit { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LastCountAt { get; set; }
    public DateTime? LastReplenishAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved - QuantityDefective;

    public virtual ProductVariant? Variant { get; set; }
    public virtual Warehouse? Warehouse { get; set; }
}