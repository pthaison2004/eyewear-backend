using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.Shipping;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/ops")]
[ApiController]
[Authorize]
public class OpsShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;
    private readonly ILogger<OpsShippingController> _logger;

    public OpsShippingController(IShippingService shippingService, ILogger<OpsShippingController> logger)
    {
        _shippingService = shippingService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    /// <summary>
    /// Lấy danh sách phương thức vận chuyển khả dụng
    /// </summary>
    [HttpGet("shipping/methods")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetShippingMethods()
    {
        try
        {
            var methods = await _shippingService.GetAvailableShippingMethodsAsync();
            return Ok(methods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetShippingMethods");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Tạo đơn vận chuyển cho đơn hàng
    /// </summary>
    [HttpPost("orders/{id}/shipping")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> CreateShippingOrder(int id, [FromBody] CreateShippingOrderRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _shippingService.CreateShippingOrderAsync(id, staffId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "CreateShippingOrder");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Đánh dấu đơn hàng đã được giao cho đơn vị vận chuyển
    /// </summary>
    [HttpPut("orders/{id}/ship")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> MarkAsShipped(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _shippingService.MarkOrderAsShippedAsync(id, staffId);
            if (result == null)
                return NotFound(new { message = $"No shipping order found for order ID {id}." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "MarkAsShipped");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }
}
