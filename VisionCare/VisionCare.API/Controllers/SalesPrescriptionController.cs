using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisionCare.DataAccessLayer.Models;
using VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1/sales")]
[ApiController]
[Authorize(Roles = "Sales,Manager,Admin")]
public class SalesPrescriptionController : ControllerBase
{
    private readonly ISalesPrescriptionService _salesPrescriptionService;
    private readonly VisionCareContext _context;
    private readonly ILogger<SalesPrescriptionController> _logger;

    public SalesPrescriptionController(
        ISalesPrescriptionService salesPrescriptionService,
        VisionCareContext context,
        ILogger<SalesPrescriptionController> logger)
    {
        _salesPrescriptionService = salesPrescriptionService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách đơn hàng kê đơn
    /// </summary>
    [HttpGet("orders/prescription")]
    public async Task<IActionResult> GetPrescriptionOrders(
        [FromQuery] string? search,
        [FromQuery] int? orderStatusFilter)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPrescriptionService.GetPrescriptionOrdersAsync(search, orderStatusFilter);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPrescriptionOrders");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Xem chi tiết đơn kê đơn
    /// </summary>
    [HttpGet("orders/{orderId}/prescription-review")]
    public async Task<IActionResult> GetPrescriptionReview(int orderId)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var prescriptionId = await GetPrescriptionIdFromOrderAsync(orderId);
            var result = await _salesPrescriptionService.GetPrescriptionReviewAsync(prescriptionId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPrescriptionReview");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Xác minh hoặc từ chối đơn kê đơn
    /// </summary>
    [HttpPut("orders/{orderId}/verify-prescription")]
    public async Task<IActionResult> VerifyPrescription(int orderId, [FromBody] VerifyPrescriptionRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var prescriptionId = await GetPrescriptionIdFromOrderAsync(orderId);
            var result = await _salesPrescriptionService.VerifyPrescriptionAsync(prescriptionId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "VerifyPrescription");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Liên hệ khách hàng để điều chỉnh đơn kê đơn
    /// </summary>
    [HttpPut("orders/{orderId}/prescription/adjust")]
    public async Task<IActionResult> AdjustPrescription(int orderId, [FromBody] AdjustPrescriptionRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var prescriptionId = await GetPrescriptionIdFromOrderAsync(orderId);
            var result = await _salesPrescriptionService.AdjustPrescriptionAsync(prescriptionId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "AdjustPrescription");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    private async Task<int> GetPrescriptionIdFromOrderAsync(int orderId)
    {
        var orderItem = await _context.OrderItems
            .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.PrescriptionId != null);

        if (orderItem == null)
            throw new KeyNotFoundException($"Order {orderId} does not have a linked prescription.");

        return orderItem.PrescriptionId!.Value;
    }
}