using System.Collections.Generic;
using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Order;

namespace VisionCare.BusinessLogicLayer.Interfaces;

public interface ICustomerPreOrderService
{
    Task<PayOSLinkResponseDto> CreateDepositLinkAsync(int reservationId, int customerId);
    Task<object> CheckDepositPaymentStatusAsync(int reservationId, string paymentLinkId, int customerId);
    Task<bool> SimulatePaymentSuccessAsync(int reservationId, int customerId);
    Task<List<CustomerReservationDto>> GetMyReservationsAsync(int customerId);
    Task<PayOSLinkResponseDto> CreateFinalPaymentLinkAsync(int reservationId, int customerId);
    Task<object> CheckFinalPaymentStatusAsync(int reservationId, string paymentLinkId, int customerId);
    Task<bool> SimulateFinalPaymentAsync(int reservationId, int customerId);
}
