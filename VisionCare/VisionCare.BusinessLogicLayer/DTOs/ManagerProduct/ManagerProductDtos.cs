using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.ManagerProduct;

public class CreateManagerProductDto
{
    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    public string? Brand { get; set; }
    
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm không được âm")]
    public decimal BasePrice { get; set; }

    public int CategoryId { get; set; } = 1;

    public bool IsPreOrder { get; set; } = false;

    public string? Image2D { get; set; }
}

public class UpdateManagerProductDto
{
    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    public string? Brand { get; set; }
    
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm không được âm")]
    public decimal BasePrice { get; set; }

    public int CategoryId { get; set; }

    public bool IsPreOrder { get; set; }

    public string? Image2D { get; set; }
}
