namespace VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;

public class ReceivePreOrderRequestDto
{
    public int WarehouseId { get; set; }
    public int ReceivedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public string? Note { get; set; }
}