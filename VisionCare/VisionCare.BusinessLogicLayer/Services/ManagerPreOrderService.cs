using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;
using Microsoft.Extensions.Logging;

namespace VisionCare.BusinessLogicLayer.Services;

public class ManagerPreOrderService : IManagerPreOrderService
{
    private readonly VisionCareContext _context;
    private readonly ILogger<ManagerPreOrderService> _logger;

    public ManagerPreOrderService(VisionCareContext context, ILogger<ManagerPreOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // --- Migrated Campaign Methods ---

    public async Task<List<PreOrderCampaignListDto>> GetPreOrderCampaignsAsync(string? statusFilter)
    {
        IQueryable<PreOrderCampaign> baseQuery = _context.PreOrderCampaigns;

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            baseQuery = baseQuery.Where(c => c.Status.ToLower() == statusFilter.ToLower());
        }

        var campaigns = await baseQuery
            .Include(c => c.CampaignProducts)
            .Include(c => c.Reservations)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(c => new PreOrderCampaignListDto
        {
            CampaignId = c.CampaignId,
            CampaignCode = c.CampaignCode,
            CampaignName = c.CampaignName,
            Description = c.Description,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            ReleaseDate = c.ReleaseDate,
            Status = c.Status,
            IsFeatured = c.IsFeatured,
            MaxQuantity = c.MaxQuantity,
            CurrentReserved = c.CurrentReserved,
            TotalReservations = c.Reservations.Count,
            DiscountPercent = c.DiscountPercent,
            DiscountAmount = c.DiscountAmount,
            ProductsCount = c.CampaignProducts.Count,
            AvailableQuantity = (c.MaxQuantity ?? 0) - c.CurrentReserved
        }).ToList();
    }

    public async Task<PreOrderCampaignDto> CreateCampaignAsync(CreatePreOrderCampaignRequestDto request, int staffId)
    {
        var year = DateTime.UtcNow.Year;
        var seqPrefix = $"PO-{year}-";
        var lastSeq = await _context.PreOrderCampaigns
            .Where(c => c.CampaignCode.StartsWith(seqPrefix))
            .Select(c => c.CampaignCode)
            .ToListAsync();

        int nextSeq = 1;
        if (lastSeq.Count > 0)
        {
            var lastCampaignCode = lastSeq.OrderByDescending(x => x).First();
            if (lastCampaignCode.Length > seqPrefix.Length)
            {
                var seqStr = lastCampaignCode.Substring(seqPrefix.Length);
                if (int.TryParse(seqStr, out var lastNum))
                    nextSeq = lastNum + 1;
            }
        }

        var campaignCode = $"{seqPrefix}{nextSeq:D3}";

        var campaign = new PreOrderCampaign
        {
            CampaignCode = campaignCode,
            CampaignName = request.CampaignName,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReleaseDate = request.ReleaseDate,
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = request.DiscountAmount,
            MaxQuantity = request.MaxQuantity,
            MaxPerCustomer = request.MaxPerCustomer,
            IsFeatured = request.IsFeatured,
            Status = "draft",
            DepositRatio = request.DepositRatio,
            MinDepositAmount = request.MinDepositAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.PreOrderCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pre-order campaign {CampaignCode} created by manager {StaffId}", campaignCode, staffId);

        return MapToCampaignDto(campaign);
    }

    public async Task<PreOrderCampaignDto?> UpdateCampaignAsync(int campaignId, UpdatePreOrderCampaignRequestDto request, int staffId)
    {
        var campaign = await _context.PreOrderCampaigns.FindAsync(campaignId);
        if (campaign == null) return null;

        if (request.CampaignName != null) campaign.CampaignName = request.CampaignName;
        if (request.Description != null) campaign.Description = request.Description;
        if (request.StartDate.HasValue) campaign.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) campaign.EndDate = request.EndDate.Value;
        if (request.ReleaseDate.HasValue) campaign.ReleaseDate = request.ReleaseDate.Value;
        if (request.DiscountPercent.HasValue) campaign.DiscountPercent = request.DiscountPercent.Value;
        if (request.DiscountAmount.HasValue) campaign.DiscountAmount = request.DiscountAmount.Value;
        if (request.MaxQuantity.HasValue) campaign.MaxQuantity = request.MaxQuantity.Value;
        if (request.MaxPerCustomer.HasValue) campaign.MaxPerCustomer = request.MaxPerCustomer.Value;
        if (request.Status != null) campaign.Status = request.Status;
        if (request.IsFeatured.HasValue) campaign.IsFeatured = request.IsFeatured.Value;
        if (request.DepositRatio.HasValue) campaign.DepositRatio = request.DepositRatio.Value;
        if (request.MinDepositAmount.HasValue) campaign.MinDepositAmount = request.MinDepositAmount.Value;

        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pre-order campaign {CampaignId} updated by manager {StaffId}", campaignId, staffId);
        return MapToCampaignDto(campaign);
    }

    public async Task<PreOrderCampaignDto?> GetCampaignDetailAsync(int campaignId)
    {
        var campaign = await _context.PreOrderCampaigns.FindAsync(campaignId);
        return campaign == null ? null : MapToCampaignDto(campaign);
    }

    // --- Existing Service Methods ---

    public async Task<GoodsReceiptDto> CreateGoodsReceiptAsync(int staffId, CreateGoodsReceiptDto request)
    {
        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == request.WarehouseId);
        if (!warehouseExists) throw new KeyNotFoundException($"Warehouse {request.WarehouseId} not found");

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
        return MapToReceiptDto(receipt);
    }

    public async Task<GoodsReceiptDto> CompleteGoodsReceiptAsync(int receiptId, int managerId, CompleteGoodsReceiptDto request)
    {
        var receipt = await _context.GoodsReceipts.Include(r => r.Details).FirstOrDefaultAsync(r => r.GoodsReceiptId == receiptId);
        if (receipt == null) throw new KeyNotFoundException($"GoodsReceipt {receiptId} not found");

        receipt.Status = "completed";
        receipt.ManagerId = managerId;
        receipt.CompletedAt = DateTime.UtcNow;

        foreach (var detail in receipt.Details)
        {
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.VariantId == detail.VariantId && i.WarehouseId == receipt.WarehouseId);
            if (inventory == null) {
                inventory = new Inventory { VariantId = detail.VariantId, WarehouseId = receipt.WarehouseId, QuantityOnHand = detail.Quantity, UpdatedAt = DateTime.UtcNow };
                _context.Inventories.Add(inventory);
            } else {
                inventory.QuantityOnHand += detail.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return MapToReceiptDto(receipt);
    }

    public async Task<ConvertPreOrderResultDto> ConvertReservationsToOrdersAsync(int campaignId, int managerId, ConvertPreOrdersDto request)
    {
        var campaign = await _context.PreOrderCampaigns.Include(c => c.Reservations).FirstOrDefaultAsync(c => c.CampaignId == campaignId);
        if (campaign == null) throw new KeyNotFoundException($"Campaign {campaignId} not found");

        var releasedReservations = campaign.Reservations.Where(r => r.Status != null && r.Status.ToLower() == "released" && !r.ConvertedOrderId.HasValue).ToList();
        
        int convertedCount = 0;
        foreach (var reservation in releasedReservations)
        {
            var order = new Order {
                CustomerId = reservation.CustomerId,
                OrderType = "Pre-order",
                OrderStatus = "Processing",
                PaymentStatus = "Paid",
                TotalAmount = reservation.UnitPrice * reservation.ReservedQuantity,
                PaidAmount = reservation.UnitPrice * reservation.ReservedQuantity,
                OrderDate = DateTime.UtcNow,
                StaffNote = request.Note ?? $"Converted from PreOrderReservation {reservation.ReservationCode}"
            };

            order.OrderItems.Add(new OrderItem { VariantId = reservation.VariantId, Quantity = reservation.ReservedQuantity, UnitPrice = reservation.UnitPrice });
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            reservation.ConvertedOrderId = order.OrderId;
            reservation.Status = "fulfilled";
            reservation.FulfilledAt = DateTime.UtcNow;
            convertedCount++;
        }

        await _context.SaveChangesAsync();
        return new ConvertPreOrderResultDto { CampaignId = campaignId, TotalConverted = convertedCount, Message = $"Successfully converted {convertedCount} reservations." };
    }

    public async Task<object> UpdateDepositConfigAsync(int campaignId, int managerId, UpdateDepositConfigDto request)
    {
        var campaign = await _context.PreOrderCampaigns.FindAsync(campaignId);
        if (campaign == null) throw new KeyNotFoundException($"Campaign {campaignId} not found");
        campaign.DepositRatio = request.DepositRatio;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new { CampaignId = campaign.CampaignId, DepositRatio = campaign.DepositRatio, Message = "Cập nhật tỉ lệ đặt cọc thành công." };
    }

    // --- Mappers ---

    private PreOrderCampaignDto MapToCampaignDto(PreOrderCampaign c) => new PreOrderCampaignDto
    {
        CampaignId = c.CampaignId, CampaignCode = c.CampaignCode, CampaignName = c.CampaignName,
        Description = c.Description, StartDate = c.StartDate, EndDate = c.EndDate,
        ReleaseDate = c.ReleaseDate, Status = c.Status, IsFeatured = c.IsFeatured,
        MaxQuantity = c.MaxQuantity, CurrentReserved = c.CurrentReserved,
        DiscountPercent = c.DiscountPercent, DiscountAmount = c.DiscountAmount,
        DepositRatio = c.DepositRatio, MinDepositAmount = c.MinDepositAmount,
        CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
    };

    private GoodsReceiptDto MapToReceiptDto(GoodsReceipt receipt) => new GoodsReceiptDto
    {
        GoodsReceiptId = receipt.GoodsReceiptId, ReceiptNumber = receipt.ReceiptNumber,
        CampaignId = receipt.CampaignId, CreatedBy = receipt.CreatedBy,
        ManagerId = receipt.ManagerId, WarehouseId = receipt.WarehouseId,
        Status = receipt.Status ?? string.Empty, CreatedAt = receipt.CreatedAt,
        CompletedAt = receipt.CompletedAt, Note = receipt.Note,
        Details = receipt.Details.Select(d => new GoodsReceiptDetailDto { DetailId = d.DetailId, VariantId = d.VariantId, Quantity = d.Quantity, UnitPrice = d.UnitPrice }).ToList()
    };
}
