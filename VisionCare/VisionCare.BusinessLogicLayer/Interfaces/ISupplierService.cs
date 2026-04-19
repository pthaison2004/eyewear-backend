using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Supplier;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(int id);
    Task<SupplierDto> CreateAsync(CreateSupplierRequestDto request);
    Task<SupplierDto?> UpdateAsync(int id, UpdateSupplierRequestDto request);
    Task<bool> DeleteAsync(int id);
    Task<List<SupplierDto>> GetActiveAsync();
}
