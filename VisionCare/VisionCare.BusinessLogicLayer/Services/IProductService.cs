using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Product;

using VisionCare.BusinessLogicLayer.DTOs.ManagerProduct;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IProductService
{
    Task<(List<ProductResponseDto> Products, int TotalCount)> GetProductsAsync(GetProductsRequestDto request);
    Task<ProductDetailDto?> GetProductByIdAsync(int id);
    
    // Manager CRUD
    Task<ProductDetailDto> CreateManagerProductAsync(CreateManagerProductDto dto);
    Task<ProductDetailDto?> UpdateManagerProductAsync(int id, UpdateManagerProductDto dto);
    Task<bool> DeleteManagerProductAsync(int id);
}
