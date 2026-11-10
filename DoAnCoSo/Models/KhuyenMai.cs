using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model quản lý chương trình khuyến mãi, mã giảm giá và ưu đãi tặng quà
    /// </summary>
    public class KhuyenMai
    {
        [Key]
        [Required(ErrorMessage = "Mã khuyến mãi không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi tối đa 50 ký tự.")]
        [Column("MaGiamGia")]
        [Display(Name = "Mã khuyến mãi / Voucher")]
        public string Code { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Tên chương trình tối đa 150 ký tự.")]
        [Display(Name = "Tên chương trình")]
        public string? TenChuongTrinh { get; set; }

        /// <summary>
        /// Phân loại: "GiamGia" (Mã giảm giá theo %) hoặc "TangQua" (Quà tặng kèm theo giá trị đơn)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "Loại khuyến mãi")]
        public string LoaiKhuyenMai { get; set; } = "GiamGia";

        [Required(ErrorMessage = "Phần trăm giảm không được để trống.")]
        [Range(0, 100, ErrorMessage = "Phần trăm giảm phải từ 0 đến 100.")]
        [Display(Name = "Phần trăm giảm (%)")]
        public int PhanTramGiam { get; set; } = 0;

        [Range(0, 1000000000, ErrorMessage = "Giá trị đơn tối thiểu không hợp lệ.")]
        [Display(Name = "Đơn hàng tối thiểu (₫)")]
        public decimal GiaTriDonToiThieu { get; set; } = 0;

        [Display(Name = "Số tiền giảm tối đa (₫)")]
        public decimal? GiamToiDa { get; set; }

        [StringLength(200, ErrorMessage = "Tên quà tặng tối đa 200 ký tự.")]
        [Display(Name = "Món quà tặng kèm")]
        public string? QuaTangKem { get; set; }

        [StringLength(500, ErrorMessage = "Link ảnh quà tặng tối đa 500 ký tự.")]
        [Display(Name = "Hình ảnh quà tặng")]
        public string? HinhAnhQuaTang { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime NgayBatDau { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
        [Display(Name = "Ngày kết thúc")]
        public DateTime NgayKetThuc { get; set; } = DateTime.Now.AddMonths(1);

        [Required(ErrorMessage = "Số lượng không được để trống.")]
        [Range(0, 1000000, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0.")]
        [Display(Name = "Số lượng mã / quà")]
        public int SoLuong { get; set; } = 100;

        [StringLength(500)]
        [Display(Name = "Mô tả / Thể lệ")]
        public string? MoTa { get; set; }

        [Display(Name = "Hiển thị trang chủ / giỏ hàng")]
        public bool HienThiTrangChu { get; set; } = true;

        [Display(Name = "Chỉ áp dụng cho khách hàng mới (Đơn đầu tiên)")]
        public bool ChiDanhChoKhachMoi { get; set; } = false;

        [Display(Name = "Mã loại sản phẩm áp dụng (Để trống nếu áp dụng tất cả)")]
        public int? MaLoaiSanPhamApDung { get; set; }

        [ForeignKey("MaLoaiSanPhamApDung")]
        public virtual LoaiSanPham? LoaiSanPhamApDung { get; set; }

        [Display(Name = "Đang kích hoạt")]
        public bool TrangThai { get; set; } = true;
    }
}
