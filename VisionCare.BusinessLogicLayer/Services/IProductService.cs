using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Product;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IProductService
{
    Task<(List<ProductResponseDto> Products, int TotalCount)> GetProductsAsync(GetProductsRequestDto request);
    Task<ProductDetailDto?> GetProductByIdAsync(int id);
    Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request);
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductRequestDto request);
    Task<bool> RestockAsync(RestockRequestDto request);
}
