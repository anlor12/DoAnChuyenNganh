using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu trữ thông tin gợi ý kích cỡ trang phục phù hợp cho khách hàng dựa trên dữ liệu hình thể.
    /// </summary>
    public class GoiYKichCo
    {
        /// <summary>Mã gợi ý (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaGoiY { get; set; }

        /// <summary>Mã khách hàng nhận gợi ý (Khóa ngoại liên kết tới bảng NguoiDung)</summary>
        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        /// <summary>Mã biến thể sản phẩm được khuyên dùng (Khóa ngoại liên kết tới bảng BienTheSanPham)</summary>
        [ForeignKey("BienTheSanPham")]
        public int MaBienThe { get; set; }

        /// <summary>Kích cỡ được hệ thống đề xuất (Ví dụ: M, L)</summary>
        [StringLength(20)]
        public string KichCoGoiY { get; set; } = string.Empty;

        /// <summary>Lý do đề xuất (Ví dụ: Phù hợp với chiều cao 170cm, cân nặng 65kg)</summary>
        [StringLength(255)]
        public string? LyDoGoiY { get; set; }

        /// <summary>Thời gian hệ thống thực hiện gợi ý (Mặc định là thời gian hiện tại)</summary>
        public DateTime ThoiGianGoiY { get; set; } = DateTime.Now;

        /// <summary>Khách hàng được gợi ý (Navigation property)</summary>
        public virtual NguoiDung? NguoiDung { get; set; }

        /// <summary>Biến thể sản phẩm gợi ý (Navigation property)</summary>
        public virtual BienTheSanPham? BienTheSanPham { get; set; }
    }
}