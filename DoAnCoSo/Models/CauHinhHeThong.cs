using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu trữ các cấu hình chung của hệ thống (Key-Value).
    /// </summary>
    public class CauHinhHeThong
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên cấu hình không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên cấu hình tối đa 100 ký tự.")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá trị cấu hình không được để trống.")]
        public string Value { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự.")]
        public string? Description { get; set; }
    }
}
