namespace VisionCare.BusinessLogicLayer.DTOs.SalesReport;

public class SalesReportDto
{
    public SalesSummaryDto Summary { get; set; } = new();
    public List<OrderStatusCountDto> ByStatus { get; set; } = new();
    public List<OrderTypeCountDto> ByOrderType { get; set; } = new();
    public DateRangeDto? DateRange { get; set; }
}

public class OrderStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OrderTypeCountDto
{
    public string OrderType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DateRangeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}