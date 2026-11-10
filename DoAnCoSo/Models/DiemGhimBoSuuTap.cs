using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho điểm ghim tương tác (hotspot) trên bức ảnh của bộ sưu tập thời trang.
    /// Cho phép khách hàng nhấn vào điểm ghim để xem nhanh thông tin sản phẩm tương ứng.
    /// </summary>
    public class DiemGhimBoSuuTap
    {
        /// <summary>Mã điểm ghim (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaDiemGhim { get; set; }

        /// <summary>Mã bộ sưu tập chứa điểm ghim này (Khóa ngoại liên kết tới bảng BoSuuTap)</summary>
        [Required(ErrorMessage = "Vui lòng chọn bộ sưu tập.")]
        [ForeignKey("BoSuuTap")]
        public int MaBoSuuTap { get; set; }

        /// <summary>Mã sản phẩm được trỏ tới từ điểm ghim này (Khóa ngoại liên kết tới bảng SanPham)</summary>
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm ghim.")]
        [ForeignKey("SanPham")]
        public int MaSanPham { get; set; }

        /// <summary>Vị trí theo chiều dọc (tỷ lệ phần trăm từ cạnh trên của ảnh, ví dụ: 45.5000%)</summary>
        [Column(TypeName = "decimal(18,4)")]
        [Range(0, 100, ErrorMessage = "Tọa độ trên phải nằm trong khoảng từ 0 đến 100%.")]
        public decimal ToaDoTren { get; set; }

        /// <summary>Vị trí theo chiều ngang (tỷ lệ phần trăm từ cạnh trái của ảnh, ví dụ: 60.2000%)</summary>
        [Column(TypeName = "decimal(18,4)")]
        [Range(0, 100, ErrorMessage = "Tọa độ trái phải nằm trong khoảng từ 0 đến 100%.")]
        public decimal ToaDoTrai { get; set; }

        /// <summary>Tiêu đề hoặc ghi chú ngắn của điểm ghim (Ví dụ: Áo sơ mi nam Linen)</summary>
        [StringLength(150, ErrorMessage = "Tiêu đề tối đa 150 ký tự.")]
        public string? TieuDe { get; set; }

        /// <summary>Thông tin bộ sưu tập liên kết (Navigation property)</summary>
        public virtual BoSuuTap? BoSuuTap { get; set; }

        /// <summary>Sản phẩm liên kết tại điểm ghim này (Navigation property)</summary>
        public virtual SanPham? SanPham { get; set; }
    }
}
