using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class ManagerPreOrderService : IManagerPreOrderService
{
    private readonly VisionCareContext _context;

    public ManagerPreOrderService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<GoodsReceiptDto> CreateGoodsReceiptAsync(int staffId, CreateGoodsReceiptDto request)
    {
        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == request.WarehouseId);
        if (!warehouseExists)
            throw new KeyNotFoundException($"Warehouse {request.WarehouseId} not found");

        if (request.CampaignId.HasValue)
        {
            var campaignExists = await _context.PreOrderCampaigns.AnyAsync(c => c.CampaignId == request.CampaignId.Value);
            if (!campaignExists)
                throw new KeyNotFoundException($"Campaign {request.CampaignId.Value} not found");
        }

        var receipt = new GoodsReceipt
        {
            ReceiptNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
            CampaignId = request.CampaignId,
            WarehouseId = request.WarehouseId,
            CreatedBy = staffId,
            Status = "draft",
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var detail in request.Details)
        {
            receipt.Details.Add(new GoodsReceiptDetail
            {
                VariantId = detail.VariantId,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice
            });
        }

        _context.GoodsReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        return MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto> CompleteGoodsReceiptAsync(int receiptId, int managerId, CompleteGoodsReceiptDto request)
    {
        var receipt = await _context.GoodsReceipts
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.GoodsReceiptId == receiptId);

        if (receipt == null)
            throw new KeyNotFoundException($"GoodsReceipt {receiptId} not found");

        if (receipt.Status == "completed")
            throw new InvalidOperationException("Receipt is already completed.");

        receipt.Status = "completed";
        receipt.ManagerId = managerId;
        receipt.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Note))
            receipt.Note = string.IsNullOrEmpty(receipt.Note) ? request.Note : receipt.Note + "\n" + request.Note;

        foreach (var detail in receipt.Details)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.VariantId == detail.VariantId && i.WarehouseId == receipt.WarehouseId);

            int previousQuantity = 0;
            if (inventory == null)
            {
                inventory = new Inventory
                {
                    VariantId = detail.VariantId,
                    WarehouseId = receipt.WarehouseId,
                    QuantityOnHand = detail.Quantity,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                previousQuantity = inventory.QuantityOnHand;
                inventory.QuantityOnHand += detail.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            var stockMovement = new StockMovement
            {
                VariantId = detail.VariantId,
                WarehouseId = receipt.WarehouseId,
                MovementType = "GOODS_RECEIPT",
                ReferenceType = "receipt",
                ReferenceId = receipt.GoodsReceiptId,
                QuantityBefore = previousQuantity,
                QuantityChange = detail.Quantity,
                QuantityAfter = inventory.QuantityOnHand,
                PerformedBy = managerId,
                PerformedAt = DateTime.UtcNow,
                StaffNote = "Completed Goods Receipt"
            };
            _context.StockMovements.Add(stockMovement);
        }

        await _context.SaveChangesAsync();
        return MapToDto(receipt);
    }

    public async Task<ConvertPreOrderResultDto> ConvertReservationsToOrdersAsync(int campaignId, int managerId, ConvertPreOrdersDto request)
    {
        var campaign = await _context.PreOrderCampaigns
            .Include(c => c.Reservations)
            .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
            throw new KeyNotFoundException($"Campaign {campaignId} not found");

        var paidReservations = campaign.Reservations
            .Where(r => r.Status != null && r.Status.ToLower() == "paid" && !r.ConvertedOrderId.HasValue)
            .ToList();

        if (!paidReservations.Any())
            return new ConvertPreOrderResultDto { CampaignId = campaignId, TotalConverted = 0, Message = "No eligible paid reservations found." };

        int convertedCount = 0;
        foreach (var reservation in paidReservations)
        {
            var order = new Order
            {
                CustomerId = reservation.CustomerId,
                OrderType = "Pre-order",
                OrderStatus = "Processing",
                PaymentStatus = "PartialPaid",
                TotalAmount = reservation.UnitPrice * reservation.ReservedQuantity,
                OrderDate = DateTime.UtcNow,
                StaffNote = request.Note ?? $"Converted from PreOrderReservation {reservation.ReservationCode}"
            };

            order.OrderItems.Add(new OrderItem
            {
                VariantId = reservation.VariantId,
                Quantity = reservation.ReservedQuantity,
                UnitPrice = reservation.UnitPrice
            });

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save to get OrderId

            reservation.ConvertedOrderId = order.OrderId;
            reservation.Status = "fulfilled";
            reservation.FulfilledAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                FromStatus = "Pending",
                ToStatus = "Processing",
                Note = "Converted from PreOrderReservation",
                ChangedBy = managerId,
                ChangedAt = DateTime.UtcNow
            });

            convertedCount++;
        }

        campaign.Status = "fulfilled";
        await _context.SaveChangesAsync();

        return new ConvertPreOrderResultDto
        {
            CampaignId = campaignId,
            TotalConverted = convertedCount,
            Message = $"Successfully converted {convertedCount} reservations to orders."
        };
    }

    public async Task<object> UpdateDepositConfigAsync(int campaignId, int managerId, UpdateDepositConfigDto request)
    {
        var campaign = await _context.PreOrderCampaigns
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
            throw new KeyNotFoundException($"Campaign {campaignId} not found");

        campaign.DepositRatio = request.DepositRatio;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new
        {
            CampaignId = campaign.CampaignId,
            DepositRatio = campaign.DepositRatio,
            Message = "Cập nhật tỉ lệ đặt cọc thành công."
        };
    }

    private GoodsReceiptDto MapToDto(GoodsReceipt receipt)
    {
        return new GoodsReceiptDto
        {
            GoodsReceiptId = receipt.GoodsReceiptId,
            ReceiptNumber = receipt.ReceiptNumber,
            CampaignId = receipt.CampaignId,
            CreatedBy = receipt.CreatedBy,
            ManagerId = receipt.ManagerId,
            WarehouseId = receipt.WarehouseId,
            Status = receipt.Status ?? string.Empty,
            CreatedAt = receipt.CreatedAt,
            CompletedAt = receipt.CompletedAt,
            Note = receipt.Note,
            Details = receipt.Details.Select(d => new GoodsReceiptDetailDto
            {
                DetailId = d.DetailId,
                VariantId = d.VariantId,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList()
        };
    }
}
