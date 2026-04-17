using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using VisionCare.BusinessLogicLayer.Interfaces;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.BusinessLogicLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOpsOrderService, OpsOrderService>();
        services.AddScoped<ISalesPrescriptionService, SalesPrescriptionService>();
        services.AddScoped<IShippingService, ShippingService>();

        // Seed prescription validation rules
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VisionCare.DataAccessLayer.Models.VisionCareContext>();
            if (!db.PrescriptionValidationRules.Any())
            {
                var rules = new List<VisionCare.DataAccessLayer.Models.PrescriptionValidationRule>
                {
                    new() { RuleName = "Max Sphere Value", RuleType = "sphere_max", MaxValue = -20, IsActive = true, Description = "Cận thị tối đa -20.00", SortOrder = 1, CreatedAt = DateTime.UtcNow },
                    new() { RuleName = "Max Cylinder Value", RuleType = "cylinder_max", MaxValue = -6, IsActive = true, Description = "Loạn thị tối đa -6.00", SortOrder = 2, CreatedAt = DateTime.UtcNow },
                    new() { RuleName = "PD Range", RuleType = "pd_range", MinValue = 50, MaxValue = 80, IsActive = true, Description = "PD hợp lệ 50-80mm", SortOrder = 3, CreatedAt = DateTime.UtcNow },
                    new() { RuleName = "Min Age", RuleType = "min_age", MinValue = 5, IsActive = true, Description = "Độ tuổi tối thiểu 5 tuổi", SortOrder = 4, CreatedAt = DateTime.UtcNow },
                    new() { RuleName = "Expiry Months", RuleType = "expiry_months", MaxValue = 24, IsActive = true, Description = "Đơn kính có hiệu lực trong 24 tháng", SortOrder = 5, CreatedAt = DateTime.UtcNow },
                    new() { RuleName = "Prescription Age Warning", RuleType = "age_warning", MaxValue = 12, IsActive = true, Description = "Cảnh báo nếu đơn kính trên 12 tháng", SortOrder = 6, CreatedAt = DateTime.UtcNow },
                };
                db.PrescriptionValidationRules.AddRange(rules);
                db.SaveChanges();
            }
        }

        return services;
    }
}