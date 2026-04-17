using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1/ops/orders")]
[ApiController]
[Authorize]
public class OpsOrderController : ControllerBase
{
    private readonly IOpsOrderService _opsOrderService;
    private readonly ILogger<OpsOrderController> _logger;

    public OpsOrderController(IOpsOrderService opsOrderService, ILogger<OpsOrderController> logger)
    {
        _opsOrderService = opsOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Đánh dấu đơn hàng đã được đóng gói
    /// </summary>
    [HttpPut("{id}/pack")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> PackOrder(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _opsOrderService.PackOrderAsync(id, staffId);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "PackOrder");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng thủ công
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _opsOrderService.UpdateOrderStatusAsync(id, staffId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "UpdateOrderStatus");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
