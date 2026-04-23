using System.Collections.Generic;
using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface IBrandService
{
    Task<IEnumerable<BrandDto>> GetApprovedBrandsAsync();
    Task<IEnumerable<BrandDto>> GetPendingRequestsAsync();
    Task<BrandDto> RequestAddBrandAsync(CreateBrandRequestDto request, int userId, string role);
    Task<BrandDto> RequestDeleteBrandAsync(int brandId, DeleteBrandRequestDto request, int userId);
    Task<bool> ProcessBrandRequestAsync(int brandId, BrandActionDto action, int managerId);
}
