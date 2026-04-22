using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayOS;
using VisionCare.BusinessLogicLayer.DTOs.Order;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class CustomerPreOrderService : ICustomerPreOrderService
{
    private readonly VisionCareContext _context;
    private readonly PayOSClient _payOS;
    private readonly IConfiguration _configuration;

    public CustomerPreOrderService(VisionCareContext context, PayOSClient payOS, IConfiguration configuration)
    {
        _context = context;
        _payOS = payOS;
        _configuration = configuration;
    }

    public async Task<PayOSLinkResponseDto> CreateDepositLinkAsync(int reservationId, int customerId)
    {
        var reservation = await _context.PreOrderReservations
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đặt chỗ Pre-order.");

        if (reservation.Status.ToLower() != "reserved")
            throw new InvalidOperationException($"Không thể thanh toán cọc lúc này (Trạng thái: {reservation.Status}).");

        var depositRatio = reservation.Campaign?.DepositRatio ?? 0.3m;
        if (depositRatio <= 0)
            depositRatio = 0.3m;

        var totalAmount = reservation.UnitPrice * reservation.ReservedQuantity;
        var depositAmount = (int)Math.Round(totalAmount * depositRatio);

        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long orderCode = long.Parse($"{reservation.ReservationId}{currentTimestamp}");

        var domain = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:5173/payment/success";
        var cancelDomain = _configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel";

        var paymentData = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = depositAmount,
            Description = $"Coc PO {reservation.ReservationId}".Substring(0, Math.Min($"Coc PO {reservation.ReservationId}".Length, 25)),
            CancelUrl = $"{cancelDomain}?reservationId={reservation.ReservationId}",
            ReturnUrl = $"{domain}?reservationId={reservation.ReservationId}"
        };

        var createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);

        reservation.PaymentLinkId = createPayment.PaymentLinkId;
        await _context.SaveChangesAsync();

        return new PayOSLinkResponseDto
        {
            PaymentLinkId = createPayment.PaymentLinkId,
            CheckoutUrl = createPayment.CheckoutUrl
        };
    }

    public async Task<object> CheckDepositPaymentStatusAsync(int reservationId, string paymentLinkId, int customerId)
    {
        var reservation = await _context.PreOrderReservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đặt chỗ Pre-order.");

        if (reservation.PaidAt != null)
        {
            return new
            {
                OrderId = reservation.ReservationId,
                PaymentLinkId = paymentLinkId,
                IsPaid = true,
                Status = "DepositPaid"
            };
        }

        if (reservation.PaymentLinkId != paymentLinkId)
            throw new InvalidOperationException("PaymentLinkId không khớp.");

        var paymentInfo = await _payOS.PaymentRequests.GetAsync(paymentLinkId);

        if (paymentInfo.Status.ToString().Equals("PAID", StringComparison.OrdinalIgnoreCase))
        {
            reservation.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new
            {
                OrderId = reservation.ReservationId,
                PaymentLinkId = paymentLinkId,
                IsPaid = true,
                Status = "DepositPaid"
            };
        }

        return new
        {
            OrderId = reservation.ReservationId,
            PaymentLinkId = paymentLinkId,
            IsPaid = false,
            Status = paymentInfo.Status.ToString()
        };
    }

    public async Task<bool> SimulatePaymentSuccessAsync(int reservationId, int customerId)
    {
        var reservation = await _context.PreOrderReservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đặt chỗ Pre-order.");

        if (reservation.Status.ToLower() == "reserved" && reservation.PaidAt == null)
        {
            reservation.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return reservation.PaidAt != null;
    }

    public async Task<List<CustomerReservationDto>> GetMyReservationsAsync(int customerId)
    {
        var reservations = await _context.PreOrderReservations
            .Include(r => r.Variant)
                .ThenInclude(v => v!.Product)
            .Include(r => r.Campaign)
            .Where(r => r.CustomerId == customerId && r.Status != "cancelled" && r.Status != "fulfilled")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var convertedOrderIds = reservations.Where(r => r.ConvertedOrderId.HasValue).Select(r => r.ConvertedOrderId!.Value).ToList();
        var orders = new Dictionary<int, Order>();
        if (convertedOrderIds.Any())
        {
            orders = await _context.Orders.Where(o => convertedOrderIds.Contains(o.OrderId)).ToDictionaryAsync(o => o.OrderId);
        }

        return reservations.Select(r =>
        {
            var depositRatio = r.Campaign?.DepositRatio ?? 0.3m;
            if (depositRatio <= 0) depositRatio = 0.3m;
            var total = r.UnitPrice * r.ReservedQuantity;
            var deposit = Math.Round(total * depositRatio);
            
            var displayStatus = r.Status;
            int? linkedOrderId = r.ConvertedOrderId;
            if (linkedOrderId.HasValue && orders.TryGetValue(linkedOrderId.Value, out var order))
            {
                var os = order.OrderStatus?.ToLower() ?? "";
                if (os == "packed" || os == "dispatched") displayStatus = "packed";
                else if (os == "shipped" || os == "delivering") displayStatus = "shipping";
                else if (os == "delivered") displayStatus = "delivered";
                else if (os == "completed") displayStatus = "fulfilled";
                else if (os == "cancelled") displayStatus = "cancelled";
            }

            return new CustomerReservationDto
            {
                ReservationId = r.ReservationId,
                ReservationCode = r.ReservationCode,
                ProductName = r.Variant?.Product?.ProductName ?? "Sản phẩm Pre-order",
                VariantSku = r.Variant?.Sku,
                Color = r.Variant?.Color,
                Size = r.Variant?.Size,
                Quantity = r.ReservedQuantity,
                UnitPrice = r.UnitPrice,
                TotalAmount = total,
                DepositAmount = deposit,
                RemainingAmount = total - deposit,
                Status = displayStatus,
                PaidAt = r.PaidAt,
                CreatedAt = r.CreatedAt,
                ShippingAddress = r.ShippingAddress,
                ConvertedOrderId = linkedOrderId
            };
        }).ToList();
    }

    public async Task<PayOSLinkResponseDto> CreateFinalPaymentLinkAsync(int reservationId, int customerId)
    {
        var reservation = await _context.PreOrderReservations
            .Include(r => r.Campaign)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đặt chỗ Pre-order.");

        if (reservation.Status.ToLower() != "customer_notified")
            throw new InvalidOperationException($"Chưa đến lúc thanh toán số tiền còn lại (Trạng thái: {reservation.Status}).");

        var depositRatio = reservation.Campaign?.DepositRatio ?? 0.3m;
        if (depositRatio <= 0) depositRatio = 0.3m;

        var totalAmount = reservation.UnitPrice * reservation.ReservedQuantity;
        var remainingAmount = (int)Math.Round(totalAmount * (1 - depositRatio));

        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long orderCode = long.Parse($"9{reservation.ReservationId}{currentTimestamp % 10000}");

        var domain = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:5173/payment/success";
        var cancelDomain = _configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel";

        var paymentData = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = remainingAmount,
            Description = $"TT PO {reservation.ReservationId}".Substring(0, Math.Min($"TT PO {reservation.ReservationId}".Length, 25)),
            CancelUrl = $"{cancelDomain}?reservationId={reservation.ReservationId}&type=final",
            ReturnUrl = $"{domain}?reservationId={reservation.ReservationId}&type=final"
        };

        var createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);

        reservation.PaymentLinkId = createPayment.PaymentLinkId;
        await _context.SaveChangesAsync();

        return new PayOSLinkResponseDto
        {
            PaymentLinkId = createPayment.PaymentLinkId,
            CheckoutUrl = createPayment.CheckoutUrl
        };
    }

    /// <summary>
    /// Kiểm tra trạng thái thanh toán 70% còn lại từ PayOS
    /// </summary>
    public async Task<object> CheckFinalPaymentStatusAsync(int reservationId, string paymentLinkId, int customerId)
    {
        var reservation = await _context.PreOrderReservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đặt chỗ Pre-order.");

        if (reservation.Status.ToLower() == "paid")
        {
            return new
            {
                OrderId = reservation.ReservationId,
                PaymentLinkId = paymentLinkId,
                IsPaid = true,
                Status = "FinalPaid"
            };
        }

        if (reservation.PaymentLinkId != paymentLinkId)
            throw new InvalidOperationException("PaymentLinkId không khớp.");

        var paymentInfo = await _payOS.PaymentRequests.GetAsync(paymentLinkId);

        if (paymentInfo.Status.ToString().Equals("PAID", StringComparison.OrdinalIgnoreCase))
        {
            // Transition from customer_notified to paid
            reservation.Status = VisionCare.BusinessLogicLayer.Constants.PreOrderStatuses.Paid;
            await _context.SaveChangesAsync();

            return new
            {
                OrderId = reservation.ReservationId,
                PaymentLinkId = paymentLinkId,
                IsPaid = true,
                Status = "FinalPaid"
            };
        }

        return new
        {
            OrderId = reservation.ReservationId,
            PaymentLinkId = paymentLinkId,
            IsPaid = false,
            Status = paymentInfo.Status.ToString()
        };
    }

    /// <summary>
    /// Giả lập thanh toán 70% còn lại cho mục đích demo
    /// </summary>
    public async Task<bool> SimulateFinalPaymentAsync(int reservationId, int customerId)
    {
        var reservation = await _context.PreOrderReservations
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.CustomerId == customerId);

        if (reservation == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đặt chỗ Pre-order.");

        if (reservation.Status.ToLower() != "customer_notified" && reservation.Status.ToLower() != "paid")
            throw new InvalidOperationException($"Trạng thái không hợp lệ để giả lập thanh toán: {reservation.Status}");

        // Mark as fully paid — Sales sẽ tiếp tục xử lý giao hàng
        reservation.Status = VisionCare.BusinessLogicLayer.Constants.PreOrderStatuses.Paid;
        await _context.SaveChangesAsync();

        return true;
    }
}
