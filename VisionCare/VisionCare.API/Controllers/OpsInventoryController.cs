using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VisionCare.BusinessLogicLayer.DTOs.OpsInventory;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/v1/ops/inventory")]
[ApiController]
[Authorize]
public class OpsInventoryController : ControllerBase
{
    private readonly IOpsInventoryService _inventoryService;
    private readonly ILogger<OpsInventoryController> _logger;

    public OpsInventoryController(IOpsInventoryService inventoryService, ILogger<OpsInventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet("{variantId}")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetInventory(int variantId)
    {
        try
        {
            var result = await _inventoryService.GetInventoryAsync(variantId);
            if (result == null)
                return NotFound(new { message = $"Inventory for variant {variantId} not found." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetInventory");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau. Vui long thu lai sau." });
        }
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetLowStock([FromQuery] int? warehouseId, [FromQuery] int threshold = 10)
    {
        try
        {
            var result = await _inventoryService.GetLowStockAsync(warehouseId, threshold);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetLowStock");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau. Vui long thu lai sau." });
        }
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> AdjustInventory([FromBody] AdjustInventoryRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

            var result = await _inventoryService.AdjustInventoryAsync(request.VariantId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "AdjustInventory");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau. Vui long thu lai sau." });
        }
    }

    [HttpPost("replenish")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> ReplenishInventory([FromBody] ReplenishInventoryRequestDto request)
    {
        try
        {
            var staffId = GetCurrentUserId();
            if (staffId == 0)
                return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

            var result = await _inventoryService.ReplenishInventoryAsync(request.VariantId, staffId, request);
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
            _logger.LogError(ex, "Unhandled error in {Method}", "ReplenishInventory");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau. Vui long thu lai sau." });
        }
    }

    [HttpGet("movements")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] int? variantId,
        [FromQuery] int? warehouseId,
        [FromQuery] string? movementType,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _inventoryService.GetStockMovementsAsync(variantId, warehouseId, movementType, dateFrom, dateTo, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetStockMovements");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau. Vui long thu lai sau." });
        }
    }

    [HttpGet("warehouses")]
    [Authorize(Roles = "Operations,Manager,Admin")]
    public async Task<IActionResult> GetWarehouses()
    {
        try
        {
            var result = await _inventoryService.GetWarehousesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in {Method}", "GetWarehouses");
            return StatusCode(500, new { message = "Da xay ra loi khi xu ly yeu cau. Vui long thu lai sau." });
        }
    }
}