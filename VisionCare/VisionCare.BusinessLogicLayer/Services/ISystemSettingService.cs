using System.Collections.Generic;
using System.Threading.Tasks;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISystemSettingService
{
    Task<IEnumerable<SystemSetting>> GetAllAsync();
    Task<SystemSetting?> GetByKeyAsync(string key);
    Task<bool> UpdateAsync(string key, string value);
    Task<bool> UpdateMultipleAsync(Dictionary<string, string> settings);
}
