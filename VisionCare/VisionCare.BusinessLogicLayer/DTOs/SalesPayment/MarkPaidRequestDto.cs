namespace VisionCare.BusinessLogicLayer.DTOs.SalesPayment;

public class MarkPaidRequestDto
{
    // "COD" | "BankTransfer" | "Cash"
    public string PaymentMethod { get; set; } = "COD";

    // Số tiền thực nhận (nullable — nếu null = full amount)
    public decimal? AmountPaid { get; set; }

    // Mã giao dịch ngân hàng (nếu chuyển khoản)
    public string? TransactionRef { get; set; }

    // Ghi chú thêm
    public string? Note { get; set; }
}
