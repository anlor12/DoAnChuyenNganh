using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// OutfitController: Quản lý tính năng Phối đồ Mix & Match (Outfit Builder / Mua theo Combo).
    /// Cho phép khách hàng tương tác kéo-chọn Áo + Quần + Phụ kiện trên cùng khung hiển thị trực quan,
    /// tự động tính chiết khấu combo (2 món giảm 5%, 3 món giảm 10%) và bấm "Mua trọn bộ" chỉ với 1 click.
    /// </summary>
    public class OutfitController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OutfitController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /Outfit hoặc /BoSuuTaps/OutfitBuilder
        /// Giao diện phòng thử đồ tương tác (Outfit Builder Canvas)
        /// </summary>
        [HttpGet]
        [Route("Outfit")]
        [Route("Outfit/Index")]
        [Route("BoSuuTaps/OutfitBuilder")]
        public async Task<IActionResult> Index(int? topId, int? bottomId, int? accessoryId, string? gender = "Nam")
        {
            // 1. Tải danh sách Áo (Tops)
            var tops = await _context.SanPhams
                .Include(s => s.LoaiSanPham).ThenInclude(l => l!.LoaiCha)
                .Include(s => s.BienTheSanPhams)
                .Where(s => (s.TrangThai == "Đang bán" || s.TrangThai == "DangBan") &&
                            (s.LoaiSanPham != null && (s.LoaiSanPham.TenLoaiSanPham.Contains("Áo") || (s.LoaiSanPham.LoaiCha != null && s.LoaiSanPham.LoaiCha.TenLoaiSanPham == "Áo"))))
                .OrderByDescending(s => s.IsNoiBat)
                .ThenByDescending(s => s.MaSanPham)
                .Take(24)
                .ToListAsync();

            // 2. Tải danh sách Quần (Bottoms)
            var bottoms = await _context.SanPhams
                .Include(s => s.LoaiSanPham).ThenInclude(l => l!.LoaiCha)
                .Include(s => s.BienTheSanPhams)
                .Where(s => (s.TrangThai == "Đang bán" || s.TrangThai == "DangBan") &&
                            (s.LoaiSanPham != null && (s.LoaiSanPham.TenLoaiSanPham.Contains("Quần") || (s.LoaiSanPham.LoaiCha != null && s.LoaiSanPham.LoaiCha.TenLoaiSanPham == "Quần"))))
                .OrderByDescending(s => s.IsNoiBat)
                .ThenByDescending(s => s.MaSanPham)
                .Take(24)
                .ToListAsync();

            // 3. Tải danh sách Phụ kiện (Accessories)
            var accessories = await _context.SanPhams
                .Include(s => s.LoaiSanPham).ThenInclude(l => l!.LoaiCha)
                .Include(s => s.BienTheSanPhams)
                .Where(s => (s.TrangThai == "Đang bán" || s.TrangThai == "DangBan") &&
                            (s.LoaiSanPham != null && (s.LoaiSanPham.TenLoaiSanPham.Contains("Phụ Kiện") || s.LoaiSanPham.TenLoaiSanPham.Contains("Giày") || (s.LoaiSanPham.LoaiCha != null && (s.LoaiSanPham.LoaiCha.TenLoaiSanPham == "Phụ Kiện" || s.LoaiSanPham.LoaiCha.TenLoaiSanPham == "Giày")))))
                .OrderByDescending(s => s.MaSanPham)
                .Take(24)
                .ToListAsync();

            // Nếu danh mục phụ kiện chưa đủ, lấy thêm một số sản phẩm khác để dự phòng
            if (accessories.Count < 3)
            {
                var extra = await _context.SanPhams
                    .Include(s => s.LoaiSanPham)
                    .Include(s => s.BienTheSanPhams)
                    .Where(s => (s.TrangThai == "Đang bán" || s.TrangThai == "DangBan") && !tops.Select(t => t.MaSanPham).Contains(s.MaSanPham) && !bottoms.Select(b => b.MaSanPham).Contains(s.MaSanPham))
                    .Take(6)
                    .ToListAsync();
                accessories.AddRange(extra);
            }

            // Chọn sản phẩm mặc định
            var defaultTop = tops.FirstOrDefault(t => t.MaSanPham == topId) ?? tops.FirstOrDefault();
            var defaultBottom = bottoms.FirstOrDefault(b => b.MaSanPham == bottomId) ?? bottoms.FirstOrDefault();
            var defaultAcc = accessories.FirstOrDefault(a => a.MaSanPham == accessoryId) ?? accessories.FirstOrDefault();

            // 4. Lấy chi tiết các sản phẩm thuộc 5 Look Lookbook mẫu của shop
            var lookProductIds = new List<int> { 2, 3, 4, 30, 41, 53, 54, 55, 56, 58 };
            var curatedProducts = await _context.SanPhams
                .Include(s => s.LoaiSanPham)
                .Include(s => s.BienTheSanPhams)
                .Where(s => lookProductIds.Contains(s.MaSanPham))
                .ToDictionaryAsync(s => s.MaSanPham);

            ViewBag.Tops = tops;
            ViewBag.Bottoms = bottoms;
            ViewBag.Accessories = accessories;
            ViewBag.CuratedProducts = curatedProducts;

            ViewBag.SelectedTop = defaultTop;
            ViewBag.SelectedBottom = defaultBottom;
            ViewBag.SelectedAcc = defaultAcc;
            ViewBag.Gender = gender;

            return View();
        }

        /// <summary>
        /// POST: /Outfit/AddComboToCart
        /// Thêm trọn bộ các món đã chọn vào giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddComboToCart([FromBody] ComboAddToCartRequest request)
        {
            var isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true;

            var username = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để lưu trọn bộ combo vào giỏ hàng.", redirectUrl = Url.Action("Login", "Auth") });
            }

            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (user?.NguoiDung == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản." });
            }

            if (request == null || request.VariantIds == null || !request.VariantIds.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 món trong bộ trang phục." });
            }

            var cart = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .FirstOrDefaultAsync(g => g.MaNguoiDung == user.NguoiDung.MaNguoiDung);

            if (cart == null)
            {
                cart = new GioHang
                {
                    MaNguoiDung = user.NguoiDung.MaNguoiDung,
                    NgayTao = DateTime.Now,
                    TrangThai = "HoatDong"
                };
                _context.GioHangs.Add(cart);
                await _context.SaveChangesAsync();
                cart.ChiTietGioHangs = new List<ChiTietGioHang>();
            }

            int addedCount = 0;
            foreach (var variantId in request.VariantIds.Distinct())
            {
                if (variantId <= 0) continue;

                var existingItem = cart.ChiTietGioHangs?.FirstOrDefault(c => c.MaBienThe == variantId);
                if (existingItem != null)
                {
                    existingItem.SoLuong += 1;
                }
                else
                {
                    var newItem = new ChiTietGioHang
                    {
                        MaGioHang = cart.MaGioHang,
                        MaBienThe = variantId,
                        SoLuong = 1
                    };
                    _context.ChiTietGioHangs.Add(newItem);
                }
                addedCount++;
            }

            await _context.SaveChangesAsync();

            var totalItemsInCart = await _context.ChiTietGioHangs
                .Where(c => c.MaGioHang == cart.MaGioHang)
                .SumAsync(c => c.SoLuong);

            int discountPercent = request.VariantIds.Count >= 3 ? 10 : (request.VariantIds.Count == 2 ? 5 : 0);

            return Json(new
            {
                success = true,
                message = $"Đã thêm thành công {addedCount} món trong bộ trang phục vào giỏ hàng! (Ưu đãi Combo: Giảm {discountPercent}%)",
                cartCount = totalItemsInCart,
                discountPercent = discountPercent
            });
        }
    }

    public class ComboAddToCartRequest
    {
        public List<int> VariantIds { get; set; } = new List<int>();
    }
}
