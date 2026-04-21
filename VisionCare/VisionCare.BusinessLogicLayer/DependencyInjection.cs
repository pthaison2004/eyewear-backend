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
        services.AddScoped<ISalesPreOrderService, SalesPreOrderService>();
        services.AddScoped<ISalesComplaintService, SalesComplaintService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<ISalesReportService, SalesReportService>();
        services.AddScoped<IOpsInventoryService, OpsInventoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IManagerPreOrderService, ManagerPreOrderService>();

        return services;
    }
}
