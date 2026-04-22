using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISalesPreOrderService
{
    /// <summary>
    /// Lấy danh sách phiếu đặt trước (Reservations) cho Sales Staff
    /// </summary>
    Task<List<ReservationSummaryDto>> GetReservationsAsync(string? statusFilter);

    /// <summary>
    /// Bước 1: Sales gọi xác nhận thông tin với khách hàng
    /// </summary>
    Task<bool> ConfirmFirstCallAsync(int reservationId, int staffId);

    /// <summary>
    /// Bước 2: Chuyển yêu cầu chuẩn bị hàng cho bộ phận Ops
    /// </summary>
    Task<bool> SendToOpsAsync(int reservationId, int staffId);

    /// <summary>
    /// Bước 3: Sales gọi báo khách hàng có hàng và nhắc thanh toán 70% còn lại
    /// </summary>
    Task<bool> NotifyStockReadyAsync(int reservationId, int staffId);

    /// <summary>
    /// Bước cuối: Sau khi khách thanh toán đủ, Sales chuyển đơn cho Ops đóng gói và ship
    /// </summary>
    Task<bool> ReleaseToShippingAsync(int reservationId, int staffId);

    /// <summary>
    /// Lấy chi tiết đơn đặt giữ chỗ
    /// </summary>
    Task<ReservationSummaryDto?> GetReservationDetailAsync(int reservationId);
}