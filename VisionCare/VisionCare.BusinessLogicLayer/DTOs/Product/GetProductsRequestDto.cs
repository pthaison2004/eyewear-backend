namespace VisionCare.BusinessLogicLayer.DTOs.Product;

public class GetProductsRequestDto
{
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public bool? IsPreOrder { get; set; }
    public string? SortBy { get; set; } = "newest";
    public string? SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
