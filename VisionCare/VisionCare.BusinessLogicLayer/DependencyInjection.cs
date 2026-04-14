using Microsoft.Extensions.DependencyInjection;
using VisionCare.BusinessLogicLayer.Services;

namespace VisionCare.BusinessLogicLayer;

public static class DependencyInjection 
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }
}