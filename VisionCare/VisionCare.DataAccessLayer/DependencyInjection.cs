using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.DataAccessLayer;

public static class DependencyInjection // PHẢI CÓ CHỮ PUBLIC
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VisionCareContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
        return services;
    }
}