using System.Collections.Generic;
using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.OpsProcurement;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IOpsProcurementService
{
    Task<List<GoodsReceiptDto>> GetAllReceiptsAsync(string? status);
    Task<GoodsReceiptDto?> GetReceiptDetailAsync(int id);
    
    // Step 1: Ops creates PR
    Task<GoodsReceiptDto> CreatePurchaseRequestAsync(int staffId, CreatePurchaseRequestDto dto);
    
    // Step 2: Manager approves PR (becomes PO)
    Task<GoodsReceiptDto> ApprovePRAsync(int receiptId, int managerId);
    
    // Step 3: Ops uploads evidence (Proof Image)
    Task<GoodsReceiptDto> SubmitEvidenceAsync(int receiptId, int staffId, SubmitEvidenceDto dto);
    
    // Step 4: Manager final confirmation (Increases Inventory)
    Task<GoodsReceiptDto> FinalConfirmReceiptAsync(int receiptId, int managerId);
    
    Task<GoodsReceiptDto> CancelReceiptAsync(int receiptId, int userId, string reason);
}
