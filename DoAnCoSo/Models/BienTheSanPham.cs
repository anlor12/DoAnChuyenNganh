using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho biến thể của sản phẩm (ví dụ: một sản phẩm có thể có nhiều kích thước, màu sắc khác nhau).
    /// </summary>
    public class BienTheSanPham
    {
        /// <summary>Mã biến thể sản phẩm (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaBienThe { get; set; }

        /// <summary>Mã sản phẩm (Khóa ngoại liên kết tới bảng SanPham)</summary>
        [ForeignKey("SanPham")]
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
        public int MaSanPham { get; set; }

        /// <summary>Kích cỡ của biến thể (Ví dụ: S, M, L, XL)</summary>
        [Required(ErrorMessage = "Kích cỡ không được để trống.")]
        [StringLength(20, ErrorMessage = "Kích cỡ tối đa 20 ký tự.")]
        public string KichCo { get; set; } = string.Empty;

        /// <summary>Màu sắc của biến thể (Ví dụ: Đỏ, Đen, Trắng)</summary>
        [Required(ErrorMessage = "Màu sắc không được để trống.")]
        [StringLength(50, ErrorMessage = "Màu sắc tối đa 50 ký tự.")]
        public string MauSac { get; set; } = string.Empty;

        /// <summary>Giá bán của biến thể sản phẩm cụ thể này</summary>
        [Range(1, 1000000000, ErrorMessage = "Giá phải lớn hơn 0.")]
        public decimal Gia { get; set; }

        /// <summary>Đường dẫn hình ảnh riêng cho biến thể này (nếu có)</summary>
        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh tối đa 255 ký tự.")]
        public string? HinhAnh { get; set; }

        /// <summary>Trạng thái tồn kho của biến thể (Mặc định: "Còn hàng")</summary>
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [StringLength(30, ErrorMessage = "Trạng thái tối đa 30 ký tự.")]
        public string TrangThai { get; set; } = "Còn hàng";

        /// <summary>Thông tin sản phẩm gốc (Navigation property)</summary>
        public virtual SanPham? SanPham { get; set; }

        /// <summary>Danh sách tồn kho của biến thể này (Navigation property)</summary>
        public virtual ICollection<KhoHang> KhoHangs { get; set; } = new List<KhoHang>();
    }
}