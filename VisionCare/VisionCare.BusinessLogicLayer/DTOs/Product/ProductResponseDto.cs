namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class ProductResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public decimal MinPrice { get; set; }
    public int TotalStock { get; set; }
    public bool? IsPreOrder { get; set; }
    public string? Image2D { get; set; }
    public string? Model3D { get; set; }
    public DateTime? CreatedAt { get; set; }
    public CategoryDto? Category { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
}
