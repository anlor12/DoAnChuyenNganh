using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho thông tin đánh giá sản phẩm của khách hàng.
    /// </summary>
    public class DanhGia
    {
        /// <summary>Mã đánh giá (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaDanhGia { get; set; }

        /// <summary>Mã sản phẩm được đánh giá (Khóa ngoại liên kết tới bảng SanPham)</summary>
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm để đánh giá.")]
        public int MaSanPham { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }

        /// <summary>Mã người dùng thực hiện đánh giá (Khóa ngoại liên kết tới bảng NguoiDung)</summary>
        [Required(ErrorMessage = "Vui lòng chọn người dùng.")]
        public int MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }

        /// <summary>Điểm số sao đánh giá (từ 1 đến 5)</summary>
        [Required(ErrorMessage = "Vui lòng chọn số sao.")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
        public int DiemDanhGia { get; set; }

        /// <summary>Nội dung bình luận nhận xét chi tiết</summary>
        [StringLength(500, ErrorMessage = "Nội dung đánh giá tối đa 500 ký tự.")]
        public string? NoiDung { get; set; }

        /// <summary>Ngày tạo đánh giá</summary>
        public DateTime NgayTao { get; set; } = DateTime.Now;

        /// <summary>Phân loại cảm xúc bằng AI (Tích cực, Tiêu cực, Trung lập)</summary>
        [StringLength(50)]
        [Column("CamXucAi")]
        public string? Sentiment { get; set; }

        /// <summary>Từ khóa tóm tắt đánh giá do AI trích xuất</summary>
        [StringLength(200)]
        [Column("TuKhoaAi")]
        public string? AiTags { get; set; }

        /// <summary>Chiều cao của khách hàng đánh giá (cm) để người khác tham khảo form dáng</summary>
        [Range(100, 250, ErrorMessage = "Chiều cao từ 100 đến 250 cm.")]
        public double? ChieuCao { get; set; }

        /// <summary>Cân nặng của khách hàng đánh giá (kg)</summary>
        [Range(30, 200, ErrorMessage = "Cân nặng từ 30 đến 200 kg.")]
        public double? CanNang { get; set; }

        /// <summary>Size sản phẩm khách hàng đã chọn mặc thực tế</summary>
        [StringLength(20)]
        public string? SizeDaMua { get; set; }

        /// <summary>Đánh giá độ vừa vặn thực tế: "Hơi chật", "Vừa vặn hoàn hảo", "Hơi rộng"</summary>
        [StringLength(50)]
        public string? DoVuaVan { get; set; }

        /// <summary>Đường dẫn hình ảnh mặc thực tế do khách hàng chụp tải lên</summary>
        [StringLength(500)]
        public string? HinhAnh { get; set; }
    }
}
