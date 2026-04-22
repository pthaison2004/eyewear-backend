using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs.ManagerProduct;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1/manager/products")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class ManagerProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ManagerProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateManagerProductDto dto)
    {
        try
        {
            var product = await _productService.CreateManagerProductAsync(dto);
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateManagerProductDto dto)
    {
        try
        {
            var product = await _productService.UpdateManagerProductAsync(id, dto);
            if (product == null) return NotFound(new { message = $"Product {id} not found" });
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _productService.DeleteManagerProductAsync(id);
            if (!success) return NotFound(new { message = $"Product {id} not found" });
            return Ok(new { message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
