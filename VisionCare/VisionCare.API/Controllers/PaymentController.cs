using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using VisionCare.DataAccessLayer;
using VisionCare.DataAccessLayer.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;

namespace VisionCare.API.Controllers;

[Route("api/payment")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly PayOSClient _payOS;
    private readonly VisionCareContext _context;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PayOSClient payOS, VisionCareContext context, ILogger<PaymentController> logger)
    {
        _payOS = payOS;
        _context = context;
        _logger = logger;
    }

    [HttpPost("payos-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSWebhook([FromBody] System.Text.Json.JsonElement request)
    {
        try
        {
            var code = request.GetProperty("code").GetString();
            var dataObj = request.GetProperty("data");
            var orderCodeStr = dataObj.GetProperty("orderCode").GetInt64().ToString();

            if (code == "00") // 00 means payment successful
            {

                
                // Extract orderId: the last 10 digits are unix timestamp, the rest is orderId
                if (orderCodeStr.Length <= 10)
                {
                    _logger.LogWarning("Invalid orderCode format from PayOS: {OrderCode}", orderCodeStr);
                    return Ok();
                }

                string orderIdStr = orderCodeStr.Substring(0, orderCodeStr.Length - 10);
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                    if (order != null)
                    {
                        if (order.PaymentStatus != "Paid")
                        {
                            order.PaymentStatus = "Paid";
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Order {OrderId} marked as Paid via PayOS webhook.", orderId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Order {OrderId} not found for PayOS hook.", orderId);
                    }
                }
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            return BadRequest(new { message = ex.Message });
        }
    }
}
