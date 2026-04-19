using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs.Supplier;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/suppliers")]
[ApiController]
[Authorize(Roles = "Admin,Manager,Operations")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Lấy danh sách tất cả nhà cung cấp
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var suppliers = await _supplierService.GetAllAsync();
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách nhà cung cấp đang hoạt động (cho dropdown)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var suppliers = await _supplierService.GetActiveAsync();
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết nhà cung cấp theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} not found." });
            }
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo mới nhà cung cấp
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SupplierName))
            {
                return BadRequest(new { message = "SupplierName is required." });
            }

            var supplier = await _supplierService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = supplier.SupplierId }, supplier);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật nhà cung cấp
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequestDto request)
    {
        try
        {
            var supplier = await _supplierService.UpdateAsync(id, request);
            if (supplier == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} not found." });
            }
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xoa nha cung cap (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _supplierService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Supplier with ID {id} not found." });
            }
            return Ok(new { message = "Supplier deactivated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
