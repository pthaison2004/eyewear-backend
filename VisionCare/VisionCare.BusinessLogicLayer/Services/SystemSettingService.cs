using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly VisionCareContext _context;

    public SystemSettingService(VisionCareContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SystemSetting>> GetAllAsync()
    {
        return await _context.SystemSettings.ToListAsync();
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key)
    {
        return await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
    }

    public async Task<bool> UpdateAsync(string key, string value)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        if (setting == null) return false;

        setting.SettingValue = value;
        setting.UpdatedAt = DateTime.UtcNow;
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateMultipleAsync(Dictionary<string, string> settings)
    {
        var keys = settings.Keys.ToList();
        var dbSettings = await _context.SystemSettings.Where(s => keys.Contains(s.SettingKey)).ToListAsync();

        foreach (var dbSetting in dbSettings)
        {
            if (settings.TryGetValue(dbSetting.SettingKey, out var newValue))
            {
                dbSetting.SettingValue = newValue;
                dbSetting.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await _context.SaveChangesAsync() > 0;
    }
}
