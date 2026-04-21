using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Order;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(int customerId, CreateOrderRequestDto request);
    Task<List<OrderListItemDto>> GetOrdersAsync(int customerId);
    Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int customerId);
    Task<OrderResponseDto> CancelOrderAsync(int orderId, int customerId);
    Task<PayOSLinkResponseDto> CreatePaymentLinkAsync(int orderId, int customerId);
    Task<bool> CheckPaymentStatusAsync(int orderId, int customerId, string paymentLinkId);
}