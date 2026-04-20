using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.API.Controllers;

[Route("api/v1/sales/pre-orders")]
[ApiController]
[Authorize(Roles = "Sales,Manager,Admin")]
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
    /// Lấy danh sách chiến dịch pre-order
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPreOrderCampaigns([FromQuery] string? statusFilter)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPreOrderService.GetPreOrderCampaignsAsync(statusFilter);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPreOrderCampaigns");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Xem chi tiết trạng thái chiến dịch pre-order
    /// </summary>
    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetPreOrderStatus(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPreOrderService.GetPreOrderStatusAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetPreOrderStatus");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Thực hiện fulfill pre-order
    /// </summary>
    [HttpPut("{id}/fulfill")]
    public async Task<IActionResult> FulfillPreOrder(int id, [FromBody] FulfillPreOrderRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPreOrderService.FulfillPreOrderAsync(id, staffId, request);
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
    /// Tạo chiến dịch pre-order mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreatePreOrderCampaignRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPreOrderService.CreateCampaignAsync(request, staffId);
            return CreatedAtAction(nameof(GetCampaignDetail), new { id = result.CampaignId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "CreateCampaign");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Cập nhật chiến dịch pre-order
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCampaign(int id, [FromBody] UpdatePreOrderCampaignRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPreOrderService.UpdateCampaignAsync(id, request, staffId);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy chiến dịch với ID: {id}" });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "UpdateCampaign");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Lấy chi tiết chiến dịch pre-order
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCampaignDetail(int id)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _salesPreOrderService.GetCampaignDetailAsync(id);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy chiến dịch với ID: {id}" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetCampaignDetail");
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
