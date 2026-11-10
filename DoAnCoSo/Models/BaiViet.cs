using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model đại diện cho Bài viết / Tin tức thời trang của hệ thống.
    /// </summary>
    [Table("BaiViets")]
    public class BaiViet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaBaiViet { get; set; }

        [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
        [StringLength(255, ErrorMessage = "Tiêu đề tối đa 255 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Đường dẫn thân thiện (Slug)")]
        public string? Slug { get; set; }

        [StringLength(500, ErrorMessage = "Tóm tắt tối đa 500 ký tự.")]
        [Display(Name = "Tóm tắt ngắn")]
        public string? TomTat { get; set; }

        [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Đường dẫn ảnh đại diện tối đa 500 ký tự.")]
        [Display(Name = "Hình ảnh đại diện")]
        public string? HinhAnh { get; set; }

        [StringLength(100, ErrorMessage = "Tên tác giả tối đa 100 ký tự.")]
        [Display(Name = "Tác giả")]
        public string? TacGia { get; set; } = "Ban biên tập";

        [StringLength(100, ErrorMessage = "Chuyên mục tối đa 100 ký tự.")]
        [Display(Name = "Chuyên mục")]
        public string? ChuyenMuc { get; set; } = "Xu hướng thời trang";

        [Display(Name = "Lượt xem")]
        public int LuotXem { get; set; } = 0;

        [Display(Name = "Bài viết nổi bật")]
        public bool NoiBat { get; set; } = false;

        [Display(Name = "Trạng thái hiển thị")]
        public bool TrangThai { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? NgayCapNhat { get; set; }
    }
}
