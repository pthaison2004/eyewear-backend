using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs.Cart;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Lấy giỏ hàng hiện tại của khách hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var cart = await _cartService.GetCartAsync(customerId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng
    /// </summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequestDto request)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var cart = await _cartService.AddItemAsync(customerId, request);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thêm Gọng + Tròng + Toa kính vào giỏ hàng
    /// </summary>
    [HttpPost("combo")]
    public async Task<IActionResult> AddCombo([FromBody] AddCartComboRequestDto request)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var cart = await _cartService.AddComboAsync(customerId, request);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm trong giỏ hàng
    /// </summary>
    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateCartItemRequestDto request)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var cart = await _cartService.UpdateItemAsync(id, customerId, request);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng
    /// </summary>
    [HttpDelete("items/{id}")]
    public async Task<IActionResult> RemoveItem(int id)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var cart = await _cartService.RemoveItemAsync(id, customerId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            await _cartService.ClearCartAsync(customerId);
            return Ok(new { message = "Giỏ hàng đã được xóa." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}