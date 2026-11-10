using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho chi tiết của một đơn hàng (chứa thông tin về biến thể sản phẩm, số lượng và đơn giá tại thời điểm mua).
    /// </summary>
    public class ChiTietDonHang
    {
        /// <summary>Mã chi tiết đơn hàng (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaChiTietDonHang { get; set; }

        /// <summary>Mã đơn hàng (Khóa ngoại liên kết tới bảng DonHang)</summary>
        [ForeignKey("DonHang")]
        [Required(ErrorMessage = "Vui lòng chọn đơn hàng.")]
        public int MaDonHang { get; set; }

        /// <summary>Mã biến thể sản phẩm (Khóa ngoại liên kết tới bảng BienTheSanPham)</summary>
        [ForeignKey("BienTheSanPham")]
        [Required(ErrorMessage = "Vui lòng chọn biến thể sản phẩm.")]
        public int MaBienThe { get; set; }

        /// <summary>Số lượng sản phẩm đặt mua trong chi tiết này</summary>
        [Range(1, 100000, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuong { get; set; }

        /// <summary>Đơn giá của biến thể sản phẩm tại thời điểm đặt hàng</summary>
        [Range(0, 1000000000, ErrorMessage = "Đơn giá không được âm.")]
        public decimal DonGia { get; set; }

        /// <summary>Đơn hàng chứa chi tiết này (Navigation property)</summary>
        public virtual DonHang? DonHang { get; set; }

        /// <summary>Biến thể sản phẩm được mua (Navigation property)</summary>
        public virtual BienTheSanPham? BienTheSanPham { get; set; }
    }
}