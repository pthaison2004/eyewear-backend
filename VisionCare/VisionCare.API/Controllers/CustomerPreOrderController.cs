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

    /// <summary>
    /// Lấy danh sách đặt trước của khách hàng hiện tại
    /// </summary>
    [HttpGet("my-reservations")]
    public async Task<IActionResult> GetMyReservations()
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _customerPreOrderService.GetMyReservationsAsync(customerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer reservations");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách đặt trước." });
        }
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
    public async Task<IActionResult> CheckPaymentStatus(int reservationId, string paymentLinkId, [FromQuery] string type = "deposit")
    {
        try
        {
            var customerId = GetCurrentUserId();
            object result;
            
            if (type.ToLower() == "final")
            {
                result = await _customerPreOrderService.CheckFinalPaymentStatusAsync(reservationId, paymentLinkId, customerId);
            }
            else
            {
                result = await _customerPreOrderService.CheckDepositPaymentStatusAsync(reservationId, paymentLinkId, customerId);
            }
            
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

    [HttpPost("{reservationId}/simulate-payment")]
    public async Task<IActionResult> SimulatePayment(int reservationId)
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _customerPreOrderService.SimulatePaymentSuccessAsync(reservationId, customerId);
            return Ok(new { success = result, message = "Thanh toán giả lập thành công!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in simulated payment");
            return StatusCode(500, new { message = "Lỗi khi giả lập thanh toán." });
        }
    }

    /// <summary>
    /// Tạo link thanh toán 70% còn lại khi hàng đã có và được thông báo
    /// </summary>
    [HttpPost("{reservationId}/create-final-payment-link")]
    public async Task<IActionResult> CreateFinalPaymentLink(int reservationId)
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _customerPreOrderService.CreateFinalPaymentLinkAsync(reservationId, customerId);
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
            _logger.LogError(ex, "Error creating final payment link");
            return StatusCode(500, new { message = "Lỗi khi tạo link thanh toán còn lại." });
        }
    }

    /// <summary>
    /// [DEMO] Giả lập hoàn thành thanh toán 70% còn lại, không cần cổng thanh toán thật
    /// </summary>
    [HttpPost("{reservationId}/simulate-final-payment")]
    public async Task<IActionResult> SimulateFinalPayment(int reservationId)
    {
        try
        {
            var customerId = GetCurrentUserId();
            var result = await _customerPreOrderService.SimulateFinalPaymentAsync(reservationId, customerId);
            return Ok(new { success = result, message = "Giả lập thanh toán thành công! Đơn hàng của bạn sẽ được xử lý sớm." });
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
            _logger.LogError(ex, "Error in simulate final payment");
            return StatusCode(500, new { message = "Lỗi khi giả lập thanh toán." });
        }
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }
}
