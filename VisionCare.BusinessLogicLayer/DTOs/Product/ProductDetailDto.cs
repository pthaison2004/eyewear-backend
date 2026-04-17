namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class ProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool? IsPreOrder { get; set; }
    public string? Image2D { get; set; }
    public string? Model3D { get; set; }
    public DateTime? CreatedAt { get; set; }
    public CategoryDto? Category { get; set; }
    public List<ProductVariantDto> ProductVariants { get; set; } = new();
    public int TotalStock => ProductVariants.Sum(v => v.StockQuantity ?? 0);

    public bool IsFrame => Category?.CategoryId == 1 || Category?.CategoryId == 2 || Category?.CategoryId == 3;
    public bool IsLens => Category?.CategoryId == 5;
}

public class ProductVariantDto
{
    public int VariantId { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? Sku { get; set; }
    public int? StockQuantity { get; set; }
    public decimal AdditionalPrice { get; set; }
    public decimal EffectivePrice { get; set; }
}
