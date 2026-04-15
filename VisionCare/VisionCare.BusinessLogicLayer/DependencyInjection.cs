using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}