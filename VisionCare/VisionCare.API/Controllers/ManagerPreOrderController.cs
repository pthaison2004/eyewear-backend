using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;
using VisionCare.BusinessLogicLayer.DTOs.SalesPreOrder;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/manager/pre-orders")]
[ApiController]
[Authorize(Roles = "Manager,Admin")]
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
    /// Manager lấy danh sách chiến dịch pre-order
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns([FromQuery] string? status)
    {
        var result = await _managerPreOrderService.GetPreOrderCampaignsAsync(status);
        return Ok(result);
    }

    /// <summary>
    /// Manager tạo chiến dịch pre-order mới
    /// </summary>
    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreatePreOrderCampaignRequestDto request)
    {
        var managerId = GetCurrentUserId();
        var result = await _managerPreOrderService.CreateCampaignAsync(request, managerId);
        return Ok(result);
    }

    /// <summary>
    /// Manager cập nhật chiến dịch
    /// </summary>
    [HttpPut("campaigns/{id}")]
    public async Task<IActionResult> UpdateCampaign(int id, [FromBody] UpdatePreOrderCampaignRequestDto request)
    {
        var managerId = GetCurrentUserId();
        var result = await _managerPreOrderService.UpdateCampaignAsync(id, request, managerId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Manager lấy chi tiết chiến dịch
    /// </summary>
    [HttpGet("campaigns/{id}")]
    public async Task<IActionResult> GetCampaignDetail(int id)
    {
        var result = await _managerPreOrderService.GetCampaignDetailAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Manager tạo Phiếu nhập hàng
    /// </summary>
    [HttpPost("receipts")]
    public async Task<IActionResult> CreateGoodsReceipt([FromBody] CreateGoodsReceiptDto request)
    {
        var staffId = GetCurrentUserId();
        var result = await _managerPreOrderService.CreateGoodsReceiptAsync(staffId, request);
        return Ok(result);
    }

    /// <summary>
    /// Manager duyệt phiếu nhập hàng
    /// </summary>
    [HttpPut("receipts/{id}/complete")]
    public async Task<IActionResult> CompleteGoodsReceipt(int id, [FromBody] CompleteGoodsReceiptDto request)
    {
        var managerId = GetCurrentUserId();
        var result = await _managerPreOrderService.CompleteGoodsReceiptAsync(id, managerId, request);
        return Ok(result);
    }

    /// <summary>
    /// Manager tạo loạt đơn hàng cho các reservation đã được "Released"
    /// </summary>
    [HttpPost("{campaignId}/convert-orders")]
    public async Task<IActionResult> ConvertPreOrders(int campaignId, [FromBody] ConvertPreOrdersDto request)
    {
        var managerId = GetCurrentUserId();
        var result = await _managerPreOrderService.ConvertReservationsToOrdersAsync(campaignId, managerId, request);
        return Ok(result);
    }

    /// <summary>
    /// Manager điều chỉnh tỉ lệ đặt cọc
    /// </summary>
    [HttpPut("{campaignId}/deposit-config")]
    public async Task<IActionResult> UpdateDepositConfig(int campaignId, [FromBody] UpdateDepositConfigDto request)
    {
        var managerId = GetCurrentUserId();
        var result = await _managerPreOrderService.UpdateDepositConfigAsync(campaignId, managerId, request);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
