using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class RestockRequestDto
{
    [Required]
    public List<RestockItemDto> Items { get; set; } = new List<RestockItemDto>();
}

public class RestockItemDto
{
    [Required]
    public int VariantId { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}
