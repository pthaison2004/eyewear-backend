using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VisionCare.BusinessLogicLayer.Services;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SystemSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try 
        {
            var settings = await _settingService.GetAllAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"System settings error: {ex.Message}");
            return Ok(new List<SystemSetting>()); // Trả về danh sách rỗng để không crash
        }
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        try 
        {
            var setting = await _settingService.GetByKeyAsync(key);
            if (setting == null) return NotFound();
            return Ok(setting);
        }
        catch 
        {
            return NotFound();
        }
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string key, [FromBody] string value)
    {
        try 
        {
            var result = await _settingService.UpdateAsync(key, value);
            if (!result) return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("bulk-update")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMultiple([FromBody] Dictionary<string, string> settings)
    {
        try 
        {
            var result = await _settingService.UpdateMultipleAsync(settings);
            if (!result) return BadRequest("Failed to update settings");
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
