using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1/sales/complaints")]
[ApiController]
[Authorize(Roles = "Sales,Manager,Admin")]
public class SalesComplaintController : ControllerBase
{
    private readonly ISalesComplaintService _salesComplaintService;
    private readonly ILogger<SalesComplaintController> _logger;

    public SalesComplaintController(ISalesComplaintService salesComplaintService, ILogger<SalesComplaintController> logger)
    {
        _salesComplaintService = salesComplaintService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách khiếu nại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] string? statusFilter,
        [FromQuery] string? priorityFilter)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesComplaintService.GetComplaintsAsync(statusFilter, priorityFilter);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetComplaints");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Tạo khiếu nại cho đơn hàng
    /// </summary>
    [HttpPost("{orderId}")]
    public async Task<IActionResult> CreateComplaint(int orderId, [FromBody] CreateComplaintRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesComplaintService.CreateComplaintAsync(orderId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "CreateComplaint");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Xử lý khiếu nại
    /// </summary>
    [HttpPut("{complaintId}/process")]
    public async Task<IActionResult> ProcessComplaint(int complaintId, [FromBody] ProcessComplaintRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesComplaintService.ProcessComplaintAsync(complaintId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "ProcessComplaint");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Giải quyết khiếu nại
    /// </summary>
    [HttpPut("{complaintId}/resolve")]
    public async Task<IActionResult> ResolveComplaint(int complaintId, [FromBody] ResolveComplaintRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesComplaintService.ResolveComplaintAsync(complaintId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "ResolveComplaint");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
