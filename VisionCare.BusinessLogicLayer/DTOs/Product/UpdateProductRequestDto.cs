using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class UpdateProductRequestDto
{
    [MaxLength(200)]
    public string? ProductName { get; set; }

    public int? CategoryId { get; set; }

    public string? Brand { get; set; }
    
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BasePrice { get; set; }

    public bool? IsPreOrder { get; set; }

    public string? Image2D { get; set; }

    public string? Model3D { get; set; }
}
