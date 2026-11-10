namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu trữ thông tin lỗi hiển thị trên giao diện thông báo lỗi (Error View).
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>Mã định danh duy nhất của HTTP Request gây ra lỗi (dùng để tra cứu log)</summary>
        public string? RequestId { get; set; }

        /// <summary>Trả về giá trị true nếu RequestId có dữ liệu, dùng để quyết định việc hiển thị RequestId trên giao diện</summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
