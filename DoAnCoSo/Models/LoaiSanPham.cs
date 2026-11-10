using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho danh mục loại sản phẩm (ví dụ: Áo khoác, Quần jean, Giày dép).
    /// </summary>
    public class LoaiSanPham
    {
        /// <summary>Mã loại sản phẩm (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaLoaiSanPham { get; set; }

        /// <summary>Tên loại sản phẩm (Ví dụ: Đồ thể thao)</summary>
        [Required(ErrorMessage = "Tên loại sản phẩm không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên loại sản phẩm tối đa 100 ký tự.")]
        public string TenLoaiSanPham { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết về loại sản phẩm này</summary>
        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự.")]
        public string? MoTa { get; set; }

        /// <summary>Đường dẫn hình ảnh đại diện của loại sản phẩm (nếu có)</summary>
        [StringLength(255)]
        public string? HinhAnh { get; set; }

        /// <summary>Mã danh mục cha (null nếu là danh mục gốc/cha)</summary>
        public int? MaLoaiCha { get; set; }

        /// <summary>Danh mục cha (Navigation property)</summary>
        public virtual LoaiSanPham? LoaiCha { get; set; }

        /// <summary>Danh sách các danh mục con (Navigation property)</summary>
        public virtual ICollection<LoaiSanPham> LoaiCons { get; set; } = new List<LoaiSanPham>();

        /// <summary>Danh sách các sản phẩm thuộc danh mục loại này (Navigation property)</summary>
        public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}