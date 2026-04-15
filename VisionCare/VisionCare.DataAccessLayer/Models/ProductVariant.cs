using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int? ProductId { get; set; }

    public string? Color { get; set; }

    public string? Size { get; set; }

    public string? Sku { get; set; }

    public int? StockQuantity { get; set; }

    public decimal? AdditionalPrice { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product? Product { get; set; }
}
