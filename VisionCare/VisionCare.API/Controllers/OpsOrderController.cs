using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.OpsOrder;
using VisionCare.BusinessLogicLayer.DTOs.OpsPreOrder;
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

    /// <summary>
    /// Lấy danh sách tất cả đơn hàng Ops cần xử lý
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetOrders([FromQuery] OpsOrderListRequestDto request)
    {
        try
        {
            var result = await _opsOrderService.GetOrdersAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetOrders");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng Ops
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetOrderDetail(int id, [FromQuery] bool isPreOrder = false)
    {
        try
        {
            var result = await _opsOrderService.GetOrderDetailAsync(id, isPreOrder);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetOrderDetail");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Lọc đơn hàng Ready-made
    /// </summary>
    [HttpGet("ready-made")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetReadyMadeOrders([FromQuery] OpsOrderListRequestDto request)
    {
        try
        {
            var result = await _opsOrderService.GetReadyMadeOrdersAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetReadyMadeOrders");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Lọc đơn hàng Prescription
    /// </summary>
    [HttpGet("prescription")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetPrescriptionOrders([FromQuery] OpsOrderListRequestDto request)
    {
        try
        {
            var result = await _opsOrderService.GetPrescriptionOrdersAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPrescriptionOrders");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Lọc đơn hàng Pre-order (từ PreOrderReservations)
    /// </summary>
    [HttpGet("pre-order")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetPreOrderOrders([FromQuery] OpsOrderListRequestDto request)
    {
        try
        {
            var result = await _opsOrderService.GetPreOrderOrdersAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPreOrderOrders");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn pre-order reservation cho Ops
    /// </summary>
    [HttpGet("pre-order/reservations/{id:int}")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetPreOrderReservationDetail(int id)
    {
        try
        {
            var result = await _opsOrderService.GetPreOrderReservationDetailAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPreOrderReservationDetail");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    /// <summary>
    /// Lấy chi tiết lens work của đơn hàng prescription
    /// </summary>
    [HttpGet("{id}/lens-work")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetLensWork(int id)
    {
        try
        {
            var result = await _opsOrderService.GetLensWorkAsync(id);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "GetLensWork");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Gán nhân viên cắt kính cho đơn hàng prescription
    /// </summary>
    [HttpPut("{id}/lens-work/assign")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> AssignLensWork(int id, [FromBody] AssignLensWorkRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _opsOrderService.AssignLensWorkAsync(id, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "AssignLensWork");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Hoàn thành cắt kính cho đơn hàng prescription
    /// </summary>
    [HttpPut("{id}/lens-work/complete")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> CompleteLensWork(int id, [FromBody] CompleteLensWorkRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _opsOrderService.CompleteLensWorkAsync(id, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "CompleteLensWork");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    // ─── Pre-Order Receive / Fulfill ───────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách đơn pre-order đang chờ nhận hàng về kho
    /// </summary>
    [HttpGet("pre-order/receive")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetPreOrderReceiveList(
        [FromQuery] string? status = "active",
        [FromQuery] int? campaignId = null)
    {
        try
        {
            var result = await _opsOrderService.GetPreOrderReceiveListAsync(status, campaignId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPreOrderReceiveList");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Xác nhận đã nhận hàng pre-order về kho
    /// </summary>
    [HttpPut("pre-order/{id}/receive")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> ReceivePreOrder(int id, [FromBody] ReceivePreOrderRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _opsOrderService.ReceivePreOrderAsync(id, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "ReceivePreOrder");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Xử lý giao hàng pre-order sau khi đã nhận (chuyển tiếp sang pack + ship)
    /// </summary>
    [HttpPut("pre-order/{id}/fulfill")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> FulfillPreOrder(int id, [FromBody] OpsFulfillPreOrderRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _opsOrderService.FulfillPreOrderAsync(id, staffId, request);
            if (result == null)
                return NotFound(new { message = "No converted orders found for fulfillment." });
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
            _logger.LogError(ex, "Unhandled error in {Method}", "FulfillPreOrder");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Bước 2.1: Ops báo đã có hàng về cho đơn đặt trước cụ thể
    /// </summary>
    [HttpPut("pre-order/reservations/{id}/stock-arrived")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> MarkPreOrderStockArrived(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var success = await _opsOrderService.MarkPreOrderStockArrivedAsync(id, staffId);
            if (!success) return NotFound(new { message = "Không tìm thấy phiếu đặt chỗ hoặc trạng thái không hợp lệ." });
            return Ok(new { message = "Đã báo có hàng về kho thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking stock arrived for reservation {Id}", id);
            return StatusCode(500, new { message = "Lỗi hệ thống." });
        }
    }
}
