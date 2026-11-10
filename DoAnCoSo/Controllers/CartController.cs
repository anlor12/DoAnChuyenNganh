using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// CartController: Quản lý giỏ hàng, chiết khấu VIP tự động (5%-15%), 
    /// ưu đãi quà tặng và voucher khuyến mãi.
    /// </summary>
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Helper: Tìm hoặc tạo mới Giỏ hàng của người dùng đang đăng nhập
        /// </summary>
        private async Task<GioHang?> GetOrCreateCartAsync()
        {
            var username = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (user == null || user.NguoiDung == null) return null;

            var cart = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .ThenInclude(c => c.BienTheSanPham!)
                .ThenInclude(b => b!.SanPham)
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

            return cart;
        }

        /// <summary>
        /// GET: /Cart (Index)
        /// Hiển thị giỏ hàng kèm widget Tiến trình VIP và Danh sách quà tặng đủ điều kiện
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            var cart = await GetOrCreateCartAsync();
            if (cart == null) return RedirectToAction("Login", "Auth");

            decimal subtotal = cart.ChiTietGioHangs?.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0)) ?? 0;
            decimal userSpending = user?.NguoiDung?.TongChiTieu ?? 0;

            // Tính toán mức VIP và chiết khấu
            var (appliedTier, vipDiscount, vipReason) = VipService.CalculateVipDiscount(subtotal, userSpending);
            var vipStatus = VipService.GetVipStatus(subtotal > userSpending ? subtotal : userSpending);

            // Tìm các ưu đãi quà tặng đang hoạt động
            var activeGifts = await _context.KhuyenMais
                .Where(k => k.LoaiKhuyenMai == "TangQua" && k.TrangThai && k.SoLuong > 0 && k.NgayKetThuc >= DateTime.Now)
                .OrderBy(k => k.GiaTriDonToiThieu)
                .ToListAsync();

            var eligibleGift = activeGifts
                .Where(g => subtotal >= g.GiaTriDonToiThieu)
                .OrderByDescending(g => g.GiaTriDonToiThieu)
                .FirstOrDefault();

            // Danh sách voucher giảm giá khả dụng
            var activeVouchers = await _context.KhuyenMais
                .Where(k => k.LoaiKhuyenMai == "GiamGia" && k.TrangThai && k.SoLuong > 0 && k.NgayKetThuc >= DateTime.Now && k.HienThiTrangChu)
                .OrderByDescending(k => k.PhanTramGiam)
                .ToListAsync();

            ViewBag.VipTier = appliedTier;
            ViewBag.VipDiscount = vipDiscount;
            ViewBag.VipReason = vipReason;
            ViewBag.VipStatus = vipStatus;
            ViewBag.ActiveGifts = activeGifts;
            ViewBag.EligibleGift = eligibleGift;
            ViewBag.ActiveVouchers = activeVouchers;

            return View(cart);
        }

        /// <summary>
        /// POST: /Cart/AddToCart
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart(int maBienThe = 0, int soLuong = 1, int? maSanPham = null)
        {
            var isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var cart = await GetOrCreateCartAsync();

            if (cart == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.", redirectUrl = Url.Action("Login", "Auth") });
                }
                return RedirectToAction("Login", "Auth");
            }

            // Nếu maBienThe <= 0 mà có maSanPham, tự động lấy biến thể đầu tiên của sản phẩm
            if (maBienThe <= 0 && maSanPham.HasValue && maSanPham.Value > 0)
            {
                var firstVariant = await _context.BienTheSanPhams
                    .FirstOrDefaultAsync(b => b.MaSanPham == maSanPham.Value);
                if (firstVariant != null)
                {
                    maBienThe = firstVariant.MaBienThe;
                }
            }

            if (maBienThe <= 0)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm hợp lệ." });
                }
                return RedirectToAction("Index", "SanPham");
            }

            var cartItem = cart.ChiTietGioHangs?.FirstOrDefault(c => c.MaBienThe == maBienThe);
            if (cartItem != null)
            {
                cartItem.SoLuong += soLuong;
            }
            else
            {
                cartItem = new ChiTietGioHang
                {
                    MaGioHang = cart.MaGioHang,
                    MaBienThe = maBienThe,
                    SoLuong = soLuong
                };
                _context.ChiTietGioHangs.Add(cartItem);
            }

            // Cập nhật mốc thời gian giỏ hàng để phục vụ quét giỏ hàng bỏ quên
            cart.NgayCapNhat = DateTime.Now;
            cart.DaGuiNhacNho = false;
            _context.GioHangs.Update(cart);

            await _context.SaveChangesAsync();

            if (isAjax)
            {
                var totalCount = await _context.ChiTietGioHangs
                    .Where(c => c.MaGioHang == cart.MaGioHang)
                    .SumAsync(c => c.SoLuong);
                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng thành công!", cartCount = totalCount });
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// POST: /Cart/UpdateQuantity
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int maChiTietGioHang, int soLuong)
        {
            var cartItem = await _context.ChiTietGioHangs.FindAsync(maChiTietGioHang);
            if (cartItem != null)
            {
                var cart = await _context.GioHangs.FindAsync(cartItem.MaGioHang);
                if (cart != null)
                {
                    cart.NgayCapNhat = DateTime.Now;
                    cart.DaGuiNhacNho = false;
                    _context.GioHangs.Update(cart);
                }

                if (soLuong > 0)
                {
                    cartItem.SoLuong = soLuong;
                }
                else
                {
                    _context.ChiTietGioHangs.Remove(cartItem);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// POST: /Cart/RemoveFromCart
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int maChiTietGioHang)
        {
            var cartItem = await _context.ChiTietGioHangs.FindAsync(maChiTietGioHang);
            if (cartItem != null)
            {
                var cart = await _context.GioHangs.FindAsync(cartItem.MaGioHang);
                if (cart != null)
                {
                    cart.NgayCapNhat = DateTime.Now;
                    cart.DaGuiNhacNho = false;
                    _context.GioHangs.Update(cart);
                }

                _context.ChiTietGioHangs.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// POST: /Cart/ApplyPromoCode (AJAX)
        /// Áp dụng mã khuyến mãi vào Session
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ApplyPromoCode([FromBody] string promoCode)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });
            }

            promoCode = promoCode.Trim().ToUpper();
            var promo = await _context.KhuyenMais.FirstOrDefaultAsync(k => k.Code == promoCode);

            if (promo == null)
            {
                return Json(new { success = false, message = "Mã khuyến mãi không tồn tại hoặc không hợp lệ." });
            }

            if (!promo.TrangThai)
            {
                return Json(new { success = false, message = "Mã khuyến mãi này đã tạm dừng sử dụng." });
            }

            if (promo.NgayKetThuc < DateTime.Now)
            {
                return Json(new { success = false, message = "Mã khuyến mãi đã hết hạn sử dụng." });
            }

            if (promo.SoLuong <= 0)
            {
                return Json(new { success = false, message = "Mã khuyến mãi đã hết lượt sử dụng." });
            }

            var cart = await GetOrCreateCartAsync();
            if (cart == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để áp dụng mã khuyến mãi." });
            }

            // Kiểm tra điều kiện chỉ dành cho khách hàng mới (chưa từng đặt đơn)
            if (promo.ChiDanhChoKhachMoi)
            {
                var daTungDatDon = await _context.DonHangs.AnyAsync(d => d.MaNguoiDung == cart.MaNguoiDung && d.TrangThaiDonHang != "Đã hủy");
                if (daTungDatDon)
                {
                    return Json(new { success = false, message = "Mã ưu đãi này chỉ áp dụng riêng cho khách hàng mới (đặt đơn hàng đầu tiên)." });
                }
            }

            // Kiểm tra điều kiện danh mục áp dụng
            decimal applicableSubtotal = cart.ChiTietGioHangs?.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0)) ?? 0;
            if (promo.MaLoaiSanPhamApDung.HasValue && promo.MaLoaiSanPhamApDung.Value > 0)
            {
                var itemsInCat = cart.ChiTietGioHangs?
                    .Where(c => c.BienTheSanPham?.SanPham?.MaLoaiSanPham == promo.MaLoaiSanPhamApDung.Value)
                    .ToList();

                if (itemsInCat == null || !itemsInCat.Any())
                {
                    var catName = await _context.LoaiSanPhams
                        .Where(l => l.MaLoaiSanPham == promo.MaLoaiSanPhamApDung.Value)
                        .Select(l => l.TenLoaiSanPham)
                        .FirstOrDefaultAsync();
                    return Json(new { success = false, message = $"Mã giảm giá này chỉ áp dụng cho danh mục: {catName ?? "được chỉ định"}." });
                }

                applicableSubtotal = itemsInCat.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0));
            }

            decimal subtotal = cart.ChiTietGioHangs?.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0)) ?? 0;

            if (promo.GiaTriDonToiThieu > 0 && subtotal < promo.GiaTriDonToiThieu)
            {
                return Json(new { 
                    success = false, 
                    message = $"Mã {promo.Code} chỉ áp dụng cho đơn hàng từ {promo.GiaTriDonToiThieu:N0} ₫ trở lên. Hiện tại giỏ hàng đạt {subtotal:N0} ₫." 
                });
            }

            // Lưu mã vào Session
            HttpContext.Session.SetString("AppliedPromoCode", promo.Code);

            decimal discountAmount = applicableSubtotal * promo.PhanTramGiam / 100m;
            if (promo.GiamToiDa.HasValue && promo.GiamToiDa.Value > 0 && discountAmount > promo.GiamToiDa.Value)
            {
                discountAmount = promo.GiamToiDa.Value;
            }

            return Json(new { 
                success = true, 
                code = promo.Code, 
                discountPercent = promo.PhanTramGiam, 
                discountAmount = discountAmount,
                message = $"Áp dụng mã {promo.Code} thành công! Giảm {promo.PhanTramGiam}%" 
            });
        }

        /// <summary>
        /// POST: /Cart/RemovePromoCode (AJAX)
        /// </summary>
        [HttpPost]
        public IActionResult RemovePromoCode()
        {
            HttpContext.Session.Remove("AppliedPromoCode");
            return Json(new { success = true, message = "Đã hủy áp dụng mã khuyến mãi." });
        }

        /// <summary>
        /// GET: /Checkout hoặc /Cart/Checkout
        /// </summary>
        [HttpGet]
        [Route("Checkout")]
        [Route("Cart/Checkout")]
        public async Task<IActionResult> Checkout()
        {
            var cart = await GetOrCreateCartAsync();
            if (cart == null || cart.ChiTietGioHangs == null || !cart.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index");
            }

            var username = HttpContext.Session.GetString("AdminUser");
            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            decimal subtotal = cart.ChiTietGioHangs.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0));
            decimal userSpending = user?.NguoiDung?.TongChiTieu ?? 0;

            // Tính toán chiết khấu VIP
            var (appliedTier, vipDiscount, vipReason) = VipService.CalculateVipDiscount(subtotal, userSpending);

            // Kiểm tra quà tặng hợp lệ
            var eligibleGift = await _context.KhuyenMais
                .Where(k => k.LoaiKhuyenMai == "TangQua" && k.TrangThai && k.SoLuong > 0 && k.NgayKetThuc >= DateTime.Now && subtotal >= k.GiaTriDonToiThieu)
                .OrderByDescending(k => k.GiaTriDonToiThieu)
                .FirstOrDefaultAsync();

            // Tính phí vận chuyển mặc định (GHTK)
            var (defaultShippingFee, isFreeShip, freeShipReason) = ShippingService.CalculateShippingFee("GHTK", subtotal, appliedTier.Level);

            ViewBag.NguoiDung = user?.NguoiDung;
            ViewBag.VipTier = appliedTier;
            ViewBag.VipDiscount = vipDiscount;
            ViewBag.VipReason = vipReason;
            ViewBag.EligibleGift = eligibleGift;
            ViewBag.AppliedPromoCode = HttpContext.Session.GetString("AppliedPromoCode");
            ViewBag.Carriers = ShippingService.Carriers;
            ViewBag.Provinces = ShippingService.Provinces;
            ViewBag.DefaultShippingFee = defaultShippingFee;
            ViewBag.IsFreeShip = isFreeShip;
            ViewBag.FreeShipReason = freeShipReason;
            ViewBag.FreeShippingThreshold = ShippingService.FreeShippingThreshold;

            return View(cart);
        }

        /// <summary>
        /// POST: /Cart/ProcessCheckout
        /// Lưu đơn hàng, trừ kho, tính phí ship, áp dụng chiết khấu VIP, quà tặng và cập nhật tích lũy khách hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(
            string diaChiGiaoHang, 
            string soDienThoaiNhan, 
            string ghiChu, 
            string phuongThucThanhToan, 
            string? donViVanChuyen,
            string? tinhThanh = null,
            string? quanHuyen = null,
            string? phuongXa = null,
            int diemSuDung = 0)
        {
            var cart = await GetOrCreateCartAsync();
            if (cart == null || cart.ChiTietGioHangs == null || !cart.ChiTietGioHangs.Any())
            {
                return RedirectToAction("Index");
            }

            // 1. KIỂM TRA TỒN KHO
            foreach (var item in cart.ChiTietGioHangs)
            {
                var khoHang = await _context.KhoHangs.FirstOrDefaultAsync(k => k.MaBienThe == item.MaBienThe);
                var tenSanPham = item.BienTheSanPham?.SanPham?.TenSanPham ?? "Sản phẩm";
                var kichCo = item.BienTheSanPham?.KichCo ?? "";
                var mauSac = item.BienTheSanPham?.MauSac ?? "";
                var tenHienThi = $"\"{tenSanPham} (Size: {kichCo}, Màu: {mauSac})\"";

                if (khoHang == null || khoHang.SoLuongTon < item.SoLuong)
                {
                    var tonHienTai = khoHang?.SoLuongTon ?? 0;
                    TempData["CheckoutError"] = $"Sản phẩm {tenHienThi} không đủ hàng tồn kho. Hiện chỉ còn {tonHienTai} sản phẩm trong kho.";
                    return RedirectToAction("Checkout");
                }
            }

            // 2. TRANSACTION ĐẶT HÀNG
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    decimal tongTienBanDau = cart.ChiTietGioHangs.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0));
                    
                    // Lấy profile khách hàng
                    var customer = await _context.NguoiDungs.FindAsync(cart.MaNguoiDung);
                    decimal customerSpending = customer?.TongChiTieu ?? 0;

                    // A. Tính chiết khấu VIP (5% - 15%)
                    var (vipTier, soTienGiamVip, vipReason) = VipService.CalculateVipDiscount(tongTienBanDau, customerSpending);

                    // B. Tính giảm giá Voucher (nếu có)
                    decimal soTienGiamPromo = 0;
                    string? maKhuyenMaiApDung = null;
                    var appliedPromoCode = HttpContext.Session.GetString("AppliedPromoCode");

                    if (!string.IsNullOrEmpty(appliedPromoCode))
                    {
                        var promo = await _context.KhuyenMais.FirstOrDefaultAsync(k => 
                            k.Code == appliedPromoCode && k.TrangThai && k.SoLuong > 0 && k.NgayKetThuc >= DateTime.Now);

                        if (promo != null && (promo.GiaTriDonToiThieu == 0 || tongTienBanDau >= promo.GiaTriDonToiThieu))
                        {
                            // Kiểm tra điều kiện chỉ dành cho khách mới
                            bool isValid = true;
                            if (promo.ChiDanhChoKhachMoi)
                            {
                                var daTungDatDon = await _context.DonHangs.AnyAsync(d => d.MaNguoiDung == cart.MaNguoiDung && d.TrangThaiDonHang != "Đã hủy");
                                if (daTungDatDon) isValid = false;
                            }

                            decimal tienApDung = tongTienBanDau;
                            if (promo.MaLoaiSanPhamApDung.HasValue && promo.MaLoaiSanPhamApDung.Value > 0)
                            {
                                var itemsInCat = cart.ChiTietGioHangs
                                    .Where(c => c.BienTheSanPham?.SanPham?.MaLoaiSanPham == promo.MaLoaiSanPhamApDung.Value)
                                    .ToList();
                                if (!itemsInCat.Any())
                                {
                                    isValid = false;
                                }
                                else
                                {
                                    tienApDung = itemsInCat.Sum(c => c.SoLuong * (c.BienTheSanPham?.SanPham?.Gia ?? 0));
                                }
                            }

                            if (isValid)
                            {
                                soTienGiamPromo = tienApDung * promo.PhanTramGiam / 100m;
                                if (promo.GiamToiDa.HasValue && promo.GiamToiDa.Value > 0 && soTienGiamPromo > promo.GiamToiDa.Value)
                                {
                                    soTienGiamPromo = promo.GiamToiDa.Value;
                                }
                                maKhuyenMaiApDung = promo.Code;

                                // Trừ số lượng mã
                                promo.SoLuong -= 1;
                                _context.KhuyenMais.Update(promo);
                            }
                        }
                    }

                    // C. Tự động kiểm tra quà tặng kèm
                    var eligibleGift = await _context.KhuyenMais
                        .Where(k => k.LoaiKhuyenMai == "TangQua" && k.TrangThai && k.SoLuong > 0 && k.NgayKetThuc >= DateTime.Now && tongTienBanDau >= k.GiaTriDonToiThieu)
                        .OrderByDescending(k => k.GiaTriDonToiThieu)
                        .FirstOrDefaultAsync();

                    string? quaTangKem = eligibleGift?.QuaTangKem;
                    if (eligibleGift != null)
                    {
                        eligibleGift.SoLuong -= 1;
                        _context.KhuyenMais.Update(eligibleGift);
                    }

                    // D. Tính phí vận chuyển đa cấp theo vị trí (Logistics)
                    if (string.IsNullOrEmpty(donViVanChuyen)) donViVanChuyen = "GHTK";
                    var (shippingFee, isFreeShip, freeShipReason, estTime, estDate) = ShippingService.CalculateShippingFee(
                        donViVanChuyen, 
                        tinhThanh ?? "Hà Nội", 
                        quanHuyen, 
                        tongTienBanDau, 
                        vipTier.Level);

                    // E2. Tính trừ điểm tích lũy thành viên (Loyalty Points)
                    decimal soTienGiamDiem = 0;
                    int diemThucDung = 0;
                    if (diemSuDung > 0 && customer != null && customer.DiemTichLuy > 0)
                    {
                        int maxDiemHopLe = Math.Min(customer.DiemTichLuy, diemSuDung);
                        decimal tienConLai = Math.Max(0, tongTienBanDau - soTienGiamVip - soTienGiamPromo);
                        decimal maxGiamChoPhep = tienConLai * 0.50m;
                        if (maxDiemHopLe > maxGiamChoPhep)
                        {
                            maxDiemHopLe = (int)Math.Floor(maxGiamChoPhep);
                        }

                        if (maxDiemHopLe > 0)
                        {
                            diemThucDung = maxDiemHopLe;
                            soTienGiamDiem = diemThucDung; // 1 điểm = 1 VNĐ
                            customer.DiemTichLuy -= diemThucDung;
                        }
                    }

                    // E. Tính tổng tiền thực tế = Hàng - VIP - Voucher - Điểm tích lũy + Phí Ship
                    decimal tongTienThucTe = tongTienBanDau - soTienGiamVip - soTienGiamPromo - soTienGiamDiem + shippingFee;
                    if (tongTienThucTe < 0) tongTienThucTe = 0;

                    // F. Tạo Đơn hàng
                    var donHang = new DonHang
                    {
                        MaNguoiDung = cart.MaNguoiDung,
                        NgayDat = DateTime.Now,
                        TongTien = tongTienThucTe,
                        SoTienGiam = soTienGiamPromo,
                        MaKhuyenMaiApDung = maKhuyenMaiApDung,
                        ChietKhauVip = vipTier.PhanTramGiam,
                        SoTienGiamVip = soTienGiamVip,
                        QuaTangKem = quaTangKem,
                        PhiGiaoHang = shippingFee,
                        DonViVanChuyen = donViVanChuyen,
                        DiaChiGiaoHang = diaChiGiaoHang,
                        TinhThanh = tinhThanh,
                        QuanHuyen = quanHuyen,
                        PhuongXa = phuongXa,
                        NhanVienGiaoHang = "Nguyễn Văn Tuấn (0981.234.567)",
                        SoDienThoaiNhan = soDienThoaiNhan,
                        GhiChu = ghiChu,
                        TrangThaiDonHang = "Chờ xác nhận",
                        PhuongThucThanhToan = string.IsNullOrEmpty(phuongThucThanhToan) ? "COD" : phuongThucThanhToan,
                        TrangThaiThanhToan = "Chưa thanh toán",
                        DiemSuDung = diemThucDung,
                        SoTienGiamDiem = soTienGiamDiem
                    };

                    _context.DonHangs.Add(donHang);
                    await _context.SaveChangesAsync();

                    // Tự động gán mã vận đơn giao hàng
                    donHang.MaVanDon = ShippingService.GenerateTrackingCode(donViVanChuyen, donHang.MaDonHang);
                    _context.DonHangs.Update(donHang);

                    // Ghi nhận biến động điểm tích lũy nếu có sử dụng
                    if (diemThucDung > 0)
                    {
                        _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
                        {
                            MaNguoiDung = cart.MaNguoiDung,
                            SoDiem = -diemThucDung,
                            LoaiGiaoDich = "TieuDiemThanhToan",
                            MoTa = $"Dùng {diemThucDung:N0} điểm tích lũy thanh toán đơn hàng #{donHang.MaDonHang}",
                            MaDonHang = donHang.MaDonHang,
                            NgayGiaoDich = DateTime.Now
                        });
                    }

                    // G. Lưu chi tiết đơn hàng & trừ kho
                    foreach (var item in cart.ChiTietGioHangs)
                    {
                        var chiTietDonHang = new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaBienThe = item.MaBienThe,
                            SoLuong = item.SoLuong,
                            DonGia = item.BienTheSanPham?.SanPham?.Gia ?? 0
                        };
                        _context.ChiTietDonHangs.Add(chiTietDonHang);

                        var khoHang = await _context.KhoHangs.FirstOrDefaultAsync(k => k.MaBienThe == item.MaBienThe);
                        if (khoHang != null)
                        {
                            khoHang.SoLuongTon -= item.SoLuong;
                            khoHang.NgayCapNhat = DateTime.Now;
                            _context.KhoHangs.Update(khoHang);
                        }
                    }

                    // H. Xóa giỏ hàng
                    _context.ChiTietGioHangs.RemoveRange(cart.ChiTietGioHangs);

                    // I. Cập nhật doanh số tích lũy & Hạng VIP cho tài khoản khách hàng
                    if (customer != null)
                    {
                        customer.TongChiTieu += (tongTienThucTe - shippingFee);
                        var newTier = VipService.GetTierByAmount(customer.TongChiTieu);
                        customer.HangVip = newTier.TenHang;
                        customer.DiemTichLuy += (int)((tongTienThucTe - shippingFee) / 10000);
                        _context.NguoiDungs.Update(customer);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Dọn dẹp session
                    HttpContext.Session.Remove("AppliedPromoCode");

                    // Nếu phương thức thanh toán là VNPAY hoặc MOMO -> Chuyển sang Cổng thanh toán trực tuyến
                    if (donHang.PhuongThucThanhToan == "VNPAY" || donHang.PhuongThucThanhToan == "MOMO")
                    {
                        return RedirectToAction(nameof(PaymentGateway), new { orderId = donHang.MaDonHang, method = donHang.PhuongThucThanhToan });
                    }

                    return RedirectToAction("CheckoutSuccess", new { id = donHang.MaDonHang });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["CheckoutError"] = "Có lỗi xảy ra trong quá trình đặt hàng: " + ex.Message;
                    return RedirectToAction("Checkout");
                }
            }
        }

        /// <summary>
        /// GET: /Cart/PaymentGateway
        /// Giao diện Cổng thanh toán trực tuyến (VNPAY / Ví MoMo) với mã QR động và thời gian đếm ngược
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PaymentGateway(int orderId, string method = "VNPAY")
        {
            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .ThenInclude(c => c.BienTheSanPham!)
                .ThenInclude(b => b.SanPham)
                .FirstOrDefaultAsync(d => d.MaDonHang == orderId);

            if (donHang == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Method = method.ToUpper();
            return View(donHang);
        }

        /// <summary>
        /// GET/POST: /Cart/PaymentCallback
        /// Tự động nhận kết quả thanh toán từ Cổng VNPAY / MoMo, cập nhật trạng thái đơn hàng ngay lập tức
        /// </summary>
        public async Task<IActionResult> PaymentCallback(int orderId, string method, string status = "00", string? transId = null)
        {
            var donHang = await _context.DonHangs.FindAsync(orderId);
            if (donHang == null)
            {
                return RedirectToAction("Index");
            }

            if (status == "00" || status == "success")
            {
                donHang.TrangThaiThanhToan = "DaThanhToan";
                donHang.TrangThaiDonHang = "Đang xử lý";
                donHang.MaGiaoDichThanhToan = string.IsNullOrEmpty(transId) 
                    ? $"{method.ToUpper()}-{DateTime.Now:yyyyMMddHHmmss}-{orderId}" 
                    : transId;

                _context.DonHangs.Update(donHang);
                await _context.SaveChangesAsync();

                TempData["PaymentSuccessMessage"] = $"Xác nhận thanh toán qua {method.ToUpper()} thành công! Mã giao dịch: {donHang.MaGiaoDichThanhToan}. Đơn hàng đang được tự động xử lý.";
            }

            return RedirectToAction("CheckoutSuccess", new { id = orderId });
        }

        /// <summary>
        /// GET: /Cart/CheckoutSuccess/5
        /// </summary>
        public async Task<IActionResult> CheckoutSuccess(int id)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(c => c.BienTheSanPham!)
                    .ThenInclude(b => b!.SanPham)
                .Include(d => d.NguoiDung)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (donHang == null) return NotFound();

            return View(donHang);
        }
    }
}
