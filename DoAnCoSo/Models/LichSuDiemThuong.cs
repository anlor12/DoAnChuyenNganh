using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model ghi vết toàn bộ lịch sử biến động điểm thưởng (Loyalty Points) của khách hàng.
    /// </summary>
    public class LichSuDiemThuong
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Mã người dùng sở hữu giao dịch điểm (FK tới NguoiDung)</summary>
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        /// <summary>Số điểm biến động (Dương: cộng điểm; Âm: tiêu điểm)</summary>
        public int SoDiem { get; set; }

        /// <summary>
        /// Phân loại giao dịch:
        /// - "TichDiemDonHang": Tích điểm khi hoàn thành đơn
        /// - "TieuDiemThanhToan": Tiêu điểm để giảm trừ tiền đơn hàng
        /// - "ThuongGioiThieu": Thưởng khi bạn bè được giới thiệu hoàn thành đơn
        /// - "ChaoMungDangKy": Tặng điểm khi tạo tài khoản
        /// - "HoanDiem": Hoàn điểm khi đơn hàng bị hủy
        /// </summary>
        [Required]
        [StringLength(50)]
        public string LoaiGiaoDich { get; set; } = string.Empty;

        /// <summary>Nội dung mô tả chi tiết giao dịch</summary>
        [Required]
        [StringLength(255)]
        public string MoTa { get; set; } = string.Empty;

        /// <summary>Mã đơn hàng liên quan (nếu có)</summary>
        public int? MaDonHang { get; set; }

        /// <summary>Thời điểm phát sinh giao dịch</summary>
        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        /// <summary>Thông tin khách hàng (Navigation property)</summary>
        public virtual NguoiDung? NguoiDung { get; set; }

        /// <summary>Thông tin đơn hàng liên quan (Navigation property)</summary>
        [ForeignKey("MaDonHang")]
        public virtual DonHang? DonHang { get; set; }
    }
}
