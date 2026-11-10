using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho thông tin tài khoản đăng nhập hệ thống của người dùng.
    /// </summary>
    public class TaiKhoan
    {
        /// <summary>Mã tài khoản (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaTaiKhoan { get; set; }

        /// <summary>Tên đăng nhập duy nhất</summary>
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
        public string TenDangNhap { get; set; } = string.Empty;

        /// <summary>Mật khẩu đăng nhập (thường được băm trước khi lưu trữ)</summary>
        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Mật khẩu từ 3 đến 100 ký tự.")]
        public string MatKhau { get; set; } = string.Empty;

        /// <summary>Địa chỉ Email của tài khoản</summary>
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên lạc liên kết với tài khoản</summary>
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        public string? SoDienThoai { get; set; }

        /// <summary>Vai trò trong hệ thống (Ví dụ: Quản trị viên, Nhân viên, Khách hàng)</summary>
        [Required(ErrorMessage = "Vai trò không được để trống.")]
        [StringLength(20, ErrorMessage = "Vai trò tối đa 20 ký tự.")]
        public string VaiTro { get; set; } = "Khách hàng";

        /// <summary>Trạng thái tài khoản (Ví dụ: Hoạt động, Bị khóa)</summary>
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [StringLength(20, ErrorMessage = "Trạng thái tối đa 20 ký tự.")]
        public string TrangThai { get; set; } = "Hoạt động";

        /// <summary>Ngày tạo tài khoản</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Nhà cung cấp OAuth (Google, Facebook, Local)</summary>
        [StringLength(50)]
        public string? OAuthProvider { get; set; }

        /// <summary>Mã định danh duy nhất từ Google/Facebook</summary>
        [StringLength(100)]
        public string? OAuthId { get; set; }

        /// <summary>Thông tin người dùng cá nhân chi tiết (Navigation property)</summary>
        public virtual NguoiDung? NguoiDung { get; set; }

        /// <summary>Họ tên hiển thị đăng ký nhanh khi tạo tài khoản</summary>
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;
    }
}