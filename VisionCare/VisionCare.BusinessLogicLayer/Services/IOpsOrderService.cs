using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IOpsOrderService
{
    Task<OrderOpsDetailDto> PackOrderAsync(int orderId, int staffId);
    Task<OrderOpsDetailDto> UpdateOrderStatusAsync(int orderId, int staffId, UpdateOrderStatusRequestDto request);
}
