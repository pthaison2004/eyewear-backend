using System.Collections.Generic;
using System.Threading.Tasks;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IAuditLogService
{
    Task LogActionAsync(int? userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null);
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(int userId);
}
