using System;
using System.Collections.Generic;

namespace VisionCare.DataAccessLayer.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Brand { get; set; }

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public bool? IsPreOrder { get; set; }

    public string? Image2D { get; set; }

    public string? Model3D { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Brand? BrandNavigation { get; set; }

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
