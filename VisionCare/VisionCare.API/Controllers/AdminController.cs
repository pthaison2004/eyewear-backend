using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VisionCare.BusinessLogicLayer.Services;
using VisionCare.DataAccessLayer.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace VisionCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly VisionCare.DataAccessLayer.Models.VisionCareContext _context;
    private readonly IAuditLogService _auditService;

    public AdminController(VisionCare.DataAccessLayer.Models.VisionCareContext context, IAuditLogService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private int? GetCurrentUserId() {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idStr, out int id)) return id;
        return null;
    }

    private async Task SafeLog(string action, string entity, string entityId, string oldV = null, string newV = null) {
        try {
            await _auditService.LogActionAsync(GetCurrentUserId(), action, entity, entityId, oldV, newV);
        } catch (Exception) { }
    }

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int count = 100)
    {
        try
        {
            var logs = await _context.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .Select(l => new {
                    l.AuditId,
                    l.Action,
                    l.EntityName,
                    l.EntityId,
                    l.OldValues,
                    l.NewValues,
                    l.CreatedAt,
                    l.IpAddress,
                    UserFullName = l.User != null ? l.User.FullName : "Hệ thống"
                })
                .ToListAsync();
            return Ok(logs);
        }
        catch { return Ok(new List<object>()); }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new {
                u.UserId,
                u.FullName,
                u.Email,
                RoleName = u.Role != null ? u.Role.RoleName : "No Role",
                IsActive = u.IsActive ?? true,
                CreatedAt = u.CreatedAt ?? DateTime.UtcNow
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("users/{userId}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUserStatus(int userId)
    {
        try 
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            bool wasActive = user.IsActive ?? true;
            user.IsActive = !wasActive;
            
            if (user.IsActive == false) {
                var exists = await _context.BlacklistedIps.AnyAsync(b => b.IpAddress == user.Email);
                if (!exists) {
                    _context.BlacklistedIps.Add(new BlacklistedIp {
                        IpAddress = user.Email ?? "Unknown",
                        Reason = $"Tài khoản bị Admin khóa: {user.FullName}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            } else {
                var banned = await _context.BlacklistedIps.FirstOrDefaultAsync(b => b.IpAddress == user.Email);
                if (banned != null) _context.BlacklistedIps.Remove(banned);
            }

            await _context.SaveChangesAsync();
            await SafeLog("TOGGLE_STATUS", "User", user.UserId.ToString(), wasActive.ToString(), user.IsActive.ToString());

            return Ok(new { success = true, isActive = user.IsActive });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRoles()
    {
        return Ok(await _context.Roles.ToListAsync());
    }

    [HttpGet("blacklist")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBlacklist()
    {
        return Ok(await _context.BlacklistedIps.ToListAsync());
    }

    [HttpPost("blacklist")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddToBlacklist([FromBody] BlacklistedIp ip)
    {
        ip.CreatedAt = DateTime.UtcNow;
        _context.BlacklistedIps.Add(ip);
        await _context.SaveChangesAsync();
        await SafeLog("ADD_BLACKLIST", "IP", ip.IpAddress, null, ip.Reason);
        return Ok(ip);
    }

    [HttpDelete("blacklist/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveFromBlacklist(int id)
    {
        var ip = await _context.BlacklistedIps.FindAsync(id);
        if (ip == null) return NotFound();
        _context.BlacklistedIps.Remove(ip);
        await _context.SaveChangesAsync();
        await SafeLog("REMOVE_BLACKLIST", "IP", ip.IpAddress, "Removed", null);
        return NoContent();
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "Paid").SumAsync(o => o.TotalAmount);
        var activePromotions = await _context.Promotions.CountAsync(p => p.IsActive == true);

        return Ok(new {
            TotalUsers = totalUsers,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            ActivePromotions = activePromotions,
            RecentGrowth = "+12.5%"
        });
    }

    [HttpPost("users/{userId}/change-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] int roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();
        int? oldR = user.RoleId;
        user.RoleId = roleId;
        await _context.SaveChangesAsync();
        await SafeLog("CHANGE_ROLE", "User", userId.ToString(), oldR.ToString(), roleId.ToString());
        return Ok(new { success = true });
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await SafeLog("CREATE_USER", "User", user.UserId.ToString(), null, user.Email);
        return Ok(user);
    }

    [HttpPost("users/{userId}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(int userId, [FromBody] string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        await SafeLog("RESET_PASSWORD", "User", userId.ToString(), null, "Password Updated");
        return Ok(new { success = true });
    }

    [HttpPost("roles/{roleId}/permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] List<string> permissions)
    {
        var key = $"RolePerms_{roleId}";
        var val = string.Join(",", permissions);
        var existing = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        string oldV = existing?.SettingValue;
        if (existing != null) existing.SettingValue = val;
        else _context.SystemSettings.Add(new SystemSetting { SettingKey = key, SettingValue = val, GroupName = "Permissions" });
        await _context.SaveChangesAsync();
        await SafeLog("UPDATE_PERMS", "Role", roleId.ToString(), oldV, val);
        return Ok(new { success = true });
    }

    [HttpPost("init-permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InitDefaultPermissions()
    {
        var roles = await _context.Roles.ToListAsync();
        var allPerms = "SYSTEM_CONFIG,USER_MANAGEMENT,ORDER_MANAGEMENT,PRODUCT_MANAGEMENT,FINANCE_STATS";
        
        foreach (var role in roles)
        {
            var key = $"RolePerms_{role.RoleId}";
            var exists = await _context.SystemSettings.AnyAsync(s => s.SettingKey == key);
            if (!exists)
            {
                _context.SystemSettings.Add(new SystemSetting {
                    SettingKey = key,
                    SettingValue = role.RoleName == "Admin" ? allPerms : "ORDER_MANAGEMENT",
                    GroupName = "Permissions",
                    Description = $"Mặc định cho {role.RoleName}"
                });
            }
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã khởi tạo quyền mặc định thành công" });
    }
}
