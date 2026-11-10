using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu trữ yêu cầu Đăng ký nhận thông báo khi có hàng lại (Back-in-Stock Alert).
    /// </summary>
    [Table("ThongBaoHetHangs")]
    public class ThongBaoHetHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaThongBao { get; set; }

        [Required]
        public int MaSanPham { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }

        public int? MaBienThe { get; set; }

        [ForeignKey("MaBienThe")]
        public virtual BienTheSanPham? BienTheSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại để nhận thông báo.")]
        [StringLength(150, ErrorMessage = "Thông tin liên hệ tối đa 150 ký tự.")]
        public string LienHe { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? MauSac { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.Now;

        public bool DaThongBao { get; set; } = false;

        public DateTime? NgayThongBao { get; set; }
    }
}
