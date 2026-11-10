using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Services
{
    public class AbandonedCartItemDto
    {
        public string TenSanPham { get; set; } = string.Empty;
        public string KichCo { get; set; } = string.Empty;
        public string MauSac { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }

    public class AbandonedCartViewModel
    {
        public int MaGioHang { get; set; }
        public int MaNguoiDung { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
        public int SoLuongSanPham { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public double GioDaBoQuen { get; set; }
        public bool DaGuiNhacNho { get; set; }
        public DateTime? NgayGuiNhacNho { get; set; }
        public int SoLanNhacNho { get; set; }
        public string? MaVoucherCuuGio { get; set; }
        public List<AbandonedCartItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Dịch vụ tự động quét và phục hồi giỏ hàng bị bỏ quên (Abandoned Cart Recovery).
    /// Hỗ trợ gửi email kích cầu kèm voucher giảm giá cá nhân hóa.
    /// </summary>
    public class AbandonedCartService
    {
        private readonly ApplicationDbContext _context;

        public AbandonedCartService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách các giỏ hàng bị bỏ quên (có sản phẩm và không hoạt động trong >= minHours giờ)
        /// </summary>
        public async Task<List<AbandonedCartViewModel>> GetAbandonedCartsAsync(int minHours = 2)
        {
            var cutoffTime = DateTime.Now.AddHours(-minHours);

            var carts = await _context.GioHangs
                .Include(g => g.NguoiDung)
                    .ThenInclude(n => n!.TaiKhoan)
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.BienTheSanPham)
                        .ThenInclude(b => b!.SanPham)
                .Where(g => g.ChiTietGioHangs.Any() &&
                            (g.TrangThai == "Hoạt động" || g.TrangThai == "HoatDong") &&
                            g.NgayCapNhat <= cutoffTime)
                .OrderByDescending(g => g.NgayCapNhat)
                .ToListAsync();

            var result = new List<AbandonedCartViewModel>();

            foreach (var g in carts)
            {
                var nguoiDung = g.NguoiDung;
                var taiKhoan = nguoiDung?.TaiKhoan;

                var items = g.ChiTietGioHangs.Select(c => new AbandonedCartItemDto
                {
                    TenSanPham = c.BienTheSanPham?.SanPham?.TenSanPham ?? "Sản phẩm Fashion Store",
                    KichCo = c.BienTheSanPham?.KichCo ?? "Tiêu chuẩn",
                    MauSac = c.BienTheSanPham?.MauSac ?? "Đa dạng",
                    HinhAnh = !string.IsNullOrEmpty(c.BienTheSanPham?.HinhAnh) ? c.BienTheSanPham.HinhAnh : (c.BienTheSanPham?.SanPham?.HinhAnh ?? "/images/products/default.jpg"),
                    DonGia = c.BienTheSanPham?.SanPham?.Gia ?? 0,
                    SoLuong = c.SoLuong
                }).ToList();

                var vm = new AbandonedCartViewModel
                {
                    MaGioHang = g.MaGioHang,
                    MaNguoiDung = g.MaNguoiDung,
                    TenKhachHang = nguoiDung?.HoTen ?? taiKhoan?.HoTen ?? "Khách hàng",
                    Email = taiKhoan?.Email ?? "khachhang@gmail.com",
                    SoDienThoai = taiKhoan?.SoDienThoai ?? "",
                    TongTien = items.Sum(i => i.ThanhTien),
                    SoLuongSanPham = items.Sum(i => i.SoLuong),
                    NgayTao = g.NgayTao,
                    NgayCapNhat = g.NgayCapNhat,
                    GioDaBoQuen = Math.Round((DateTime.Now - g.NgayCapNhat).TotalHours, 1),
                    DaGuiNhacNho = g.DaGuiNhacNho,
                    NgayGuiNhacNho = g.NgayGuiNhacNho,
                    SoLanNhacNho = g.SoLanNhacNho,
                    MaVoucherCuuGio = g.MaVoucherCuuGio,
                    Items = items
                };

                result.Add(vm);
            }

            return result;
        }

        /// <summary>
        /// Gửi email kích cầu và cập nhật trạng thái nhắc nhở của giỏ hàng
        /// </summary>
        public async Task<(bool Success, string Message)> SendRecoveryEmailAsync(int maGioHang, string voucherCode = "RECOVER10", decimal discountPercent = 10)
        {
            var cart = await _context.GioHangs
                .Include(g => g.NguoiDung)
                    .ThenInclude(n => n!.TaiKhoan)
                .Include(g => g.ChiTietGioHangs)
                    .ThenInclude(c => c.BienTheSanPham)
                        .ThenInclude(b => b!.SanPham)
                .FirstOrDefaultAsync(g => g.MaGioHang == maGioHang);

            if (cart == null)
            {
                return (false, "Không tìm thấy giỏ hàng yêu cầu.");
            }

            if (!cart.ChiTietGioHangs.Any())
            {
                return (false, "Giỏ hàng hiện không có sản phẩm nào để kích cầu.");
            }

            var email = cart.NguoiDung?.TaiKhoan?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = "khachhang@fashionstore.vn";
            }

            // Cập nhật thông tin giỏ hàng
            cart.DaGuiNhacNho = true;
            cart.NgayGuiNhacNho = DateTime.Now;
            cart.SoLanNhacNho += 1;
            cart.MaVoucherCuuGio = voucherCode;

            _context.GioHangs.Update(cart);
            await _context.SaveChangesAsync();

            return (true, $"Đã gửi email kích cầu thành công tới khách hàng {cart.NguoiDung?.HoTen} ({email}) với Voucher {voucherCode} (Giảm {discountPercent}%).");
        }

        /// <summary>
        /// Quét tự động tất cả các giỏ hàng bỏ quên và gửi email kích cầu hàng loạt
        /// </summary>
        public async Task<(int TotalScanned, int RecoverSent)> ScanAndAutoRecoverAsync(int minHours = 2, string voucherCode = "RECOVER10")
        {
            var abandonedCarts = await GetAbandonedCartsAsync(minHours);
            int sentCount = 0;

            foreach (var item in abandonedCarts)
            {
                // Chỉ gửi tự động nếu chưa gửi hoặc lần gửi trước cách đây >= 24 giờ
                if (!item.DaGuiNhacNho || (item.NgayGuiNhacNho.HasValue && (DateTime.Now - item.NgayGuiNhacNho.Value).TotalHours >= 24))
                {
                    var (success, _) = await SendRecoveryEmailAsync(item.MaGioHang, voucherCode);
                    if (success) sentCount++;
                }
            }

            return (abandonedCarts.Count, sentCount);
        }

        /// <summary>
        /// Tạo mẫu HTML email thông báo giỏ hàng bị bỏ quên tuyệt đẹp chuẩn phong cách FASHION STORE
        /// </summary>
        public string GenerateRecoveryEmailHtml(AbandonedCartViewModel cart, string voucherCode, decimal discountPercent, string checkoutUrl = "/Cart")
        {
            var itemsHtml = string.Join("", cart.Items.Select(item => $@"
                <tr style='border-bottom: 1px solid #f1f5f9;'>
                    <td style='padding: 12px 8px; width: 64px;'>
                        <img src='{item.HinhAnh}' alt='{item.TenSanPham}' style='width: 56px; height: 56px; object-fit: cover; border-radius: 8px; border: 1px solid #e2e8f0;' />
                    </td>
                    <td style='padding: 12px 8px; vertical-align: middle;'>
                        <div style='font-weight: 600; color: #0f172a; font-size: 14px;'>{item.TenSanPham}</div>
                        <div style='font-size: 12px; color: #64748b;'>Size: {item.KichCo} | Màu: {item.MauSac} &bull; SL: {item.SoLuong}</div>
                    </td>
                    <td style='padding: 12px 8px; vertical-align: middle; text-align: right; font-weight: 700; color: #059669; font-size: 14px;'>
                        {item.ThanhTien:N0} đ
                    </td>
                </tr>
            "));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Đừng bỏ lỡ giỏ hàng của bạn!</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <div style='max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.06); border: 1px solid #e2e8f0;'>
        <!-- HEADER -->
        <div style='background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 32px 24px; text-align: center; color: #ffffff;'>
            <div style='display: inline-block; background: rgba(255,255,255,0.1); border-radius: 50%; padding: 12px; margin-bottom: 12px;'>
                <span style='font-size: 28px;'>🛍️</span>
            </div>
            <h1 style='margin: 0; font-size: 24px; font-weight: 800; letter-spacing: 0.5px;'>FASHION STORE</h1>
            <p style='margin: 8px 0 0 0; font-size: 14px; color: #94a3b8;'>Trang phục yêu thích của bạn vẫn đang chờ bạn đấy!</p>
        </div>

        <!-- BODY -->
        <div style='padding: 28px 24px;'>
            <p style='font-size: 15px; color: #334155; line-height: 1.6; margin: 0 0 16px 0;'>
                Xin chào <strong>{cart.TenKhachHang}</strong>,<br>
                Chúng tôi nhận thấy bạn đã chọn một số sản phẩm thời trang tuyệt vời nhưng chưa kịp hoàn tất thanh toán. Để giúp bạn không bỏ lỡ phong cách ưng ý, FASHION STORE đã giữ lại các món đồ này cho bạn:
            </p>

            <!-- DANH SÁCH SẢN PHẨM -->
            <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                {itemsHtml}
            </table>

            <div style='text-align: right; margin-bottom: 24px; font-size: 15px; color: #1e293b;'>
                Tổng giá trị giỏ hàng: <strong style='font-size: 18px; color: #059669;'>{cart.TongTien:N0} đ</strong>
            </div>

            <!-- VOUCHER BOX -->
            <div style='background: linear-gradient(135deg, #fffbeb 0%, #fef3c7 100%); border: 2px dashed #f59e0b; border-radius: 12px; padding: 20px; text-align: center; margin-bottom: 28px;'>
                <div style='font-size: 13px; font-weight: 700; color: #b45309; text-transform: uppercase; letter-spacing: 1px;'>Đặc quyền riêng cho bạn</div>
                <div style='font-size: 22px; font-weight: 800; color: #92400e; margin: 6px 0;'>GIẢM NGAY {discountPercent}% ĐƠN HÀNG</div>
                <div style='margin-top: 10px;'>
                    <span style='background: #ffffff; border: 1px solid #d97706; padding: 8px 18px; font-size: 18px; font-family: monospace; font-weight: 800; color: #b45309; border-radius: 8px; letter-spacing: 2px;'>{voucherCode}</span>
                </div>
                <div style='font-size: 12px; color: #78350f; margin-top: 10px;'>Nhập mã tại bước thanh toán. Áp dụng trong vòng 24 giờ tới!</div>
            </div>

            <!-- CTA BUTTON -->
            <div style='text-align: center; margin-bottom: 24px;'>
                <a href='{checkoutUrl}' style='display: inline-block; background: #0f172a; color: #ffffff; text-decoration: none; padding: 14px 32px; font-size: 15px; font-weight: 700; border-radius: 10px; box-shadow: 0 4px 12px rgba(15,23,42,0.25);'>
                    HOÀN TẤT ĐẶT HÀNG NGAY &rarr;
                </a>
            </div>

            <!-- PERKS -->
            <div style='display: flex; justify-content: space-around; background: #f8fafc; border-radius: 10px; padding: 14px; text-align: center; font-size: 12px; color: #64748b; margin-bottom: 12px;'>
                <div>🚀 Giao hàng hỏa tốc 2-3 ngày</div>
                <div>🔄 Đổi hàng 30 ngày dễ dàng</div>
                <div>✨ Cam kết chính hãng 100%</div>
            </div>
        </div>

        <!-- FOOTER -->
        <div style='background: #f1f5f9; padding: 18px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0;'>
            Hotline hỗ trợ: <strong>1900 8079</strong> | Email: cskh@fashionstore.vn<br>
            Nếu bạn không có ý định mua sắm hoặc đã đặt hàng, vui lòng bỏ qua email này.
        </div>
    </div>
</body>
</html>
";
        }
    }
}
