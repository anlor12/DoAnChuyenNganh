using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model quản lý thông tin kho hàng và số lượng tồn kho của từng biến thể sản phẩm.
    /// </summary>
    public class KhoHang
    {
        /// <summary>Mã kho hàng (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaKhoHang { get; set; }

        /// <summary>Mã biến thể sản phẩm tương ứng trong kho (Khóa ngoại liên kết tới bảng BienTheSanPham)</summary>
        [ForeignKey("BienTheSanPham")]
        [Required(ErrorMessage = "Vui lòng chọn biến thể sản phẩm.")]
        public int MaBienThe { get; set; }

        /// <summary>Số lượng hàng đang còn tồn trong kho cho biến thể này</summary>
        [Range(0, 1000000, ErrorMessage = "Số lượng tồn không được âm.")]
        public int SoLuongTon { get; set; }

        /// <summary>Ngày giờ cập nhật số lượng tồn kho gần nhất</summary>
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        /// <summary>Ghi chú thêm về lô hàng hoặc tình trạng nhập xuất</summary>
        [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
        public string? GhiChu { get; set; }

        /// <summary>Thông tin biến thể sản phẩm liên kết (Navigation property)</summary>
        public virtual BienTheSanPham? BienTheSanPham { get; set; }
    }
}