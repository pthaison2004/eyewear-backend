using System;
using System.Collections.Generic;
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

        var depositRatio = reservation.Campaign?.DepositRatio ?? 0m;
        if (depositRatio <= 0)
            throw new InvalidOperationException("Chiến dịch này chưa được thiết lập tỉ lệ cọc. Vui lòng liên hệ hỗ trợ.");

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

        if (reservation.PaymentLinkId != paymentLinkId)
            throw new InvalidOperationException("PaymentLinkId không khớp.");

        var paymentInfo = await _payOS.PaymentRequests.GetAsync(paymentLinkId);

        if (paymentInfo.Status.ToString().Equals("PAID", StringComparison.OrdinalIgnoreCase))
        {
            if (reservation.Status == "reserved")
            {
                reservation.Status = "paid";
                reservation.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new 
            {
                OrderId = reservation.ReservationId,
                PaymentLinkId = paymentLinkId,
                IsPaid = true,
                Status = "Paid" // Front-end mapping
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
}
