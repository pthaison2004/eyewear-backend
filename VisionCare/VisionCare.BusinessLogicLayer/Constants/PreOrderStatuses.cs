namespace VisionCare.BusinessLogicLayer.Constants
{
    public static class PreOrderStatuses
    {
        /// <summary>
        /// Khách đã đặt cọc 30%, đang chờ Sales gọi xác nhận lần 1.
        /// </summary>
        public const string Reserved = "reserved";

        /// <summary>
        /// Sales đã gọi xác nhận thông tin với khách (Lần 1).
        /// </summary>
        public const string Confirmed = "confirmed";

        /// <summary>
        /// Sales đã chuyển yêu cầu chuẩn bị hàng cho bộ phận Ops (Kho).
        /// </summary>
        public const string SentToOps = "sent_to_ops";

        /// <summary>
        /// Bộ phận Ops báo đã có hàng về kho cho đơn này.
        /// </summary>
        public const string StockArrived = "stock_arrived";

        /// <summary>
        /// Sales đã gọi báo khách có hàng (Lần 2) và yêu cầu thanh toán nốt.
        /// </summary>
        public const string CustomerNotified = "customer_notified";

        /// <summary>
        /// Khách đã thanh toán đủ 100%, chờ Sales duyệt để chuyển Ops làm bước cuối.
        /// </summary>
        public const string Paid = "paid";

        /// <summary>
        /// Sales đã duyệt và đẩy về Ops để thực hiện đóng gói và giao hàng.
        /// </summary>
        public const string Released = "released";

        /// <summary>
        /// Ops đã giao hàng thành công (Đã chuyển đổi thành Order thực tế).
        /// </summary>
        public const string Fulfilled = "fulfilled";

        /// <summary>
        /// Đơn đặt chỗ đã bị hủy.
        /// </summary>
        public const string Cancelled = "cancelled";
    }
}
