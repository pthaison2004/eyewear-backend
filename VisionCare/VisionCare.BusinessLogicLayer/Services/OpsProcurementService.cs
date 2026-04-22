using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.OpsProcurement;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;
using VisionCare.BusinessLogicLayer.Constants;

namespace VisionCare.BusinessLogicLayer.Services;

public class OpsProcurementService : IOpsProcurementService
{
    private readonly VisionCareContext _context;

    public OpsProcurementService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<List<GoodsReceiptDto>> GetAllReceiptsAsync(string? status)
    {
        var query = _context.GoodsReceipts
            .AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ManagerUser)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v!.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status.ToLower() == status.ToLower());
        }

        var results = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return results.Select(MapToDto).ToList();
    }

    public async Task<GoodsReceiptDto?> GetReceiptDetailAsync(int id)
    {
        var receipt = await _context.GoodsReceipts
            .AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ManagerUser)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(r => r.GoodsReceiptId == id);

        return receipt == null ? null : MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto> CreatePurchaseRequestAsync(int staffId, CreatePurchaseRequestDto dto)
    {
        var receipt = new GoodsReceipt
        {
            WarehouseId = dto.WarehouseId,
            CreatedBy = staffId,
            CreatedAt = DateTime.UtcNow,
            Status = "PendingApproval",
            Note = dto.Note,
            ReceiptNumber = $"PR-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        _context.GoodsReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            var actualVariantId = item.VariantId;
            // Handle case where frontend might send ProductId instead of VariantId
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == actualVariantId);
            if (!variantExists)
            {
                var firstVariant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == actualVariantId);
                if (firstVariant != null)
                {
                    actualVariantId = firstVariant.VariantId;
                }
                else
                {
                    throw new Exception($"Invalid Product or Variant ID: {item.VariantId}");
                }
            }

            var detail = new GoodsReceiptDetail
            {
                GoodsReceiptId = receipt.GoodsReceiptId,
                VariantId = actualVariantId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            _context.GoodsReceiptDetails.Add(detail);
        }

        await _context.SaveChangesAsync();
        return (await GetReceiptDetailAsync(receipt.GoodsReceiptId))!;
    }

    public async Task<GoodsReceiptDto> ApprovePRAsync(int receiptId, int managerId)
    {
        var receipt = await _context.GoodsReceipts.FindAsync(receiptId);
        if (receipt == null) throw new KeyNotFoundException("Receipt not found.");

        if (receipt.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot approve receipt in status '{receipt.Status}'.");

        receipt.Status = "Approved";
        receipt.ManagerId = managerId;
        receipt.ApprovedAt = DateTime.UtcNow;
        receipt.ReceiptNumber = receipt.ReceiptNumber.Replace("PR-", "PO-");

        await _context.SaveChangesAsync();
        return (await GetReceiptDetailAsync(receiptId))!;
    }

    public async Task<GoodsReceiptDto> SubmitEvidenceAsync(int receiptId, int staffId, SubmitEvidenceDto dto)
    {
        var receipt = await _context.GoodsReceipts.FindAsync(receiptId);
        if (receipt == null) throw new KeyNotFoundException("Receipt not found.");

        if (receipt.Status != "Approved")
            throw new InvalidOperationException($"Cannot submit evidence for receipt in status '{receipt.Status}'.");

        receipt.Status = "AwaitingConfirmation";
        receipt.ProofImage = dto.ProofImage;
        if (!string.IsNullOrEmpty(dto.Note))
        {
            receipt.Note = string.IsNullOrEmpty(receipt.Note) ? dto.Note : $"{receipt.Note} | Evidence Note: {dto.Note}";
        }

        await _context.SaveChangesAsync();
        return (await GetReceiptDetailAsync(receiptId))!;
    }

    public async Task<GoodsReceiptDto> FinalConfirmReceiptAsync(int receiptId, int managerId)
    {
        var receipt = await _context.GoodsReceipts
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.GoodsReceiptId == receiptId);

        if (receipt == null) throw new KeyNotFoundException("Receipt not found.");

        if (receipt.Status != "AwaitingConfirmation")
            throw new InvalidOperationException($"Cannot confirm receipt in status '{receipt.Status}'. Need evidence upload first.");

        // 1. Update status
        receipt.Status = "Completed";
        receipt.CompletedAt = DateTime.UtcNow;
        receipt.ManagerId = managerId;

        // 2. Increase Inventory
        foreach (var detail in receipt.Details)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.VariantId == detail.VariantId && i.WarehouseId == receipt.WarehouseId);

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    VariantId = detail.VariantId,
                    WarehouseId = receipt.WarehouseId,
                    QuantityOnHand = 0,
                    QuantityReserved = 0,
                    QuantityDefective = 0,
                    QuantityTransit = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Inventories.Add(inventory);
            }

            int qtyBefore = inventory.QuantityOnHand;
            inventory.QuantityOnHand += detail.Quantity;
            inventory.LastReplenishAt = DateTime.UtcNow;
            inventory.UpdatedAt = DateTime.UtcNow;

            // Record Movement
            var movement = new StockMovement
            {
                VariantId = detail.VariantId,
                WarehouseId = receipt.WarehouseId,
                MovementType = "PURCHASE",
                QuantityBefore = qtyBefore,
                QuantityChange = detail.Quantity,
                QuantityAfter = inventory.QuantityOnHand,
                ReferenceType = "GoodsReceipt",
                ReferenceId = receiptId,
                Reason = "Inventory Procurement",
                PerformedBy = managerId,
                PerformedAt = DateTime.UtcNow
            };
            _context.StockMovements.Add(movement);

            // Sync Variant total quantity
            var variant = await _context.ProductVariants.FindAsync(detail.VariantId);
            if (variant != null)
            {
                variant.StockQuantity += detail.Quantity;
            }
        }

        // 3. Auto-notify Pre-orders waiting for this stock
        var variantIdsInReceipt = receipt.Details.Select(d => d.VariantId).ToList();
        var waitingPreOrders = await _context.PreOrderReservations
            .Where(r => variantIdsInReceipt.Contains(r.VariantId) && 
                       (r.Status.ToLower() == "sent_to_ops" || r.Status.ToLower() == "pending"))
            .ToListAsync();

        if (waitingPreOrders.Any())
        {
            var salesStaff = await _context.Users.Where(u => u.RoleId == 3).ToListAsync();
            foreach (var preOrder in waitingPreOrders)
            {
                preOrder.Status = PreOrderStatuses.StockArrived;
                
                foreach (var staff in salesStaff)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = staff.UserId,
                        Title = "Hàng Pre-order đã về kho",
                        Message = $"Đơn {preOrder.ReservationCode} đã có hàng về (tự động cập nhật từ phiếu nhập {receipt.ReceiptNumber}).",
                        Type = "PreOrder",
                        Link = $"/staff/pre-orders?reservationId={preOrder.ReservationId}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return (await GetReceiptDetailAsync(receiptId))!;
    }

    public async Task<GoodsReceiptDto> CancelReceiptAsync(int receiptId, int userId, string reason)
    {
        var receipt = await _context.GoodsReceipts.FindAsync(receiptId);
        if (receipt == null) throw new KeyNotFoundException("Receipt not found.");

        if (receipt.Status == "Completed")
            throw new InvalidOperationException("Cannot cancel a completed receipt.");

        receipt.Status = "Cancelled";
        receipt.Note = string.IsNullOrEmpty(receipt.Note) ? $"Cancelled: {reason}" : $"{receipt.Note} | Cancelled: {reason}";

        await _context.SaveChangesAsync();
        return (await GetReceiptDetailAsync(receiptId))!;
    }

    private static GoodsReceiptDto MapToDto(GoodsReceipt r)
    {
        return new GoodsReceiptDto
        {
            GoodsReceiptId = r.GoodsReceiptId,
            ReceiptCode = r.ReceiptNumber,
            CreatedBy = r.CreatedBy,
            CreatedByName = r.CreatedByUser?.FullName ?? "Staff",
            ManagerId = r.ManagerId,
            ManagerName = r.ManagerUser?.FullName,
            WarehouseId = r.WarehouseId,
            WarehouseName = r.Warehouse?.WarehouseName ?? "Warehouse",
            Status = r.Status,
            ProofImage = r.ProofImage,
            CreatedAt = r.CreatedAt,
            ApprovedAt = r.ApprovedAt,
            CompletedAt = r.CompletedAt,
            Note = r.Note,
            TotalAmount = r.Details.Sum(d => (d.UnitPrice ?? 0) * d.Quantity),
            Items = r.Details.Select(d => new GoodsReceiptDetailDto
            {
                DetailId = d.DetailId,
                VariantId = d.VariantId,
                ProductName = d.Variant?.Product?.ProductName ?? "Unknown",
                Sku = d.Variant?.Sku ?? "N/A",
                VariantInfo = $"{d.Variant?.Color} {d.Variant?.Size}".Trim(),
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList()
        };
    }
}
