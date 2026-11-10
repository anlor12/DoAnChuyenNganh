using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu trữ Nhật ký thao tác hệ thống (Audit Logs) của các tài khoản Admin / Nhân viên
    /// Ghi vết các hành vi nhạy cảm: sửa giá sản phẩm, chỉnh kho thủ công, hủy đơn hàng, xuất báo cáo...
    /// </summary>
    public class NhatKyHeThong
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Tên đăng nhập của tài khoản thực hiện</summary>
        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        /// <summary>Họ tên người thực hiện</summary>
        [StringLength(100)]
        public string? HoTen { get; set; }

        /// <summary>Vai trò (Quản trị viên, Nhân viên, Hệ thống...)</summary>
        [StringLength(50)]
        public string? VaiTro { get; set; }

        /// <summary>
        /// Loại hành động: 
        /// SUA_GIA_SAN_PHAM, CHINH_KHO_THU_CONG, HUY_DON_HANG, XUAT_BAO_CAO, 
        /// DOI_TRANG_THAI_DON, XOA_SAN_PHAM, THEM_SAN_PHAM, DANG_NHAP_ADMIN...
        /// </summary>
        [Required]
        [StringLength(50)]
        public string HanhDong { get; set; } = string.Empty;

        /// <summary>Nội dung mô tả chi tiết hành động</summary>
        [Required]
        [StringLength(500)]
        public string MoTa { get; set; } = string.Empty;

        /// <summary>Đối tượng bị ảnh hưởng (Ví dụ: SanPham #12, KhoHang #5, DonHang #105)</summary>
        [StringLength(100)]
        public string? DoiTuongAnhHuong { get; set; }

        /// <summary>Dữ liệu trước khi thay đổi (JSON hoặc chuỗi text)</summary>
        [StringLength(1000)]
        public string? GiaTriCu { get; set; }

        /// <summary>Dữ liệu sau khi thay đổi (JSON hoặc chuỗi text)</summary>
        [StringLength(1000)]
        public string? GiaTriMoi { get; set; }

        /// <summary>Địa chỉ IP của người dùng thực hiện</summary>
        [StringLength(50)]
        public string? DiaChiIp { get; set; }

        /// <summary>Thông tin trình duyệt / hệ điều hành (User Agent)</summary>
        [StringLength(255)]
        public string? UserAgent { get; set; }

        /// <summary>Mức độ nghiêm trọng: "Thấp", "Trung bình", "Cao", "Nguy hiểm"</summary>
        [StringLength(20)]
        public string MucDo { get; set; } = "Trung bình";

        /// <summary>Thời điểm phát sinh hành vi</summary>
        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }
}
