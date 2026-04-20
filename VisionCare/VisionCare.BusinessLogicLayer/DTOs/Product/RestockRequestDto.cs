using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class RestockRequestDto
{
    [Required]
    public List<RestockItemDto> Items { get; set; } = new();
}

public class RestockItemDto
{
    [Required]
    public int VariantId { get; set; }
    
    [Required]
    public int Quantity { get; set; }
}
