using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho một bộ sưu tập thời trang (gồm hình ảnh trình diễn lớn và các điểm ghim trỏ tới sản phẩm).
    /// </summary>
    public class BoSuuTap
    {
        /// <summary>Mã bộ sưu tập (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaBoSuuTap { get; set; }

        /// <summary>Tên của bộ sưu tập (Ví dụ: BST Thu Đông Fashion Store 2026)</summary>
        [Required(ErrorMessage = "Tên bộ sưu tập không được để trống.")]
        [StringLength(150, ErrorMessage = "Tên bộ sưu tập tối đa 150 ký tự.")]
        public string TenBoSuuTap { get; set; } = string.Empty;

        /// <summary>Mô tả ý tưởng phong cách của bộ sưu tập</summary>
        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string? MoTa { get; set; }

        /// <summary>Đường dẫn hình ảnh chụp bộ sưu tập chất lượng cao</summary>
        [StringLength(255)]
        public string? HinhAnh { get; set; }

        /// <summary>Trạng thái hiển thị (Hoạt động, Tạm ẩn)</summary>
        [StringLength(30)]
        public string TrangThai { get; set; } = "Hoạt động";

        /// <summary>Ngày tạo bộ sưu tập</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Danh sách các điểm ghim sản phẩm trên bức ảnh bộ sưu tập này (Navigation property)</summary>
        public virtual ICollection<DiemGhimBoSuuTap> DiemGhimBoSuuTaps { get; set; } = new List<DiemGhimBoSuuTap>();
    }
}
