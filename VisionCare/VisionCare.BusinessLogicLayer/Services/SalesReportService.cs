using Microsoft.EntityFrameworkCore;
using VisionCare.BusinessLogicLayer.DTOs.SalesReport;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SalesReportService : ISalesReportService
{
    private readonly VisionCareContext _context;

    public SalesReportService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(SalesSummaryRequestDto request)
    {
        var query = BuildBaseQuery(request.DateFrom, request.DateTo);

        var today = DateTime.UtcNow.Date;
        var todayQuery = _context.Orders.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == today);

        var results = await query.Select(o => new
        {
            o.OrderStatus,
            o.PaymentStatus,
            o.OrderType,
            o.TotalAmount
        }).ToListAsync();

        var todayResults = await todayQuery.Select(o => new
        {
            o.TotalAmount
        }).ToListAsync();

        var summary = new SalesSummaryDto
        {
            TotalOrders = results.Count,
            TotalRevenue = results.Sum(r => r.TotalAmount),
            AvgOrderValue = results.Count == 0 ? 0 : results.Average(r => r.TotalAmount),
            ReadyMadeCount = results.Count(r => string.Equals(r.OrderType, "Ready-made", StringComparison.OrdinalIgnoreCase)),
            PrescriptionCount = results.Count(r => string.Equals(r.OrderType, "Prescription", StringComparison.OrdinalIgnoreCase)),
            PreOrderCount = results.Count(r => string.Equals(r.OrderType, "Pre-order", StringComparison.OrdinalIgnoreCase)),
            PendingCount = results.Count(r => string.Equals(r.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase)),
            ConfirmedCount = results.Count(r => string.Equals(r.OrderStatus, "Confirmed", StringComparison.OrdinalIgnoreCase)),
            ProcessingCount = results.Count(r => string.Equals(r.OrderStatus, "Processing", StringComparison.OrdinalIgnoreCase)),
            CancelledCount = results.Count(r => string.Equals(r.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)),
            DeliveredCount = results.Count(r => string.Equals(r.OrderStatus, "Delivered", StringComparison.OrdinalIgnoreCase)),
            PaidCount = results.Count(r => string.Equals(r.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)),
            UnpaidCount = results.Count(r => string.Equals(r.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(r.PaymentStatus, "Unpaid", StringComparison.OrdinalIgnoreCase)),
            OrdersToday = todayResults.Count,
            RevenueToday = todayResults.Sum(r => r.TotalAmount)
        };

        return summary;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(SalesReportRequestDto request)
    {
        var query = BuildBaseQuery(request.DateFrom, request.DateTo);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(o => o.OrderStatus != null && o.OrderStatus.ToLower() == request.Status.ToLower());

        if (!string.IsNullOrWhiteSpace(request.OrderType))
            query = query.Where(o => o.OrderType != null && o.OrderType.ToLower() == request.OrderType.ToLower());

        var filteredResults = await query.ToListAsync();

        var summary = new SalesSummaryDto
        {
            TotalOrders = filteredResults.Count,
            TotalRevenue = filteredResults.Sum(r => r.TotalAmount),
            AvgOrderValue = filteredResults.Count == 0 ? 0 : filteredResults.Average(r => r.TotalAmount),
            ReadyMadeCount = filteredResults.Count(r => string.Equals(r.OrderType, "Ready-made", StringComparison.OrdinalIgnoreCase)),
            PrescriptionCount = filteredResults.Count(r => string.Equals(r.OrderType, "Prescription", StringComparison.OrdinalIgnoreCase)),
            PreOrderCount = filteredResults.Count(r => string.Equals(r.OrderType, "Pre-order", StringComparison.OrdinalIgnoreCase)),
            PendingCount = filteredResults.Count(r => string.Equals(r.OrderStatus, "Pending", StringComparison.OrdinalIgnoreCase)),
            ConfirmedCount = filteredResults.Count(r => string.Equals(r.OrderStatus, "Confirmed", StringComparison.OrdinalIgnoreCase)),
            ProcessingCount = filteredResults.Count(r => string.Equals(r.OrderStatus, "Processing", StringComparison.OrdinalIgnoreCase)),
            CancelledCount = filteredResults.Count(r => string.Equals(r.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)),
            DeliveredCount = filteredResults.Count(r => string.Equals(r.OrderStatus, "Delivered", StringComparison.OrdinalIgnoreCase)),
            PaidCount = filteredResults.Count(r => string.Equals(r.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)),
            UnpaidCount = filteredResults.Count(r => string.Equals(r.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(r.PaymentStatus, "Unpaid", StringComparison.OrdinalIgnoreCase)),
            OrdersToday = 0,
            RevenueToday = 0
        };

        var byStatus = filteredResults
            .GroupBy(r => r.OrderStatus ?? "Unknown")
            .Select(g => new OrderStatusCountDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byOrderType = filteredResults
            .GroupBy(r => r.OrderType ?? "Unknown")
            .Select(g => new OrderTypeCountDto
            {
                OrderType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var dateRange = new DateRangeDto();
        if (request.DateFrom.HasValue)
            dateRange.From = request.DateFrom.Value.ToString("yyyy-MM-dd");
        if (request.DateTo.HasValue)
            dateRange.To = request.DateTo.Value.ToString("yyyy-MM-dd");

        return new SalesReportDto
        {
            Summary = summary,
            ByStatus = byStatus,
            ByOrderType = byOrderType,
            DateRange = dateRange
        };
    }

    private IQueryable<Order> BuildBaseQuery(DateTime? dateFrom, DateTime? dateTo)
    {
        var query = _context.Orders.AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(o => o.OrderDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(o => o.OrderDate <= dateTo.Value);

        return query;
    }
}