using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISalesPreOrderService
{
    /// <summary>
    /// Lấy danh sách tất cả Pre-order campaigns cho Sales
    /// </summary>
    Task<List<PreOrderCampaignListDto>> GetPreOrderCampaignsAsync(string? statusFilter);

    /// <summary>
    /// Lấy trạng thái chi tiết của 1 campaign
    /// </summary>
    Task<PreOrderStatusDto> GetPreOrderStatusAsync(int campaignId);

    /// <summary>
    /// Sales staff đánh dấu đã nhận hàng pre-order về kho và fulfill các reservation
    /// </summary>
    Task<FulfillPreOrderResponseDto> FulfillPreOrderAsync(int campaignId, int staffId, FulfillPreOrderRequestDto request);

    /// <summary>
    /// Tạo chiến dịch pre-order mới (Manager/Admin)
    /// </summary>
    Task<PreOrderCampaignDto> CreateCampaignAsync(CreatePreOrderCampaignRequestDto request, int staffId);

    /// <summary>
    /// Cập nhật chiến dịch pre-order (Manager/Admin)
    /// </summary>
    Task<PreOrderCampaignDto?> UpdateCampaignAsync(int campaignId, UpdatePreOrderCampaignRequestDto request, int staffId);

    /// <summary>
    /// Lấy chi tiết 1 chiến dịch pre-order
    /// </summary>
    Task<PreOrderCampaignDto?> GetCampaignDetailAsync(int campaignId);
}