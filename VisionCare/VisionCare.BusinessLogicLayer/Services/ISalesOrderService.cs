using VisionCare.BusinessLogicLayer.DTOs.SalesOrder;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISalesOrderService
{
    Task<PaginatedResultDto<SalesOrderListItemDto>> GetOrdersAsync(SalesOrderListRequestDto request);
    Task<PaginatedResultDto<SalesOrderListItemDto>> GetPendingOrdersAsync(SalesOrderListRequestDto request);
    Task<SalesOrderDetailDto> GetOrderDetailAsync(int orderId);
    Task<SalesOrderDetailDto> ConfirmOrderAsync(int orderId, int staffId, ConfirmOrderRequestDto request);
    Task<SalesOrderDetailDto> RejectOrderAsync(int orderId, int staffId, RejectOrderRequestDto request);
    Task<SalesOrderDetailDto> AssignOrderAsync(int orderId, int staffId, AssignOrderRequestDto request);
    Task<SalesOrderDetailDto> AddStaffNoteAsync(int orderId, int staffId, StaffNoteRequestDto request);
}