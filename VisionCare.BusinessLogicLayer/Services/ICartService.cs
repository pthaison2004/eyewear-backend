using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Cart;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ICartService
{
    Task<CartResponseDto> GetCartAsync(int customerId);
    Task<CartResponseDto> AddItemAsync(int customerId, AddCartItemRequestDto request);
    Task<CartResponseDto> UpdateItemAsync(int cartItemId, int customerId, UpdateCartItemRequestDto request);
    Task<CartResponseDto> RemoveItemAsync(int cartItemId, int customerId);
    Task<bool> ClearCartAsync(int customerId);
    Task<CartResponseDto> AddComboAsync(int customerId, AddCartComboRequestDto request);
}