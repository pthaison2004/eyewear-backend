using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

public class FulfillPreOrderRequestDto
{
    public string? Note { get; set; }
    public int? ReceivedQuantity { get; set; }  // optional: override received quantity
}
