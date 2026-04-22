using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IManagerPreOrderService
{
    Task<GoodsReceiptDto> CreateGoodsReceiptAsync(int staffId, CreateGoodsReceiptDto request);
    Task<GoodsReceiptDto> CompleteGoodsReceiptAsync(int receiptId, int managerId, CompleteGoodsReceiptDto request);
    Task<ConvertPreOrderResultDto> ConvertReservationsToOrdersAsync(int campaignId, int managerId, ConvertPreOrdersDto request);
    Task<object> UpdateDepositConfigAsync(int campaignId, int managerId, UpdateDepositConfigDto request);

    // --- Campaign Strategy (Moved from Sales) ---
    Task<List<VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder.PreOrderCampaignListDto>> GetPreOrderCampaignsAsync(string? statusFilter);
    Task<VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder.PreOrderCampaignDto> CreateCampaignAsync(VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder.CreatePreOrderCampaignRequestDto request, int staffId);
    Task<VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder.PreOrderCampaignDto?> UpdateCampaignAsync(int campaignId, VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder.UpdatePreOrderCampaignRequestDto request, int staffId);
    Task<VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder.PreOrderCampaignDto?> GetCampaignDetailAsync(int campaignId);
}
