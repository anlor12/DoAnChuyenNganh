using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho thông tin đơn hàng của khách hàng.
    /// </summary>
    public class DonHang
    {
        /// <summary>Mã đơn hàng (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaDonHang { get; set; }

        /// <summary>Mã người dùng đặt hàng (Khóa ngoại liên kết tới bảng NguoiDung)</summary>
        [ForeignKey("NguoiDung")]
        [Required(ErrorMessage = "Vui lòng chọn người dùng.")]
        public int MaNguoiDung { get; set; }

        /// <summary>Ngày và giờ đặt hàng (Mặc định lấy thời gian hiện tại)</summary>
        public DateTime NgayDat { get; set; } = DateTime.Now;

        /// <summary>Tổng số tiền của đơn hàng</summary>
        [Range(0, 1000000000, ErrorMessage = "Tổng tiền không được âm.")]
        public decimal TongTien { get; set; }

        /// <summary>Địa chỉ nhận hàng của khách</summary>
        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống.")]
        [StringLength(255, ErrorMessage = "Địa chỉ giao hàng tối đa 255 ký tự.")]
        public string DiaChiGiaoHang { get; set; } = string.Empty;

        /// <summary>Số điện thoại người nhận hàng</summary>
        [Required(ErrorMessage = "Số điện thoại nhận không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại nhận không hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại nhận tối đa 15 ký tự.")]
        public string SoDienThoaiNhan { get; set; } = string.Empty;

        /// <summary>Trạng thái đơn hàng (Ví dụ: Chờ xác nhận, Đang giao, Hoàn thành, Đã hủy)</summary>
        [Required(ErrorMessage = "Trạng thái đơn hàng không được để trống.")]
        [StringLength(30, ErrorMessage = "Trạng thái đơn hàng tối đa 30 ký tự.")]
        public string TrangThaiDonHang { get; set; } = "Chờ xác nhận";

        /// <summary>Phương thức thanh toán (Ví dụ: COD, MoMo, VNPAY, ChuyenKhoan)</summary>
        [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
        [StringLength(50, ErrorMessage = "Phương thức thanh toán tối đa 50 ký tự.")]
        public string PhuongThucThanhToan { get; set; } = "COD";

        /// <summary>Trạng thái thanh toán (Ví dụ: Chưa thanh toán, Đã thanh toán)</summary>
        [Required(ErrorMessage = "Trạng thái thanh toán không được để trống.")]
        [StringLength(50, ErrorMessage = "Trạng thái thanh toán tối đa 50 ký tự.")]
        public string TrangThaiThanhToan { get; set; } = "Chưa thanh toán";

        /// <summary>Ghi chú thêm từ khách hàng hoặc hệ thống</summary>
        [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
        public string? GhiChu { get; set; }

        /// <summary>Mã khuyến mãi đã áp dụng (nếu có)</summary>
        [StringLength(50)]
        public string? MaKhuyenMaiApDung { get; set; }

        /// <summary>Số tiền được giảm nhờ khuyến mãi</summary>
        public decimal SoTienGiam { get; set; } = 0;

        /// <summary>Phần trăm chiết khấu VIP áp dụng</summary>
        public int ChietKhauVip { get; set; } = 0;

        /// <summary>Số tiền được giảm từ chương trình VIP</summary>
        public decimal SoTienGiamVip { get; set; } = 0;

        /// <summary>Quà tặng kèm theo đơn hàng (nếu có)</summary>
        [StringLength(200)]
        public string? QuaTangKem { get; set; }

        /// <summary>Phí vận chuyển giao hàng</summary>
        public decimal PhiGiaoHang { get; set; } = 0;

        /// <summary>Đơn vị vận chuyển (GHTK, GHN Express, Viettel Post...)</summary>
        [StringLength(100)]
        public string? DonViVanChuyen { get; set; }

        /// <summary>Mã vận đơn giao hàng</summary>
        [StringLength(100)]
        public string? MaVanDon { get; set; }

        /// <summary>Mã giao dịch cổng thanh toán (VNPAY / MoMo...)</summary>
        [StringLength(100)]
        public string? MaGiaoDichThanhToan { get; set; }

        /// <summary>Tỉnh / Thành phố nhận hàng</summary>
        [StringLength(100)]
        public string? TinhThanh { get; set; }

        /// <summary>Quận / Huyện nhận hàng</summary>
        [StringLength(100)]
        public string? QuanHuyen { get; set; }

        /// <summary>Phường / Xã nhận hàng</summary>
        [StringLength(100)]
        public string? PhuongXa { get; set; }

        /// <summary>Lịch sử hành trình bưu kiện (JSON timeline events)</summary>
        public string? LichSuVanChuyen { get; set; }

        /// <summary>Tên và số điện thoại nhân viên giao hàng (Shipper)</summary>
        [StringLength(150)]
        public string? NhanVienGiaoHang { get; set; }

        /// <summary>Số điểm thưởng thành viên đã sử dụng cho đơn hàng này</summary>
        public int DiemSuDung { get; set; } = 0;

        /// <summary>Số tiền được giảm trừ từ điểm thưởng</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienGiamDiem { get; set; } = 0;

        /// <summary>Thông tin người dùng đặt hàng (Navigation property)</summary>
        public virtual NguoiDung? NguoiDung { get; set; }

        /// <summary>Danh sách các mặt hàng chi tiết trong đơn hàng này (Navigation property)</summary>
        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
    }
}