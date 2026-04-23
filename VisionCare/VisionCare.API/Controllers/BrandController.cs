using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisionCare.BusinessLogicLayer.DTOs;
using VisionCare.BusinessLogicLayer.Interfaces;

namespace VisionCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BrandController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet("approved")]
    [AllowAnonymous]
    public async Task<IActionResult> GetApprovedBrands()
    {
        var result = await _brandService.GetApprovedBrandsAsync();
        return Ok(result);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var result = await _brandService.GetPendingRequestsAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> RequestAddBrand([FromBody] CreateBrandRequestDto request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff";
        
        var result = await _brandService.RequestAddBrandAsync(request, userId, role);
        return Ok(result);
    }

    [HttpDelete("{id}/request")]
    public async Task<IActionResult> RequestDeleteBrand(int id, [FromBody] DeleteBrandRequestDto request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _brandService.RequestDeleteBrandAsync(id, request, userId);
        
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ProcessRequest(int id, [FromBody] BrandActionDto action)
    {
        var managerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var success = await _brandService.ProcessBrandRequestAsync(id, action, managerId);
        
        if (!success) return NotFound();
        return Ok(new { message = "Request processed successfully" });
    }
}
