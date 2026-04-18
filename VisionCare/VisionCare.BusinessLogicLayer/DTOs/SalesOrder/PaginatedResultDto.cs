namespace VisionCare.BusinessLogicLayer.DTOs.SalesOrder;

public class PaginatedResultDto<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public PaginationMeta Meta { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
