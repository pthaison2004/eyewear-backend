using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.Shipping;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/ops/shipping")]
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
    [HttpGet("methods")]
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
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Tao don van chuyen cho don hang
    /// </summary>
    [HttpPost("orders/{id}/shipping")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> CreateShippingOrder(int id, [FromBody] CreateShippingOrderRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

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
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Danh dau don hang da duoc giao cho don vi van chuyen
    /// </summary>
    [HttpPut("orders/{id}/ship")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> MarkAsShipped(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

            var result = await _shippingService.MarkOrderAsShippedAsync(id, staffId);
            if (result == null)
                return NotFound(new { message = $"No shipping order found for order ID {id}." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "MarkAsShipped");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Lay danh sach trang thai van chuyen
    /// </summary>
    [HttpGet("statuses")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetShippingStatuses()
    {
        try
        {
            var statuses = await _shippingService.GetShippingStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetShippingStatuses");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Cap nhat trang thai van chuyen
    /// </summary>
    [HttpPut("orders/{id}/status")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> UpdateShippingStatus(int id, [FromBody] UpdateShippingStatusRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

            var result = await _shippingService.UpdateShippingStatusAsync(id, staffId, request);
            if (result == null)
                return NotFound(new { message = $"No shipping order found with ID {id}." });

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
            _logger.LogError(ex, "Unhandled error in {Method}", "UpdateShippingStatus");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Lay lich su trang thai van chuyen
    /// </summary>
    [HttpGet("orders/{id}/history")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetShippingHistory(int id)
    {
        try
        {
            var history = await _shippingService.GetShippingHistoryAsync(id);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetShippingHistory");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Theo doi van don bang ma tracking
    /// </summary>
    [HttpGet("tracking/{trackingNo}")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> TrackShipping(string trackingNo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(trackingNo))
                return BadRequest(new { message = "Tracking number is required." });

            var result = await _shippingService.TrackShippingAsync(trackingNo);
            if (result == null)
                return NotFound(new { message = $"No shipping order found for tracking number '{trackingNo}'." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "TrackShipping");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau." });
        }
    }

    /// <summary>
    /// Xác nhận giao hàng thành công (cập nhật cả ShippingOrder + Order về Delivered)
    /// </summary>
    [HttpPut("orders/{id}/delivered")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> MarkAsDelivered(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _shippingService.MarkAsDeliveredAsync(id, staffId);
            if (result == null)
                return NotFound(new { message = "No shipping order found for this order." });
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
            _logger.LogError(ex, "Unhandled error in MarkAsDelivered");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }
}
