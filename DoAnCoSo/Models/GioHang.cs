using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho giỏ hàng của khách hàng (mỗi khách hàng liên kết với một giỏ hàng).
    /// </summary>
    public class GioHang
    {
        /// <summary>Mã giỏ hàng (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaGioHang { get; set; }

        /// <summary>Mã người sở hữu giỏ hàng (Khóa ngoại liên kết tới bảng NguoiDung)</summary>
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        /// <summary>Ngày tạo giỏ hàng (Mặc định lấy thời gian hiện tại)</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Trạng thái hoạt động của giỏ hàng (Ví dụ: Hoạt động, Đã thanh toán)</summary>
        public string TrangThai { get; set; } = "Hoạt động";

        /// <summary>Thời điểm cập nhật giỏ hàng lần cuối</summary>
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        /// <summary>Đánh dấu đã gửi email kích cầu/cứu giỏ hàng chưa</summary>
        public bool DaGuiNhacNho { get; set; } = false;

        /// <summary>Thời điểm gửi email nhắc nhở cứu giỏ hàng gần nhất</summary>
        public DateTime? NgayGuiNhacNho { get; set; }

        /// <summary>Số lần đã gửi email nhắc nhở</summary>
        public int SoLanNhacNho { get; set; } = 0;

        /// <summary>Mã Voucher kích cầu đã đính kèm gửi khách</summary>
        public string? MaVoucherCuuGio { get; set; }

        /// <summary>Thông tin người dùng sở hữu giỏ hàng này (Navigation property)</summary>
        public virtual NguoiDung? NguoiDung { get; set; }

        /// <summary>Danh sách các mặt hàng chi tiết có trong giỏ hàng này (Navigation property)</summary>
        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();
    }
}