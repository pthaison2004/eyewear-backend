using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.Order;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/customer/pre-orders")]
[ApiController]
[Authorize(Roles = "Customer")]
public class CustomerPreOrderController : ControllerBase
{
    private readonly ICustomerPreOrderService _customerPreOrderService;
    private readonly ILogger<CustomerPreOrderController> _logger;

    public CustomerPreOrderController(ICustomerPreOrderService customerPreOrderService, ILogger<CustomerPreOrderController> logger)
    {
        _customerPreOrderService = customerPreOrderService;
        _logger = logger;
    }

    [HttpPost("{reservationId}/create-deposit-link")]
    public async Task<IActionResult> CreateDepositLink(int reservationId)
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _customerPreOrderService.CreateDepositLinkAsync(reservationId, customerId);
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
            _logger.LogError(ex, "Error creating deposit link");
            return StatusCode(500, new { message = "Lỗi khi tạo link đặt cọc." });
        }
    }

    [HttpGet("{reservationId}/payment-status/{paymentLinkId}")]
    public async Task<IActionResult> CheckPaymentStatus(int reservationId, string paymentLinkId)
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _customerPreOrderService.CheckDepositPaymentStatusAsync(reservationId, paymentLinkId, customerId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status");
            return StatusCode(500, new { message = "Lỗi khi kiểm tra thanh toán." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
