using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IManagerPreOrderService
{
    Task<GoodsReceiptDto> CreateGoodsReceiptAsync(int staffId, CreateGoodsReceiptDto request);
    Task<GoodsReceiptDto> CompleteGoodsReceiptAsync(int receiptId, int managerId, CompleteGoodsReceiptDto request);
    Task<ConvertPreOrderResultDto> ConvertReservationsToOrdersAsync(int campaignId, int managerId, ConvertPreOrdersDto request);
    Task<object> UpdateDepositConfigAsync(int campaignId, int managerId, UpdateDepositConfigDto request);
}
