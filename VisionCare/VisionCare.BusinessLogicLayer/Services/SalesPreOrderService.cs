using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.DataAccessLayer.Models;
using VisionCare.BusinessLogicLayer.Constants;

namespace VisionCare.BusinessLogicLayer.Services;

public class SalesPreOrderService : ISalesPreOrderService
{
    private readonly VisionCareContext _context;
    private readonly ILogger<SalesPreOrderService> _logger;
    private readonly INotificationService _notificationService;

    public SalesPreOrderService(
        VisionCareContext context, 
        ILogger<SalesPreOrderService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<List<ReservationSummaryDto>> GetReservationsAsync(string? statusFilter)
    {
        IQueryable<PreOrderReservation> query = _context.PreOrderReservations
            .Include(r => r.Customer)
            .Include(r => r.Variant)
                .ThenInclude(v => v!.Product)
            .Include(r => r.Campaign);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(r => r.Status.ToLower() == statusFilter.ToLower());
        }

        var reservations = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reservations.Select(r => MapToSummaryDto(r)).ToList();
    }

    public async Task<bool> ConfirmFirstCallAsync(int reservationId, int staffId)
    {
        var reservation = await _context.PreOrderReservations.FindAsync(reservationId);
        if (reservation == null) return false;

        if (reservation.Status != PreOrderStatuses.Reserved)
        {
            throw new InvalidOperationException("Chỉ có thể xác nhận các đơn đặt trước mới (Reserved).");
        }

        reservation.Status = PreOrderStatuses.Confirmed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Staff {StaffId} confirmed reservation {ReservationId} (Stage 1)", staffId, reservationId);
        return true;
    }

    public async Task<bool> SendToOpsAsync(int reservationId, int staffId)
    {
        var reservation = await _context.PreOrderReservations.FindAsync(reservationId);
        if (reservation == null) return false;

        if (reservation.Status != PreOrderStatuses.Confirmed)
        {
            throw new InvalidOperationException("Cần xác nhận thông tin với khách trước khi chuyển sang bộ phận Ops.");
        }

        reservation.Status = PreOrderStatuses.SentToOps;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Staff {StaffId} sent reservation {ReservationId} to Ops", staffId, reservationId);
        
        // Notify Ops — wrap in try/catch so a missing Notifications table won't crash the action
        try
        {
            var opsUsers = await _context.Users.Where(u => u.RoleId == 4).ToListAsync();
            foreach (var ops in opsUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    ops.UserId,
                    "Đơn Pre-order mới cần xử lý",
                    $"Đơn {reservation.ReservationCode} đã được Sales xác nhận và cần kiểm tra kho.",
                    "PreOrder",
                    $"/operation?reservationId={reservationId}"
                );
            }
        }
        catch (Exception notifEx)
        {
            _logger.LogWarning(notifEx, "Failed to send Ops notification for reservation {ReservationId} — continuing anyway.", reservationId);
        }

        return true;
    }

    public async Task<bool> NotifyStockReadyAsync(int reservationId, int staffId)
    {
        var reservation = await _context.PreOrderReservations.FindAsync(reservationId);
        if (reservation == null) return false;

        if (reservation.Status != PreOrderStatuses.StockArrived)
        {
            throw new InvalidOperationException("Chỉ gọi báo khách khi bộ phận Ops đã xác nhận hàng về kho.");
        }

        reservation.Status = PreOrderStatuses.CustomerNotified;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Staff {StaffId} notified customer about stock arrival for reservation {ReservationId}", staffId, reservationId);
        
        // At this stage, the customer can now pay the remaining 70%
        // We could send an automated notification/email to the customer here if implemented.

        return true;
    }

    public async Task<bool> ReleaseToShippingAsync(int reservationId, int staffId)
    {
        var reservation = await _context.PreOrderReservations.FindAsync(reservationId);
        if (reservation == null) return false;

        // In a real system, we'd check if the invoice status is "Paid" (100% sum)
        // Here we assume if status is 'paid', they have paid the final amount.
        if (reservation.Status != PreOrderStatuses.Paid)
        {
            throw new InvalidOperationException("Khách hàng cần hoàn tất thanh toán 70% còn lại trước khi giao hàng.");
        }

        reservation.Status = PreOrderStatuses.Released;

        if (!reservation.ConvertedOrderId.HasValue)
        {
            var order = new Order {
                CustomerId = reservation.CustomerId,
                OrderType = "Pre-order",
                OrderStatus = "Processing",
                PaymentStatus = "Paid",
                TotalAmount = reservation.UnitPrice * reservation.ReservedQuantity,
                PaidAmount = reservation.UnitPrice * reservation.ReservedQuantity,
                OrderDate = DateTime.UtcNow,
                StaffNote = $"Auto-converted from PreOrderReservation {reservation.ReservationCode} upon Sales release",
                ShippingAddress = reservation.ShippingAddress
            };

            order.OrderItems.Add(new OrderItem { 
                VariantId = reservation.VariantId, 
                Quantity = reservation.ReservedQuantity, 
                UnitPrice = reservation.UnitPrice 
            });

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            reservation.ConvertedOrderId = order.OrderId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Staff {StaffId} released reservation {ReservationId} to Shipping and auto-converted to Order {OrderId}", staffId, reservationId, reservation.ConvertedOrderId);
        
        // Notify Ops to start packing — wrap in try/catch so notification errors don't crash the action
        try
        {
            var opsUsers = await _context.Users.Where(u => u.RoleId == 4).ToListAsync();
            foreach (var ops in opsUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    ops.UserId,
                    "Đơn Pre-order sẵn sàng đóng gói",
                    $"Đơn {reservation.ReservationCode} đã thanh toán đủ và có thể giao hàng.",
                    "PreOrder",
                    $"/operation?reservationId={reservationId}"
                );
            }
        }
        catch (Exception notifEx)
        {
            _logger.LogWarning(notifEx, "Failed to send shipping notification for reservation {ReservationId} — continuing anyway.", reservationId);
        }

        return true;
    }

    public async Task<ReservationSummaryDto?> GetReservationDetailAsync(int reservationId)
    {
        var reservation = await _context.PreOrderReservations
            .Include(r => r.Customer)
            .Include(r => r.Variant)
                .ThenInclude(v => v!.Product)
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

        return reservation == null ? null : MapToSummaryDto(reservation);
    }

    private static ReservationSummaryDto MapToSummaryDto(PreOrderReservation r) => new ReservationSummaryDto
    {
        ReservationId = r.ReservationId,
        ReservationCode = r.ReservationCode,
        CampaignName = r.Campaign?.CampaignName,
        CustomerName = r.Customer?.FullName ?? string.Empty,
        CustomerEmail = r.Customer?.Email,
        CustomerPhone = r.Customer?.PhoneNumber,
        ShippingAddress = r.ShippingAddress,
        ProductName = r.Variant?.Product?.ProductName,
        VariantSku = r.Variant?.Sku,
        Color = r.Variant?.Color,
        Size = r.Variant?.Size,
        Quantity = r.ReservedQuantity,
        UnitPrice = r.UnitPrice,
        Status = r.Status,
        PaidAt = r.PaidAt,
        FulfilledAt = r.FulfilledAt,
        CreatedAt = r.CreatedAt
    };
}