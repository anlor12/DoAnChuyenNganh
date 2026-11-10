using System;
using System.Linq;
using System.Threading.Tasks;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// MarketingController: Quản trị các tính năng Marketing tự động & Giữ chân khách hàng
    /// - Cứu giỏ hàng bị bỏ quên (Abandoned Cart Recovery)
    /// - Quản lý Hệ thống Điểm tích lũy & Giới thiệu bạn bè (Loyalty Points & Referral)
    /// </summary>
    public class MarketingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AbandonedCartService _abandonedCartService;
        private readonly LoyaltyService _loyaltyService;

        public MarketingController(
            ApplicationDbContext context,
            AbandonedCartService abandonedCartService,
            LoyaltyService loyaltyService)
        {
            _context = context;
            _abandonedCartService = abandonedCartService;
            _loyaltyService = loyaltyService;
        }

        // =========================================================================
        // 1. CỨU GIỎ HÀNG BỊ BỎ QUÊN (ABANDONED CART RECOVERY)
        // =========================================================================

        /// <summary>
        /// GET: /Marketing/AbandonedCarts
        /// Hiển thị danh sách các giỏ hàng bỏ quên chưa thanh toán kèm KPI
        /// </summary>
        public async Task<IActionResult> AbandonedCarts(int minHours = 2)
        {
            if (minHours < 1) minHours = 2;

            var abandonedCarts = await _abandonedCartService.GetAbandonedCartsAsync(minHours);

            // Thống kê nhanh
            int totalCarts = abandonedCarts.Count;
            decimal totalPotentialValue = abandonedCarts.Sum(c => c.TongTien);
            int recoveredCount = abandonedCarts.Count(c => c.DaGuiNhacNho);
            int pendingCount = totalCarts - recoveredCount;

            ViewBag.TotalCarts = totalCarts;
            ViewBag.TotalPotentialValue = totalPotentialValue;
            ViewBag.RecoveredCount = recoveredCount;
            ViewBag.PendingCount = pendingCount;
            ViewBag.MinHours = minHours;

            return View(abandonedCarts);
        }

        /// <summary>
        /// POST: /Marketing/SendRecoveryEmail (AJAX)
        /// Gửi email nhắc nhở kèm voucher cho 1 giỏ hàng cụ thể
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendRecoveryEmail(int maGioHang, string voucherCode = "RECOVER10", decimal discountPercent = 10)
        {
            if (string.IsNullOrWhiteSpace(voucherCode)) voucherCode = "RECOVER10";
            if (discountPercent <= 0) discountPercent = 10;

            var (success, message) = await _abandonedCartService.SendRecoveryEmailAsync(maGioHang, voucherCode, discountPercent);
            return Json(new { success, message });
        }

        /// <summary>
        /// POST: /Marketing/AutoRecoverAll (AJAX)
        /// Quét tự động tất cả giỏ hàng bỏ quên và kích hoạt gửi email hàng loạt
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AutoRecoverAll(int minHours = 2, string voucherCode = "RECOVER10")
        {
            if (string.IsNullOrWhiteSpace(voucherCode)) voucherCode = "RECOVER10";

            var (totalScanned, recoverSent) = await _abandonedCartService.ScanAndAutoRecoverAsync(minHours, voucherCode);

            return Json(new
            {
                success = true,
                totalScanned,
                recoverSent,
                message = $"Đã quét {totalScanned} giỏ hàng bỏ quên và gửi email kích cầu thành công tới {recoverSent} khách hàng."
            });
        }

        /// <summary>
        /// GET: /Marketing/PreviewRecoveryEmail/{id}
        /// Xem trước giao diện email HTML sẽ gửi tới khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PreviewRecoveryEmail(int id)
        {
            var carts = await _abandonedCartService.GetAbandonedCartsAsync(minHours: 0);
            var cart = carts.FirstOrDefault(c => c.MaGioHang == id);

            if (cart == null)
            {
                return Content("<div style='font-family:sans-serif;padding:30px;color:red;'>Không tìm thấy giỏ hàng yêu cầu.</div>", "text/html; charset=utf-8");
            }

            string hostUrl = $"{Request.Scheme}://{Request.Host}";
            string html = _abandonedCartService.GenerateRecoveryEmailHtml(cart, cart.MaVoucherCuuGio ?? "RECOVER10", 10, $"{hostUrl}/Cart");

            return Content(html, "text/html; charset=utf-8");
        }

        // =========================================================================
        // 2. HỆ THỐNG ĐIỂM TÍCH LŨY & GIỚI THIỆU (LOYALTY & REFERRAL)
        // =========================================================================

        /// <summary>
        /// GET: /Marketing/LoyaltyProgram
        /// Trang điều khiển quản trị Điểm tích lũy và Chiến dịch Giới thiệu bạn bè
        /// </summary>
        public async Task<IActionResult> LoyaltyProgram()
        {
            // 1. Thống kê tổng quan
            int totalPointsCirculating = await _context.NguoiDungs.SumAsync(n => n.DiemTichLuy);
            
            var pointLogs = await _context.LichSuDiemThuongs
                .Include(l => l.NguoiDung)
                    .ThenInclude(u => u!.TaiKhoan)
                .Include(l => l.DonHang)
                .OrderByDescending(l => l.NgayGiaoDich)
                .ToListAsync();

            int totalPointsSpent = Math.Abs(pointLogs.Where(l => l.LoaiGiaoDich == "TieuDiemThanhToan").Sum(l => l.SoDiem));
            int totalPointsAwardedRef = pointLogs.Where(l => l.LoaiGiaoDich == "ThuongGioiThieu").Sum(l => l.SoDiem);
            int totalReferralsSuccessful = await _context.NguoiDungs.CountAsync(n => n.NguoiGioiThieuId.HasValue);

            // 2. Top thành viên có điểm cao nhất
            var topMembers = await _context.NguoiDungs
                .Include(n => n.TaiKhoan)
                .OrderByDescending(n => n.DiemTichLuy)
                .Take(8)
                .ToListAsync();

            ViewBag.TotalPointsCirculating = totalPointsCirculating;
            ViewBag.TotalPointsSpent = totalPointsSpent;
            ViewBag.TotalPointsAwardedRef = totalPointsAwardedRef;
            ViewBag.TotalReferralsSuccessful = totalReferralsSuccessful;
            ViewBag.TopMembers = topMembers;

            return View(pointLogs);
        }
    }
}
