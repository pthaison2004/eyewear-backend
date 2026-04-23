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
        var today = DateTime.UtcNow.Date;
        
        var query = _context.Orders.Where(o => o.OrderDate.HasValue && o.PaymentStatus == "Paid");

        if (request.DateFrom.HasValue)
        {
            var startDate = request.DateFrom.Value.Date;
            query = query.Where(o => o.OrderDate >= startDate);
        }
        if (request.DateTo.HasValue)
        {
            var endDate = request.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= endDate);
        }

        var allOrders = await query
            .Select(o => new { o.OrderDate, o.TotalAmount, o.OrderStatus, o.OrderType, o.PaymentStatus })
            .ToListAsync();

        var dailyRevenue = allOrders.Where(o => o.OrderDate.Value.Date == today).Sum(o => o.TotalAmount);
        var weeklyRevenue = allOrders.Where(o => o.OrderDate.Value.Date >= today.AddDays(-7)).Sum(o => o.TotalAmount);
        var monthlyRevenue = allOrders.Where(o => o.OrderDate.Value.Month == today.Month && o.OrderDate.Value.Year == today.Year).Sum(o => o.TotalAmount);
        var yearlyRevenue = allOrders.Where(o => o.OrderDate.Value.Year == today.Year).Sum(o => o.TotalAmount);

        var chartData = new List<ChartDataDto>();
        var period = request.Period?.ToLower() ?? "daily";

        if (period == "daily")
        {
            // Last 14 days
            for (int i = 13; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                chartData.Add(new ChartDataDto
                {
                    Name = date.ToString("dd/MM"),
                    Value = allOrders.Where(o => o.OrderDate.Value.Date == date).Sum(o => o.TotalAmount) / 1000000m
                });
            }
        }
        else if (period == "weekly")
        {
            // Last 4 weeks
            for (int i = 3; i >= 0; i--)
            {
                var start = today.AddDays(-((int)today.DayOfWeek + (i * 7)));
                var end = start.AddDays(6).AddDays(1).AddTicks(-1);
                chartData.Add(new ChartDataDto
                {
                    Name = $"Tuần {i + 1}",
                    Value = allOrders.Where(o => o.OrderDate.Value.Date >= start && o.OrderDate.Value.Date <= end).Sum(o => o.TotalAmount) / 1000000m
                });
            }
        }
        else if (period == "monthly")
        {
            // Months of current year
            for (int i = 1; i <= 12; i++)
            {
                chartData.Add(new ChartDataDto
                {
                    Name = $"Tháng {i}",
                    Value = allOrders.Where(o => o.OrderDate.Value.Month == i && o.OrderDate.Value.Year == today.Year).Sum(o => o.TotalAmount) / 1000000m
                });
            }
        }
        else if (period == "custom" && request.DateFrom.HasValue && request.DateTo.HasValue)
        {
            // Daily for the range
            var start = request.DateFrom.Value.Date;
            var end = request.DateTo.Value.Date;
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                chartData.Add(new ChartDataDto
                {
                    Name = d.ToString("dd/MM"),
                    Value = allOrders.Where(o => o.OrderDate.Value.Date == d).Sum(o => o.TotalAmount) / 1000000m
                });
            }
        }

        var results = allOrders;

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
            OrdersToday = dailyRevenue > 0 ? allOrders.Count(o => o.OrderDate.Value.Date == today) : 0,
            RevenueToday = dailyRevenue,
            DailyRevenue = dailyRevenue,
            WeeklyRevenue = weeklyRevenue,
            MonthlyRevenue = monthlyRevenue,
            YearlyRevenue = yearlyRevenue,
            ChartData = chartData
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
        {
            var startDate = dateFrom.Value.Date;
            query = query.Where(o => o.OrderDate >= startDate);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= endDate);
        }

        return query;
    }
}