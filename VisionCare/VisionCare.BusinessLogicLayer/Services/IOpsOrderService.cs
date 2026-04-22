using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;
using VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;
using VisionCare.BusinessLogicLayer.DTOs.Shipping;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IOpsOrderService
{
    Task<OrderOpsDetailDto> PackOrderAsync(int orderId, int staffId);
    Task<OrderOpsDetailDto> UpdateOrderStatusAsync(int orderId, int staffId, UpdateOrderStatusRequestDto request);
    Task<OrderOpsDetailDto> GetOrderDetailAsync(int orderId);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetOrdersAsync(OpsOrderListRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetReadyMadeOrdersAsync(OpsOrderListRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetPrescriptionOrdersAsync(OpsOrderListRequestDto request);

    Task<PaginatedResultDto<OpsOrderListItemDto>> GetPreOrderOrdersAsync(OpsOrderListRequestDto request);

    Task<LensWorkDetailDto> GetLensWorkAsync(int orderId);
    Task<LensWorkDetailDto> AssignLensWorkAsync(int orderId, int staffId, AssignLensWorkRequestDto request);
    Task<LensWorkDetailDto> CompleteLensWorkAsync(int orderId, int staffId, CompleteLensWorkRequestDto request);

    Task<List<PreOrderReceiveListDto>> GetPreOrderReceiveListAsync(string? status, int? campaignId);
    Task<PreOrderReceiveResultDto> ReceivePreOrderAsync(int campaignId, int staffId, ReceivePreOrderRequestDto request);
    Task<ShippingOrderDto?> FulfillPreOrderAsync(int campaignId, int staffId, OpsFulfillPreOrderRequestDto request);
    Task<bool> MarkPreOrderStockArrivedAsync(int reservationId, int opsStaffId);
    Task<OpsOrderListItemDto?> GetPreOrderReservationDetailAsync(int reservationId);
}
