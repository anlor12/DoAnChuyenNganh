using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho thông tin chung của một sản phẩm thời trang.
    /// </summary>
    public class SanPham
    {
        /// <summary>Mã sản phẩm (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaSanPham { get; set; }

        /// <summary>Mã danh mục loại sản phẩm (Khóa ngoại liên kết tới bảng LoaiSanPham)</summary>
        [ForeignKey("LoaiSanPham")]
        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm.")]
        public int MaLoaiSanPham { get; set; }

        /// <summary>Tên của sản phẩm (Ví dụ: Áo phông Basic)</summary>
        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(150, ErrorMessage = "Tên sản phẩm tối đa 150 ký tự.")]
        public string TenSanPham { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết về đặc điểm, chất liệu sản phẩm</summary>
        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? MoTa { get; set; }

        /// <summary>Giá bán cơ sở của sản phẩm (chưa tính giá riêng của từng biến thể nếu có thay đổi)</summary>
        [Range(1, 1000000000, ErrorMessage = "Giá phải lớn hơn 0.")]
        public decimal Gia { get; set; }

        /// <summary>Đường dẫn hình ảnh đại diện chính của sản phẩm (Ảnh 1)</summary>
        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh tối đa 255 ký tự.")]
        public string? HinhAnh { get; set; }

        /// <summary>Hình ảnh phụ 1 (Ảnh 2)</summary>
        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh tối đa 255 ký tự.")]
        public string? HinhAnh2 { get; set; }

        /// <summary>Hình ảnh phụ 2 (Ảnh 3)</summary>
        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh tối đa 255 ký tự.")]
        public string? HinhAnh3 { get; set; }

        /// <summary>Hình ảnh phụ 3 (Ảnh 4)</summary>
        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh tối đa 255 ký tự.")]
        public string? HinhAnh4 { get; set; }

        /// <summary>Danh sách tất cả các ảnh hợp lệ của sản phẩm để hiển thị gallery</summary>
        [NotMapped]
        public List<string> DanhSachHinhAnh
        {
            get
            {
                var list = new List<string>();
                if (!string.IsNullOrWhiteSpace(HinhAnh)) list.Add(HinhAnh);
                if (!string.IsNullOrWhiteSpace(HinhAnh2)) list.Add(HinhAnh2);
                if (!string.IsNullOrWhiteSpace(HinhAnh3)) list.Add(HinhAnh3);
                if (!string.IsNullOrWhiteSpace(HinhAnh4)) list.Add(HinhAnh4);
                return list;
            }
        }

        /// <summary>Kiểu cổ áo (Ví dụ: Cổ bẻ, Cổ tròn)</summary>
        [StringLength(50, ErrorMessage = "Kiểu cổ áo tối đa 50 ký tự.")]
        public string? KieuCoAo { get; set; }

        /// <summary>Chất liệu vải (Ví dụ: Cotton, Linen)</summary>
        [StringLength(50, ErrorMessage = "Chất liệu tối đa 50 ký tự.")]
        public string? ChatLieu { get; set; }

        /// <summary>Form dáng sản phẩm (Ví dụ: Slim fit, Regular fit)</summary>
        [StringLength(50, ErrorMessage = "Form dáng tối đa 50 ký tự.")]
        public string? FormDang { get; set; }

        /// <summary>Trạng thái kinh doanh của sản phẩm (Ví dụ: Đang bán, Ngừng bán)</summary>
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [StringLength(30, ErrorMessage = "Trạng thái tối đa 30 ký tự.")]
        public string TrangThai { get; set; } = "Đang bán";

        /// <summary>Đối tượng giới tính phù hợp (Ví dụ: Nam, Nu, Unisex)</summary>
        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        [StringLength(15, ErrorMessage = "Giới tính tối đa 15 ký tự.")]
        public string GioiTinh { get; set; } = "Unisex";

        /// <summary>Đánh dấu sản phẩm nổi bật để hiển thị ở trang chủ</summary>
        public bool IsNoiBat { get; set; } = false;

        /// <summary>Đánh dấu sản phẩm thuộc nhóm bán chạy</summary>
        public bool IsBanChay { get; set; } = false;

        /// <summary>Danh mục loại sản phẩm liên kết (Navigation property)</summary>
        public virtual LoaiSanPham? LoaiSanPham { get; set; }

        /// <summary>Danh sách các biến thể kích cỡ/màu sắc của sản phẩm này (Navigation property)</summary>
        public virtual ICollection<BienTheSanPham> BienTheSanPhams { get; set; } = new List<BienTheSanPham>();

        /// <summary>Danh sách các điểm ghim trong bộ sưu tập trỏ tới sản phẩm này (Navigation property)</summary>
        public virtual ICollection<DiemGhimBoSuuTap> DiemGhimBoSuuTaps { get; set; } = new List<DiemGhimBoSuuTap>();

        /// <summary>Danh sách đánh giá của khách hàng về sản phẩm (Navigation property)</summary>
        public virtual ICollection<DanhGia> DanhGias { get; set; } = new List<DanhGia>();
    }
}