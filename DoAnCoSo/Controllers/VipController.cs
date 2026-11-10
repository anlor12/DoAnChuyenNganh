using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý Chính sách tích VIP & Chiết khấu khách hàng dành cho Admin.
    /// </summary>
    [AdminAuthorize]
    public class VipController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VipController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /Vip
        /// Dashboard chính sách VIP và danh sách khách hàng xếp hạng VIP
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var tiers = VipService.GetAllTiers();

            // Lấy danh sách khách hàng kèm số đơn hàng và tổng tiền
            var customers = await _context.NguoiDungs
                .Include(n => n.TaiKhoan)
                .Include(n => n.DonHangs)
                .ToListAsync();

            // Đồng bộ lại tổng chi tiêu thực tế nếu chưa được tính
            foreach (var cus in customers)
            {
                var totalSpentFromOrders = cus.DonHangs?
                    .Where(d => d.TrangThaiDonHang != "Đã hủy")
                    .Sum(d => d.TongTien) ?? 0;

                if (totalSpentFromOrders > cus.TongChiTieu)
                {
                    cus.TongChiTieu = totalSpentFromOrders;
                    var tier = VipService.GetTierByAmount(cus.TongChiTieu);
                    cus.HangVip = tier.TenHang;
                }
            }
            await _context.SaveChangesAsync();

            // Tổng chiết khấu VIP đã trao cho khách hàng
            var totalVipDiscounts = await _context.DonHangs
                .Where(d => d.TrangThaiDonHang != "Đã hủy")
                .SumAsync(d => d.SoTienGiamVip);

            ViewBag.Tiers = tiers;
            ViewBag.TotalVipDiscounts = totalVipDiscounts;
            ViewBag.Customers = customers;

            return View(tiers);
        }

        /// <summary>
        /// POST: /Vip/RecalculateAll
        /// Tính toán lại toàn bộ hạng VIP của tất cả khách hàng dựa trên lịch sử mua hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateAll()
        {
            var customers = await _context.NguoiDungs
                .Include(n => n.DonHangs)
                .ToListAsync();

            foreach (var cus in customers)
            {
                var totalSpent = cus.DonHangs?
                    .Where(d => d.TrangThaiDonHang != "Đã hủy")
                    .Sum(d => d.TongTien) ?? 0;

                cus.TongChiTieu = totalSpent;
                var tier = VipService.GetTierByAmount(totalSpent);
                cus.HangVip = tier.TenHang;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đồng bộ và tính toán lại hạng VIP cho toàn bộ khách hàng!";
            return RedirectToAction(nameof(Index));
        }
    }
}
