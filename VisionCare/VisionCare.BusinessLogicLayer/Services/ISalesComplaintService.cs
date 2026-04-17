using VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISalesComplaintService
{
    /// <summary>
    /// Lấy danh sách khiếu nại
    /// </summary>
    Task<List<ComplaintListDto>> GetComplaintsAsync(string? statusFilter, string? priorityFilter);

    /// <summary>
    /// Tạo khiếu nại mới cho 1 đơn hàng
    /// </summary>
    Task<ComplaintDetailDto> CreateComplaintAsync(int orderId, int staffId, CreateComplaintRequestDto request);

    /// <summary>
    /// Sales staff xử lý khiếu nại (assign + process)
    /// </summary>
    Task<ComplaintResponseDto> ProcessComplaintAsync(int complaintId, int staffId, ProcessComplaintRequestDto request);

    /// <summary>
    /// Sales staff giải quyết khiếu nại
    /// </summary>
    Task<ComplaintResponseDto> ResolveComplaintAsync(int complaintId, int staffId, ResolveComplaintRequestDto request);
}