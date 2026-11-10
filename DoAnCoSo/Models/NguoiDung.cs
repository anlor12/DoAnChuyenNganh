using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu trữ thông tin cá nhân chi tiết của người dùng/khách hàng.
    /// </summary>
    public class NguoiDung
    {
        /// <summary>Mã thông tin người dùng (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaNguoiDung { get; set; }

        /// <summary>Mã tài khoản đăng nhập liên kết (Khóa ngoại liên kết tới bảng TaiKhoan)</summary>
        [ForeignKey("TaiKhoan")]
        [Required(ErrorMessage = "Vui lòng chọn tài khoản.")]
        public int MaTaiKhoan { get; set; }

        /// <summary>Họ tên đầy đủ của người dùng</summary>
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        /// <summary>Giới tính (Nam, Nữ, Khác)</summary>
        [StringLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
        public string? GioiTinh { get; set; }

        /// <summary>Ngày sinh của người dùng</summary>
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        /// <summary>Địa chỉ liên lạc hoặc địa chỉ giao hàng mặc định</summary>
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
        public string? DiaChi { get; set; }

        /// <summary>Chiều cao tính bằng cm (phục vụ tính năng gợi ý size quần áo)</summary>
        [Range(50, 250, ErrorMessage = "Chiều cao phải từ 50 đến 250 cm.")]
        public double? ChieuCao { get; set; }

        /// <summary>Cân nặng tính bằng kg (phục vụ tính năng gợi ý size quần áo)</summary>
        [Range(10, 300, ErrorMessage = "Cân nặng phải từ 10 đến 300 kg.")]
        public double? CanNang { get; set; }

        /// <summary>Đường dẫn tệp tin ảnh đại diện (avatar)</summary>
        [StringLength(255, ErrorMessage = "Ảnh đại diện tối đa 255 ký tự.")]
        public string? AnhDaiDien { get; set; }

        /// <summary>Tổng chi tiêu tích lũy của khách hàng</summary>
        public decimal TongChiTieu { get; set; } = 0;

        /// <summary>Cấp độ / Hạng VIP hiện tại của khách hàng</summary>
        [StringLength(50)]
        public string? HangVip { get; set; } = "VIP Khởi Đầu";

        /// <summary>Điểm tích lũy thành viên</summary>
        public int DiemTichLuy { get; set; } = 0;

        /// <summary>Mã giới thiệu cá nhân độc nhất</summary>
        [StringLength(50)]
        public string? MaGioiThieu { get; set; }

        /// <summary>Mã Người dùng đã giới thiệu khách hàng này (nếu có)</summary>
        public int? NguoiGioiThieuId { get; set; }

        /// <summary>Số lượng bạn bè đã giới thiệu thành công</summary>
        public int SoNguoiDaGioiThieu { get; set; } = 0;

        /// <summary>Tài khoản đăng nhập tương ứng (Navigation property)</summary>
        public virtual TaiKhoan? TaiKhoan { get; set; }

        /// <summary>Danh sách các giỏ hàng của người dùng này (Navigation property)</summary>
        public virtual ICollection<GioHang> GioHangs { get; set; } = new List<GioHang>();

        /// <summary>Danh sách các đơn đặt hàng đã thực hiện (Navigation property)</summary>
        public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();

        /// <summary>Danh sách các gợi ý size hệ thống đề xuất (Navigation property)</summary>
        public virtual ICollection<GoiYKichCo> GoiYKichCos { get; set; } = new List<GoiYKichCo>();
    }
}