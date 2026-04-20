namespace VisionCare.BusinessLogicLayer.DTOs.SalesReport;

public class SalesSummaryDto
{
    // Totals
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }

    // By Order Type
    public int ReadyMadeCount { get; set; }
    public int PrescriptionCount { get; set; }
    public int PreOrderCount { get; set; }

    // By Order Status
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int CancelledCount { get; set; }
    public int DeliveredCount { get; set; }

    // By Payment Status
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }

    // Today's stats
    public int OrdersToday { get; set; }
    public decimal RevenueToday { get; set; }
}