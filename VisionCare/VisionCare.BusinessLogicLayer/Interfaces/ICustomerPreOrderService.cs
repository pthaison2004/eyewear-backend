using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Order;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface ICustomerPreOrderService
{
    Task<PayOSLinkResponseDto> CreateDepositLinkAsync(int reservationId, int customerId);
    Task<object> CheckDepositPaymentStatusAsync(int reservationId, string paymentLinkId, int customerId);
}
