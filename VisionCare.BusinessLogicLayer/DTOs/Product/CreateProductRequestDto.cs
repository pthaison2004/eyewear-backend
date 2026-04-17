using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class CreateProductRequestDto
{
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
    [MaxLength(200)]
    public string ProductName { get; set; } = null!;

    [Required(ErrorMessage = "Danh mục là bắt buộc.")]
    public int CategoryId { get; set; }

    public string? Brand { get; set; }
    
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá cơ bản không được âm.")]
    public decimal BasePrice { get; set; }

    public bool IsPreOrder { get; set; } = false;

    public string? Image2D { get; set; }

    public string? Model3D { get; set; }

    // Danh sách biến thể ban đầu (Tùy chọn)
    public List<CreateProductVariantDto>? Variants { get; set; }
}

public class CreateProductVariantDto
{
    public string? Color { get; set; }
    public string? Size { get; set; }
    
    [Required(ErrorMessage = "SKU là bắt buộc.")]
    public string Sku { get; set; } = null!;
    
    public int StockQuantity { get; set; } = 0;
    public decimal AdditionalPrice { get; set; } = 0;
}
