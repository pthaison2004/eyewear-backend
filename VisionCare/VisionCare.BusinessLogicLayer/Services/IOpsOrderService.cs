using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IOpsOrderService
{
    Task<OrderOpsDetailDto> PackOrderAsync(int orderId, int staffId);
    Task<OrderOpsDetailDto> UpdateOrderStatusAsync(int orderId, int staffId, UpdateOrderStatusRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetOrdersAsync(OpsOrderListRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetReadyMadeOrdersAsync(OpsOrderListRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetPrescriptionOrdersAsync(OpsOrderListRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetPreOrderOrdersAsync(OpsOrderListRequestDto request);

    Task<LensWorkDetailDto> GetLensWorkAsync(int orderId);
    Task<LensWorkDetailDto> AssignLensWorkAsync(int orderId, int staffId, AssignLensWorkRequestDto request);
    Task<LensWorkDetailDto> CompleteLensWorkAsync(int orderId, int staffId, CompleteLensWorkRequestDto request);
}
