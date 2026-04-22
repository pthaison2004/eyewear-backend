using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1/sales/pre-orders")]
[ApiController]
[Authorize(Roles = "Sales,Staff,Manager,Admin")]
public class SalesPreOrderController : ControllerBase
{
    private readonly ISalesPreOrderService _salesPreOrderService;
    private readonly ILogger<SalesPreOrderController> _logger;

    public SalesPreOrderController(ISalesPreOrderService salesPreOrderService, ILogger<SalesPreOrderController> logger)
    {
        _salesPreOrderService = salesPreOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách phiếu đặt trước (Reservations) cho Sales Staff
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReservations([FromQuery] string? statusFilter)
    {
        try
        {
            var result = await _salesPreOrderService.GetReservationsAsync(statusFilter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservations");
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy danh sách đặt chỗ." });
        }
    }

    /// <summary>
    /// Bước 1: Sales gọi xác nhận thông tin với khách hàng
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmFirstCall(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var success = await _salesPreOrderService.ConfirmFirstCallAsync(id, staffId);
            if (!success) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ." });
            return Ok(new { message = "Xác nhận cuộc gọi lần 1 thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming first call for reservation {Id}", id);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }

    /// <summary>
    /// Bước 2: Chuyển yêu cầu chuẩn bị hàng cho bộ phận Ops
    /// </summary>
    [HttpPost("{id}/send-to-ops")]
    public async Task<IActionResult> SendToOps(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var success = await _salesPreOrderService.SendToOpsAsync(id, staffId);
            if (!success) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ." });
            return Ok(new { message = "Đã chuyển yêu cầu sang bộ phận Ops." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reservation {Id} to Ops", id);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }

    /// <summary>
    /// Bước 3: Sales gọi báo khách hàng có hàng và nhắc thanh toán 70% còn lại
    /// </summary>
    [HttpPost("{id}/notify-stock")]
    public async Task<IActionResult> NotifyStockReady(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var success = await _salesPreOrderService.NotifyStockReadyAsync(id, staffId);
            if (!success) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ." });
            return Ok(new { message = "Đã xác nhận báo khách hàng có hàng sẵn sàng." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying customer for reservation {Id}", id);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }

    /// <summary>
    /// Bước cuối: Sales chuyển đơn cho Ops đóng gói và ship sau khi đã thanh toán đủ
    /// </summary>
    [HttpPost("{id}/release")]
    public async Task<IActionResult> ReleaseToShipping(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var success = await _salesPreOrderService.ReleaseToShippingAsync(id, staffId);
            if (!success) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ." });
            return Ok(new { message = "Đã giải phóng đơn hàng sang bộ phận Vận chuyển." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reservation {Id} to shipping", id);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn đặt giữ chỗ
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReservationDetail(int id)
    {
        var result = await _salesPreOrderService.GetReservationDetailAsync(id);
        if (result == null) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ." });
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
