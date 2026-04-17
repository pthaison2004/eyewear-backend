using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SalesPreOrderService : ISalesPreOrderService
{
    private readonly VisionCareContext _context;
    private readonly ILogger<SalesPreOrderService> _logger;

    public SalesPreOrderService(VisionCareContext context, ILogger<SalesPreOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

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

    public async Task<PreOrderStatusDto> GetPreOrderStatusAsync(int campaignId)
    {
        var campaign = await _context.PreOrderCampaigns
            .Include(c => c.CampaignProducts)
                .ThenInclude(cp => cp.Product)
            .Include(c => c.Reservations)
                .ThenInclude(r => r.Customer)
            .Include(c => c.Reservations)
                .ThenInclude(r => r.Variant)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            throw new KeyNotFoundException($"Campaign not found with ID: {campaignId}");
        }

        var paidReservations = campaign.Reservations.Count(r => r.Status.ToLower() == "paid");
        var fulfilledReservations = campaign.Reservations.Count(r => r.Status.ToLower() == "fulfilled");
        var cancelledReservations = campaign.Reservations.Count(r => r.Status.ToLower() == "cancelled");

        var productsAwaitingStock = campaign.CampaignProducts
            .Count(cp => cp.ReceivedQuantity < cp.ReservedQuantity);

        var productsWithFullStock = campaign.CampaignProducts
            .Count(cp => cp.ReceivedQuantity >= cp.ReservedQuantity);

        var recentReservations = campaign.Reservations
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new ReservationSummaryDto
            {
                ReservationId = r.ReservationId,
                ReservationCode = r.ReservationCode,
                CustomerName = r.Customer?.FullName ?? string.Empty,
                VariantSku = r.Variant?.Sku,
                Quantity = r.ReservedQuantity,
                UnitPrice = r.UnitPrice,
                Status = r.Status,
                PaidAt = r.PaidAt,
                FulfilledAt = r.FulfilledAt
            }).ToList();

        return new PreOrderStatusDto
        {
            CampaignId = campaign.CampaignId,
            CampaignCode = campaign.CampaignCode,
            CampaignName = campaign.CampaignName,
            Status = campaign.Status,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            ReleaseDate = campaign.ReleaseDate,
            TotalReservations = campaign.Reservations.Count,
            PaidReservations = paidReservations,
            FulfilledReservations = fulfilledReservations,
            CancelledReservations = cancelledReservations,
            TotalProducts = campaign.CampaignProducts.Count,
            ProductsWithFullStock = productsWithFullStock,
            ProductsAwaitingStock = productsAwaitingStock,
            RecentReservations = recentReservations
        };
    }

    public async Task<FulfillPreOrderResponseDto> FulfillPreOrderAsync(int campaignId, int staffId, FulfillPreOrderRequestDto request)
    {
        var campaign = await _context.PreOrderCampaigns
            .Include(c => c.CampaignProducts)
            .Include(c => c.Reservations)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null)
        {
            throw new KeyNotFoundException($"Campaign not found with ID: {campaignId}");
        }

        var paidReservations = campaign.Reservations
            .Where(r => r.Status.ToLower() == "paid")
            .ToList();

        foreach (var reservation in paidReservations)
        {
            reservation.Status = "fulfilled";
            reservation.FulfilledAt = DateTime.UtcNow;
        }

        if (request.ReceivedQuantity.HasValue)
        {
            foreach (var campaignProduct in campaign.CampaignProducts)
            {
                campaignProduct.ReceivedQuantity = request.ReceivedQuantity.Value;
            }
            campaign.UpdatedAt = DateTime.UtcNow;
        }

        var staff = await _context.Users.FindAsync(staffId);
        var staffName = staff?.FullName ?? "Unknown";

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            var noteEntry = $"[Fulfilled by Sales Staff: {staffName}] [{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Note}";

            if (string.IsNullOrWhiteSpace(campaign.Description))
            {
                campaign.Description = noteEntry;
            }
            else
            {
                campaign.Description += Environment.NewLine + noteEntry;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pre-order campaign {CampaignId} fulfilled by staff {StaffId}. Reservations fulfilled: {Count}",
            campaignId, staffId, paidReservations.Count);

        var pendingReservations = campaign.Reservations
            .Count(r => r.Status.ToLower() != "fulfilled" && r.Status.ToLower() != "cancelled");

        return new FulfillPreOrderResponseDto
        {
            CampaignId = campaign.CampaignId,
            CampaignCode = campaign.CampaignCode,
            CampaignName = campaign.CampaignName,
            Status = campaign.Status,
            ReservationsFulfilled = paidReservations.Count,
            ReservationsPending = pendingReservations,
            FulfilledAt = DateTime.UtcNow,
            FulfilledBy = staffId,
            FulfilledByName = staffName,
            Note = request.Note
        };
    }
}