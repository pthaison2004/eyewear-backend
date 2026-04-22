using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs.Product;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm với bộ lọc và phân trang
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsRequestDto request)
    {
        try
        {
            var (products, totalCount) = await _productService.GetProductsAsync(request);
            return Ok(new
            {
                data = products,
                totalCount,
                page = request.Page,
                pageSize = request.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
