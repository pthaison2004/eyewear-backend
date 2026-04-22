namespace VisionCare.BusinessLogicLayer.DTOs.Cart;

public class CartResponseDto
{
    public int CartId { get; set; }
    public int CustomerId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}