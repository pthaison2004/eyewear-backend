using VisionCare.BusinessLogicLayer.DTOs.Shipping;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IShippingService
{
    Task<List<ShippingMethodDto>> GetAvailableShippingMethodsAsync();
    Task<ShippingOrderDto> CreateShippingOrderAsync(int orderId, int staffId, CreateShippingOrderRequestDto request);
    Task<ShippingOrderDto?> MarkOrderAsShippedAsync(int orderId, int staffId);
}
