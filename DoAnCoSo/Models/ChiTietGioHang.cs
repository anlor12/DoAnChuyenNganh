using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho từng mặt hàng chi tiết nằm trong giỏ hàng (liên kết giỏ hàng với biến thể sản phẩm và số lượng tương ứng).
    /// </summary>
    public class ChiTietGioHang
    {
        /// <summary>Mã chi tiết giỏ hàng (Khóa chính, tự động tăng)</summary>
        [Key]
        public int MaChiTietGioHang { get; set; }

        /// <summary>Mã giỏ hàng (Khóa ngoại liên kết tới bảng GioHang)</summary>
        [ForeignKey("GioHang")]
        public int MaGioHang { get; set; }

        /// <summary>Mã biến thể sản phẩm (Khóa ngoại liên kết tới bảng BienTheSanPham)</summary>
        [ForeignKey("BienTheSanPham")]
        public int MaBienThe { get; set; }

        /// <summary>Số lượng của mặt hàng này trong giỏ</summary>
        public int SoLuong { get; set; }

        /// <summary>Giỏ hàng chứa chi tiết này (Navigation property)</summary>
        public virtual GioHang? GioHang { get; set; }

        /// <summary>Thông tin biến thể sản phẩm trong giỏ (Navigation property)</summary>
        public virtual BienTheSanPham? BienTheSanPham { get; set; }
    }
}