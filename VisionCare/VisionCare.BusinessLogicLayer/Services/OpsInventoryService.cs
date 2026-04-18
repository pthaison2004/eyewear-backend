using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.OpsInventory;
using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class OpsInventoryService : IOpsInventoryService
{
    private readonly VisionCareContext _context;

    public OpsInventoryService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<InventoryDetailDto?> GetInventoryAsync(int variantId)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.Inventories)
            .ThenInclude(i => i.Warehouse)
            .FirstOrDefaultAsync(v => v.VariantId == variantId);

        if (variant == null)
            return null;

        var inventories = variant.Inventories.ToList();

        var dto = new InventoryDetailDto
        {
            VariantId = variant.VariantId,
            Sku = variant.Sku ?? string.Empty,
            ProductName = variant.Product?.ProductName ?? string.Empty,
            VariantInfo = BuildVariantInfo(variant),
            QuantityOnHand = inventories.Sum(i => i.QuantityOnHand),
            QuantityReserved = inventories.Sum(i => i.QuantityReserved),
            QuantityAvailable = inventories.Sum(i => i.QuantityAvailable),
            QuantityDefective = inventories.Sum(i => i.QuantityDefective),
            QuantityTransit = inventories.Sum(i => i.QuantityTransit),
            BatchNumber = inventories.FirstOrDefault(i => !string.IsNullOrEmpty(i.BatchNumber))?.BatchNumber,
            LastReplenishAt = inventories.Where(i => i.LastReplenishAt.HasValue)
                .OrderByDescending(i => i.LastReplenishAt)
                .FirstOrDefault()?.LastReplenishAt,
            Warehouses = inventories.Select(i => new WarehouseStockDto
            {
                WarehouseId = i.WarehouseId,
                WarehouseName = i.Warehouse?.WarehouseName ?? string.Empty,
                WarehouseType = i.Warehouse?.WarehouseType ?? string.Empty,
                QuantityOnHand = i.QuantityOnHand,
                QuantityAvailable = i.QuantityAvailable,
                QuantityDefective = i.QuantityDefective,
                QuantityTransit = i.QuantityTransit
            }).ToList()
        };

        return dto;
    }

    public async Task<List<LowStockItemDto>> GetLowStockAsync(int? warehouseId, int threshold = 10)
    {
        var query = _context.Inventories
            .Include(i => i.Variant)
            .ThenInclude(v => v!.Product)
            .Include(i => i.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var result = await query
            .Where(i => (i.QuantityOnHand - i.QuantityReserved - i.QuantityDefective) <= threshold)
            .Select(i => new LowStockItemDto
            {
                VariantId = i.VariantId,
                Sku = i.Variant!.Sku ?? string.Empty,
                ProductName = i.Variant.Product!.ProductName ?? string.Empty,
                VariantInfo = BuildVariantInfoFromParts(i.Variant.Color, i.Variant.Size),
                QuantityAvailable = (i.QuantityOnHand - i.QuantityReserved - i.QuantityDefective),
                LowStockThreshold = threshold,
                WarehouseName = i.Warehouse!.WarehouseName
            })
            .OrderBy(x => x.QuantityAvailable)
            .ToListAsync();

        return result;
    }

    public async Task<InventoryDetailDto> AdjustInventoryAsync(int variantId, int staffId, AdjustInventoryRequestDto request)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.VariantId == variantId);

        if (variant == null)
            throw new KeyNotFoundException($"Product variant with ID {variantId} not found.");

        var warehouseId = request.WarehouseId ?? 1;

        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == warehouseId);
        if (!warehouseExists)
            throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found.");

        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.VariantId == variantId && i.WarehouseId == warehouseId);

        if (inventory == null)
        {
            if (request.Adjustment < 0)
                throw new InvalidOperationException($"Cannot apply negative adjustment on non-existing inventory record. Please replenish first.");

            inventory = new Inventory
            {
                VariantId = variantId,
                WarehouseId = warehouseId,
                QuantityOnHand = 0,
                QuantityReserved = 0,
                QuantityDefective = 0,
                QuantityTransit = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        if (request.Adjustment < 0)
        {
            var availableQty = inventory.QuantityAvailable + request.Adjustment;
            if (availableQty < 0)
                throw new InvalidOperationException($"Adjustment would result in negative available quantity. Current available: {inventory.QuantityAvailable}, requested adjustment: {request.Adjustment}.");
        }

        var quantityBefore = inventory.QuantityOnHand;
        inventory.QuantityOnHand += request.Adjustment;
        inventory.LastCountAt = DateTime.UtcNow;
        inventory.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovement
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            MovementType = "ADJUSTMENT",
            QuantityBefore = quantityBefore,
            QuantityChange = request.Adjustment,
            QuantityAfter = inventory.QuantityOnHand,
            Reason = request.Reason,
            StaffNote = request.Note,
            PerformedBy = staffId,
            PerformedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);

        await SyncVariantStockQuantity(variantId);

        await _context.SaveChangesAsync();

        return (await GetInventoryAsync(variantId))!;
    }

    public async Task<InventoryDetailDto> ReplenishInventoryAsync(int variantId, int staffId, ReplenishInventoryRequestDto request)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.VariantId == variantId);

        if (variant == null)
            throw new KeyNotFoundException($"Product variant with ID {variantId} not found.");

        var warehouseId = request.WarehouseId ?? 1;

        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == warehouseId);
        if (!warehouseExists)
            throw new KeyNotFoundException($"Warehouse with ID {warehouseId} not found.");

        var inventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.VariantId == variantId && i.WarehouseId == warehouseId);

        if (inventory == null)
        {
            inventory = new Inventory
            {
                VariantId = variantId,
                WarehouseId = warehouseId,
                QuantityOnHand = 0,
                QuantityReserved = 0,
                QuantityDefective = 0,
                QuantityTransit = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Inventories.Add(inventory);
        }

        var quantityBefore = inventory.QuantityOnHand;
        inventory.QuantityOnHand += request.Quantity;
        inventory.LastReplenishAt = DateTime.UtcNow;
        inventory.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.BatchNumber))
            inventory.BatchNumber = request.BatchNumber;

        if (request.ManufacturingDate.HasValue)
            inventory.ManufacturingDate = request.ManufacturingDate;

        var movement = new StockMovement
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            MovementType = "PURCHASE",
            QuantityBefore = quantityBefore,
            QuantityChange = request.Quantity,
            QuantityAfter = inventory.QuantityOnHand,
            Reason = "Stock replenishment",
            StaffNote = request.Note,
            PerformedBy = staffId,
            PerformedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(movement);

        await SyncVariantStockQuantity(variantId);

        await _context.SaveChangesAsync();

        return (await GetInventoryAsync(variantId))!;
    }

    public async Task<PaginatedResultDto<StockMovementDto>> GetStockMovementsAsync(
        int? variantId, int? warehouseId, string? movementType, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var query = _context.StockMovements
            .Include(m => m.Variant)
            .ThenInclude(v => v!.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.PerformedByUser)
            .AsQueryable();

        if (variantId.HasValue)
            query = query.Where(m => m.VariantId == variantId.Value);

        if (warehouseId.HasValue)
            query = query.Where(m => m.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrEmpty(movementType))
            query = query.Where(m => m.MovementType == movementType);

        if (dateFrom.HasValue)
            query = query.Where(m => m.PerformedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(m => m.PerformedAt <= dateTo.Value);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var items = await query
            .OrderByDescending(m => m.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementDto
            {
                MovementId = m.MovementId,
                VariantId = m.VariantId,
                Sku = m.Variant!.Sku ?? string.Empty,
                ProductName = m.Variant.Product!.ProductName ?? string.Empty,
                MovementType = m.MovementType,
                QuantityBefore = m.QuantityBefore,
                QuantityChange = m.QuantityChange,
                QuantityAfter = m.QuantityAfter,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                Reason = m.Reason,
                StaffNote = m.StaffNote,
                PerformedByName = m.PerformedByUser != null ? m.PerformedByUser.FullName : null,
                PerformedAt = m.PerformedAt
            })
            .ToListAsync();

        return new PaginatedResultDto<StockMovementDto>
        {
            Success = true,
            Data = items,
            Meta = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            },
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<List<WarehouseStockDto>> GetWarehousesAsync()
    {
        return await _context.Warehouses
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.IsPrimary)
            .ThenBy(w => w.WarehouseName)
            .Select(w => new WarehouseStockDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseName = w.WarehouseName,
                WarehouseType = w.WarehouseType ?? string.Empty,
                QuantityOnHand = 0,
                QuantityAvailable = 0,
                QuantityDefective = 0,
                QuantityTransit = 0
            })
            .ToListAsync();
    }

    private async Task SyncVariantStockQuantity(int variantId)
    {
        var totalOnHand = await _context.Inventories
            .Where(i => i.VariantId == variantId)
            .SumAsync(i => i.QuantityOnHand);

        var variant = await _context.ProductVariants.FindAsync(variantId);
        if (variant != null)
            variant.StockQuantity = totalOnHand;
    }

    private static string BuildVariantInfo(ProductVariant variant)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(variant.Color))
            parts.Add(variant.Color);
        if (!string.IsNullOrEmpty(variant.Size))
            parts.Add(variant.Size);
        return string.Join(" / ", parts);
    }

    private static string BuildVariantInfoFromParts(string? color, string? size)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(color))
            parts.Add(color);
        if (!string.IsNullOrEmpty(size))
            parts.Add(size);
        return string.Join(" / ", parts);
    }
}