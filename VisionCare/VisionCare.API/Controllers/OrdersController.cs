using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs.Order;
using VisionCare.BusinessLogicLayer.Services;
using System.Security.Claims;

namespace VisionCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Tạo đơn hàng mới từ giỏ hàng
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var order = await _orderService.CreateOrderAsync(customerId, request);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của khách hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var orders = await _orderService.GetOrdersAsync(customerId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var order = await _orderService.GetOrderByIdAsync(id, customerId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Hủy đơn hàng
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var order = await _orderService.CancelOrderAsync(id, customerId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo checkout link thanh toán PayOS cho đơn hàng
    /// </summary>
    [HttpPost("{id}/create-payment-link")]
    public async Task<IActionResult> CreatePaymentLink(int id)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var result = await _orderService.CreatePaymentLinkAsync(id, customerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Callback API để Frontend tự gọi kiểm tra kết quả giao dịch
    /// </summary>
    [HttpGet("{id}/payment-status/{paymentLinkId}")]
    public async Task<IActionResult> CheckPayment(int id, string paymentLinkId)
    {
        try
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out var customerId))
                return BadRequest(new { message = "Không xác định được người dùng." });

            var isPaid = await _orderService.CheckPaymentStatusAsync(id, customerId, paymentLinkId);
            
            return Ok(new { 
                OrderId = id, 
                PaymentLinkId = paymentLinkId, 
                IsPaid = isPaid, 
                Status = isPaid ? "Paid" : "Unpaid"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}