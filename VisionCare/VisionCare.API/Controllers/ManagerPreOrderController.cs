using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/manager/pre-orders")]
[ApiController]
[Authorize(Roles = "Manager,Admin,Operations")]
public class ManagerPreOrderController : ControllerBase
{
    private readonly IManagerPreOrderService _managerPreOrderService;
    private readonly ILogger<ManagerPreOrderController> _logger;

    public ManagerPreOrderController(IManagerPreOrderService managerPreOrderService, ILogger<ManagerPreOrderController> logger)
    {
        _managerPreOrderService = managerPreOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Manager tạo Phiếu nhập hàng để dọn đường cho PreOrder fulfilled
    /// </summary>
    [HttpPost("receipts")]
    public async Task<IActionResult> CreateGoodsReceipt([FromBody] CreateGoodsReceiptDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            var result = await _managerPreOrderService.CreateGoodsReceiptAsync(staffId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error CreateGoodsReceipt");
            return StatusCode(500, new { message = "Lỗi khi tạo phiếu nhập hàng." });
        }
    }

    /// <summary>
    /// Manager duyệt phiếu nhập hàng (Tiến hành nhập kho thực tế)
    /// </summary>
    [HttpPut("receipts/{id}/complete")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CompleteGoodsReceipt(int id, [FromBody] CompleteGoodsReceiptDto request)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var result = await _managerPreOrderService.CompleteGoodsReceiptAsync(id, managerId, request);
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
            _logger.LogError(ex, "Error CompleteGoodsReceipt");
            return StatusCode(500, new { message = "Lỗi khi hoàn thành phiếu nhập hàng." });
        }
    }

    /// <summary>
    /// Manager tạo loạt đơn hàng cho các reservation thuộc chiến dịch
    /// </summary>
    [HttpPost("{campaignId}/convert-orders")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ConvertPreOrders(int campaignId, [FromBody] ConvertPreOrdersDto request)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var result = await _managerPreOrderService.ConvertReservationsToOrdersAsync(campaignId, managerId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ConvertPreOrders");
            return StatusCode(500, new { message = "Lỗi khi convert reservation sang order." });
        }
    }

    /// <summary>
    /// Manager điều chỉnh tỉ lệ đặt cọc (Deposit Ratio) cho một chiến dịch PreOrder
    /// </summary>
    [HttpPut("{campaignId}/deposit-config")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateDepositConfig(int campaignId, [FromBody] UpdateDepositConfigDto request)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var result = await _managerPreOrderService.UpdateDepositConfigAsync(campaignId, managerId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error UpdateDepositConfig");
            return StatusCode(500, new { message = "Lỗi khi cập nhật cấu hình đặt cọc." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
