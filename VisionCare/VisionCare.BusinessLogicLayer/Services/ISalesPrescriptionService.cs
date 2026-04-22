using VisionCare.BusinessLogicLayer.DTOs.SalesPrescription;

namespace VisionCare.BusinessLogicLayer.Services;

public interface ISalesPrescriptionService
{
    /// <summary>
    /// Lấy danh sách đơn kính cần verify (chưa verify, chưa reject)
    /// </summary>
    Task<List<SalesPrescriptionListDto>> GetPrescriptionOrdersAsync(string? search, int? orderStatusFilter);

    /// <summary>
    /// Lấy chi tiết prescription để review kèm validation
    /// </summary>
    Task<PrescriptionReviewDto> GetPrescriptionReviewAsync(int prescriptionId);

    /// <summary>
    /// Sales staff verify hoặc reject một prescription
    /// </summary>
    Task<PrescriptionReviewDto> VerifyPrescriptionAsync(int prescriptionId, int staffId, VerifyPrescriptionRequestDto request);

    /// <summary>
    /// Sales staff liên hệ khách để điều chỉnh đơn kính
    /// </summary>
    Task<AdjustPrescriptionResponseDto> AdjustPrescriptionAsync(int prescriptionId, int staffId, AdjustPrescriptionRequestDto request);
}
