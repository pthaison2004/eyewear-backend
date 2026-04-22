using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VisionCare.BusinessLogicLayer.DTOs.OpsProcurement;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/ops/procurement")]
[ApiController]
[Authorize]
public class OpsProcurementController : ControllerBase
{
    private readonly IOpsProcurementService _procurementService;
    private readonly ILogger<OpsProcurementController> _logger;

    public OpsProcurementController(IOpsProcurementService procurementService, ILogger<OpsProcurementController> logger)
    {
        _procurementService = procurementService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet("receipts")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetReceipts([FromQuery] string? status)
    {
        try
        {
            var result = await _procurementService.GetAllReceiptsAsync(status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReceipts");
            return StatusCode(500, new { message = "Lỗi khi tải danh sách phiếu nhập." });
        }
    }

    [HttpGet("receipts/{id}")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetReceiptDetail(int id)
    {
        try
        {
            var result = await _procurementService.GetReceiptDetailAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy phiếu nhập." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReceiptDetail");
            return StatusCode(500, new { message = "Lỗi khi tải chi tiết phiếu nhập." });
        }
    }

    [HttpPost("request")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> CreatePR([FromBody] CreatePurchaseRequestDto dto)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var result = await _procurementService.CreatePurchaseRequestAsync(staffId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreatePR");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPut("receipts/{id}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ApprovePR(int id)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var result = await _procurementService.ApprovePRAsync(id, managerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApprovePR");
            return StatusCode(500, new { message = "Lỗi khi duyệt phiếu." });
        }
    }

    [HttpPut("receipts/{id}/evidence")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> SubmitEvidence(int id, [FromBody] SubmitEvidenceDto dto)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var result = await _procurementService.SubmitEvidenceAsync(id, staffId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitEvidence");
            return StatusCode(500, new { message = "Lỗi khi tải bằng chứng." });
        }
    }

    [HttpPut("receipts/{id}/confirm")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> FinalConfirm(int id)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var result = await _procurementService.FinalConfirmReceiptAsync(id, managerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FinalConfirm");
            return StatusCode(500, new { message = "Lỗi khi xác nhận nhập kho." });
        }
    }
}
