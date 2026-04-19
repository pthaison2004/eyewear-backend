using VisionCare.BusinessLogicLayer.DTOs.Shipping;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IShippingService
{
    Task<List<ShippingMethodDto>> GetAvailableShippingMethodsAsync();
    Task<ShippingOrderDto> CreateShippingOrderAsync(int orderId, int staffId, CreateShippingOrderRequestDto request);
    Task<ShippingOrderDto?> MarkOrderAsShippedAsync(int orderId, int staffId);
    Task<List<ShippingStatusDto>> GetShippingStatusesAsync();
    Task<ShippingOrderDto?> UpdateShippingStatusAsync(int shippingOrderId, int staffId, UpdateShippingStatusRequestDto request);
    Task<List<ShippingStatusHistoryDto>> GetShippingHistoryAsync(int shippingOrderId);
    Task<ShippingTrackingDto?> TrackShippingAsync(string trackingNo);
    Task<ShippingOrderDto?> MarkAsDeliveredAsync(int orderId, int staffId);
}

public class ShippingStatusDto
{
    public int ShippingStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int StatusOrder { get; set; }
}
