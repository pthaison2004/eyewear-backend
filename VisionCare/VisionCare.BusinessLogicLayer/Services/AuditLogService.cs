using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class AuditLogService : IAuditLogService
{
    private readonly VisionCareContext _context;

    public AuditLogService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(int? userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _context.AuditLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId)
    {
        return await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }
}
