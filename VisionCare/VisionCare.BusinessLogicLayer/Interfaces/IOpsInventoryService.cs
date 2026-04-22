using VisionCare.BusinessLogicLayer.DTOs.OpsInventory;
using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IOpsInventoryService
{
    Task<InventoryDetailDto?> GetInventoryAsync(int variantId);
    Task<List<LowStockItemDto>> GetLowStockAsync(int? warehouseId, int threshold = 10);
    Task<InventoryDetailDto> AdjustInventoryAsync(int variantId, int staffId, AdjustInventoryRequestDto request);
    Task<InventoryDetailDto> ReplenishInventoryAsync(int variantId, int staffId, ReplenishInventoryRequestDto request);
    Task<PaginatedResultDto<StockMovementDto>> GetStockMovementsAsync(int? variantId, int? warehouseId, string? movementType, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
    Task<List<WarehouseStockDto>> GetWarehousesAsync();
}