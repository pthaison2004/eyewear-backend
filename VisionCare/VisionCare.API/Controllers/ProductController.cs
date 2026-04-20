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

    /// <summary>
    /// Thêm sản phẩm mới (Chỉ dành cho Admin/Manager)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request)
    {
        try
        {
            var product = await _productService.CreateProductAsync(request);
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin sản phẩm (Admin/Manager)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequestDto request)
    {
        try
        {
            var product = await _productService.UpdateProductAsync(id, request);
            if (product == null) return NotFound(new { message = "Sản phẩm không tồn tại." });
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Nhập hàng - Cập nhật số lượng tồn kho (Admin/Manager)
    /// </summary>
    [HttpPatch("restock")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Restock([FromBody] RestockRequestDto request)
    {
        try
        {
            var result = await _productService.RestockAsync(request);
            if (!result) return BadRequest(new { message = "Không tìm thấy biến thể nào để cập nhật." });
            return Ok(new { message = "Cập nhật tồn kho thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
