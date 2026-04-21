using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class CreateProductRequestDto
{
    [Required]
    public string ProductName { get; set; } = null!;
    
    [Required]
    public int CategoryId { get; set; }
    
    public string? Brand { get; set; }
    public string? Description { get; set; }
    
    [Required]
    public decimal BasePrice { get; set; }
    
    public bool IsPreOrder { get; set; }
    public string? Image2D { get; set; }
    public bool IsFrame { get; set; }
    public bool IsLens { get; set; }
    public string? Model3D { get; set; }

    public List<CreateProductVariantDto> Variants { get; set; } = new();
}

public class CreateProductVariantDto
{
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? Sku { get; set; }
    public int? StockQuantity { get; set; }
    public decimal? AdditionalPrice { get; set; }
}
