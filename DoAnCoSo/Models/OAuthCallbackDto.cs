namespace DoAnCoSo.Models
{
    /// <summary>
    /// DTO chứa thông tin nhận về từ quy trình xác thực OAuth một chạm (Google / Facebook)
    /// </summary>
    public class OAuthCallbackDto
    {
        /// <summary>Nhà cung cấp OAuth: Google, Facebook</summary>
        public string Provider { get; set; } = "Google";

        /// <summary>Địa chỉ Email của tài khoản</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Họ tên người dùng</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Ảnh đại diện từ Google/Facebook</summary>
        public string? Avatar { get; set; }

        /// <summary>URL chuyển hướng sau khi hoàn tất đăng nhập</summary>
        public string? ReturnUrl { get; set; }
    }
}
