using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng dành cho khách hàng: Xem/Cập nhật Profile cá nhân, Đổi mật khẩu và Xem Lịch sử đơn hàng.
    /// </summary>
    public class KhachHangsController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor inject DbContext để tương tác với Cơ sở dữ liệu.
        /// </summary>
        public KhachHangsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hàm nội bộ (Helper) lấy thông tin người dùng (NguoiDung) hiện tại dựa vào Session đăng nhập.
        /// </summary>
        /// <returns>Đối tượng NguoiDung kèm TaiKhoan, hoặc null nếu chưa đăng nhập</returns>
        private NguoiDung? GetCurrentUser()
        {
            var tenDangNhap = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(tenDangNhap)) return null;

            var taiKhoan = _context.TaiKhoans.FirstOrDefault(t => t.TenDangNhap == tenDangNhap);
            if (taiKhoan == null) return null;

            return _context.NguoiDungs
                .Include(n => n.TaiKhoan)
                .FirstOrDefault(n => n.MaTaiKhoan == taiKhoan.MaTaiKhoan);
        }

        // ============================================================
        //  PROFILE — Thông tin cá nhân
        // ============================================================
        
        /// <summary>
        /// GET: KhachHangs/Profile
        /// Hiển thị trang thông tin cá nhân của khách hàng.
        /// </summary>
        [HttpGet]
        public IActionResult Profile()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Auth");

            // Truyền thêm thông tin tài khoản đăng nhập ra ViewBag để hiển thị readonly trên View
            ViewBag.Email = user.TaiKhoan?.Email;
            ViewBag.SoDienThoai = user.TaiKhoan?.SoDienThoai;
            ViewBag.TenDangNhap = user.TaiKhoan?.TenDangNhap;
            ViewBag.VipStatus = DoAnCoSo.Services.VipService.GetVipStatus(user.TongChiTieu);

            return View(user);
        }

        /// <summary>
        /// POST: KhachHangs/Profile
        /// Xử lý cập nhật thông tin cá nhân (Họ tên, giới tính, chiều cao, cân nặng, địa chỉ...) và số điện thoại.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(NguoiDung model, string? soDienThoai)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return RedirectToAction("Login", "Auth");

            // Cập nhật thông tin cá nhân trong NguoiDung
            currentUser.HoTen = model.HoTen;
            currentUser.GioiTinh = model.GioiTinh;
            currentUser.NgaySinh = model.NgaySinh;
            currentUser.DiaChi = model.DiaChi;
            currentUser.ChieuCao = model.ChieuCao;
            currentUser.CanNang = model.CanNang;

            // Cập nhật số điện thoại trong TaiKhoan nếu có thay đổi
            if (currentUser.TaiKhoan != null && soDienThoai != null)
            {
                currentUser.TaiKhoan.SoDienThoai = string.IsNullOrWhiteSpace(soDienThoai) ? null : soDienThoai.Trim();
                _context.Update(currentUser.TaiKhoan);
            }

            try
            {
                _context.Update(currentUser);
                await _context.SaveChangesAsync();
                TempData["ProfileSuccess"] = "Cập nhật thông tin cá nhân thành công!";
            }
            catch (Exception ex)
            {
                TempData["ProfileError"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        // ============================================================
        //  ĐỔI MẬT KHẨU
        // ============================================================
        
        /// <summary>
        /// POST: KhachHangs/DoiMatKhau
        /// Thực hiện đổi mật khẩu cho tài khoản khách hàng.
        /// </summary>
        /// <param name="matKhauCu">Mật khẩu cũ của tài khoản</param>
        /// <param name="matKhauMoi">Mật khẩu mới</param>
        /// <param name="xacNhanMatKhau">Nhập lại mật khẩu mới để kiểm tra trùng khớp</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            var tenDangNhap = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(tenDangNhap))
                return RedirectToAction("Login", "Auth");

            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

            if (taiKhoan == null)
            {
                TempData["PasswordError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(Profile));
            }

            // Kiểm tra mật khẩu cũ (lưu ý: dự án hiện tại đang lưu plaintext, nếu mã hóa mật khẩu cần sửa lại)
            if (taiKhoan.MatKhau != matKhauCu)
            {
                TempData["PasswordError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction(nameof(Profile));
            }

            // Kiểm tra độ dài mật khẩu mới
            if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 6)
            {
                TempData["PasswordError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction(nameof(Profile));
            }

            // Kiểm tra xác nhận mật khẩu mới có trùng khớp không
            if (matKhauMoi != xacNhanMatKhau)
            {
                TempData["PasswordError"] = "Xác nhận mật khẩu không khớp.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                taiKhoan.MatKhau = matKhauMoi;
                _context.Update(taiKhoan);
                await _context.SaveChangesAsync();
                TempData["PasswordSuccess"] = "Đổi mật khẩu thành công!";
            }
            catch (Exception ex)
            {
                TempData["PasswordError"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        // ============================================================
        //  LỊCH SỬ ĐƠN HÀNG
        // ============================================================
        
        /// <summary>
        /// GET: KhachHangs/LichSuDonHang
        /// Hiển thị lịch sử toàn bộ các đơn hàng đã đặt của người dùng hiện tại.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LichSuDonHang()
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Auth");

            var donHangs = await _context.DonHangs
                .Where(d => d.MaNguoiDung == user.MaNguoiDung)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.VipStatus = DoAnCoSo.Services.VipService.GetVipStatus(user.TongChiTieu);

            return View(donHangs);
        }

        /// <summary>
        /// GET: KhachHangs/ChiTietDonHang/5
        /// Hiển thị thông tin hóa đơn và các sản phẩm đã mua của một đơn hàng theo ID.
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        [HttpGet]
        public async Task<IActionResult> ChiTietDonHang(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Auth");

            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .ThenInclude(c => c.BienTheSanPham!)
                .ThenInclude(b => b!.SanPham)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaNguoiDung == user.MaNguoiDung);

            if (donHang == null) return NotFound();

            // Lấy danh sách các sản phẩm đã được khách hàng đánh giá để hiển thị trên view chi tiết đơn hàng
            var maSanPhamsDaDanhGia = await _context.DanhGias
                .Where(dg => dg.MaNguoiDung == user.MaNguoiDung)
                .Select(dg => dg.MaSanPham)
                .ToListAsync();

            ViewBag.MaSanPhamsDaDanhGia = maSanPhamsDaDanhGia;

            // Load thông tin các đánh giá cụ thể (nếu có) của khách hàng cho đơn này
            var danhGiasCuaKhach = (await _context.DanhGias
                .Where(dg => dg.MaNguoiDung == user.MaNguoiDung)
                .ToListAsync())
                .GroupBy(dg => dg.MaSanPham)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.NgayTao).First());
            ViewBag.DanhGiasCuaKhach = danhGiasCuaKhach;

            return View(donHang);
        }

        /// <summary>
        /// POST: KhachHangs/ThemDanhGia
        /// Tiếp nhận và lưu đánh giá sản phẩm từ phía khách hàng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDanhGia(int maSanPham, int diemDanhGia, string? noiDung, int maDonHang)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Auth");

            // 1. Kiểm tra đơn hàng có tồn tại và thuộc về user không
            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .ThenInclude(ct => ct.BienTheSanPham!)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == user.MaNguoiDung);

            if (donHang == null)
            {
                TempData["ReviewError"] = "Không tìm thấy đơn hàng hợp lệ.";
                return RedirectToAction(nameof(LichSuDonHang));
            }

            // Kiểm tra trạng thái hoàn thành
            var trangThai = donHang.TrangThaiDonHang.Trim();
            if (trangThai != "Hoàn thành" && trangThai != "HoanThanh")
            {
                TempData["ReviewError"] = "Bạn chỉ có thể đánh giá sản phẩm của đơn hàng đã hoàn thành.";
                return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
            }

            // 2. Kiểm tra sản phẩm có thuộc đơn hàng này không
            var coSanPham = donHang.ChiTietDonHangs != null && donHang.ChiTietDonHangs
                .Any(ct => ct.BienTheSanPham != null && ct.BienTheSanPham.MaSanPham == maSanPham);

            if (!coSanPham)
            {
                TempData["ReviewError"] = "Sản phẩm này không nằm trong đơn hàng của bạn.";
                return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
            }

            // 3. Kiểm tra xem người dùng đã đánh giá sản phẩm này chưa (chỉ được đánh giá 1 lần)
            var daDanhGia = await _context.DanhGias
                .AnyAsync(dg => dg.MaNguoiDung == user.MaNguoiDung && dg.MaSanPham == maSanPham);

            if (daDanhGia)
            {
                TempData["ReviewError"] = "Bạn đã đánh giá sản phẩm này trước đó.";
                return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
            }

            // 4. Lưu đánh giá mới
            if (diemDanhGia < 1 || diemDanhGia > 5)
            {
                TempData["ReviewError"] = "Điểm đánh giá phải từ 1 đến 5 sao.";
                return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
            }

            try
            {
                var danhGia = new DanhGia
                {
                    MaSanPham = maSanPham,
                    MaNguoiDung = user.MaNguoiDung,
                    DiemDanhGia = diemDanhGia,
                    NoiDung = noiDung?.Trim(),
                    NgayTao = DateTime.Now
                };

                _context.DanhGias.Add(danhGia);
                await _context.SaveChangesAsync();

                TempData["ReviewSuccess"] = "Gửi đánh giá sản phẩm thành công. Cảm ơn phản hồi của bạn!";
            }
            catch (Exception ex)
            {
                TempData["ReviewError"] = "Có lỗi xảy ra khi lưu đánh giá: " + ex.Message;
            }

            return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
        }

        /// <summary>
        /// POST: KhachHangs/HuyDonHang
        /// Cho phép khách hàng tự hủy đơn hàng khi đơn còn ở trạng thái "Chờ xác nhận".
        /// Tự động hoàn lại tồn kho và voucher đã sử dụng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDonHang(int maDonHang, string? lyDoHuy)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Auth");

            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == user.MaNguoiDung);

            if (donHang == null)
            {
                TempData["OrderError"] = "Không tìm thấy đơn hàng cần hủy.";
                return RedirectToAction(nameof(LichSuDonHang));
            }

            var trangThai = donHang.TrangThaiDonHang.Trim();
            if (trangThai != "Chờ xác nhận" && trangThai != "ChoXacNhan")
            {
                TempData["OrderError"] = "Chỉ có thể hủy đơn hàng khi đơn đang ở trạng thái 'Chờ xác nhận'.";
                return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Hoàn lại số lượng tồn kho
                if (donHang.ChiTietDonHangs != null)
                {
                    foreach (var item in donHang.ChiTietDonHangs)
                    {
                        var kho = await _context.KhoHangs.FirstOrDefaultAsync(k => k.MaBienThe == item.MaBienThe);
                        if (kho != null)
                        {
                            kho.SoLuongTon += item.SoLuong;
                            kho.NgayCapNhat = DateTime.Now;
                            _context.KhoHangs.Update(kho);
                        }
                    }
                }

                // 2. Hoàn lại số lượng voucher nếu có áp dụng
                if (!string.IsNullOrEmpty(donHang.MaKhuyenMaiApDung))
                {
                    var promo = await _context.KhuyenMais.FirstOrDefaultAsync(k => k.Code == donHang.MaKhuyenMaiApDung);
                    if (promo != null)
                    {
                        promo.SoLuong += 1;
                        _context.KhuyenMais.Update(promo);
                    }
                }

                // 3. Giảm trừ doanh số tích lũy & điểm VIP nếu đã được tính lúc đặt hàng
                var tienHang = donHang.TongTien - donHang.PhiGiaoHang;
                if (user.TongChiTieu >= tienHang && tienHang > 0)
                {
                    user.TongChiTieu -= tienHang;
                    var tier = DoAnCoSo.Services.VipService.GetTierByAmount(user.TongChiTieu);
                    user.HangVip = tier.TenHang;
                    _context.NguoiDungs.Update(user);
                }

                // Hoàn lại điểm tích lũy đã dùng khi đặt đơn
                if (donHang.DiemSuDung > 0)
                {
                    user.DiemTichLuy += donHang.DiemSuDung;
                    _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
                    {
                        MaNguoiDung = user.MaNguoiDung,
                        SoDiem = donHang.DiemSuDung,
                        LoaiGiaoDich = "HoanDiem",
                        MoTa = $"Hoàn trả {donHang.DiemSuDung:N0} điểm do khách hủy đơn hàng #{donHang.MaDonHang}",
                        MaDonHang = donHang.MaDonHang,
                        NgayGiaoDich = DateTime.Now
                    });
                    _context.NguoiDungs.Update(user);
                }

                // 4. Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã hủy";
                if (!string.IsNullOrWhiteSpace(lyDoHuy))
                {
                    donHang.GhiChu = (string.IsNullOrEmpty(donHang.GhiChu) ? "" : donHang.GhiChu + " | ") + "Lý do khách hủy: " + lyDoHuy.Trim();
                }

                _context.DonHangs.Update(donHang);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["OrderSuccess"] = $"Đã hủy đơn hàng #{donHang.MaDonHang} thành công. Số lượng sản phẩm và điểm thưởng (nếu có) đã được hoàn trả lại.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["OrderError"] = "Có lỗi xảy ra khi hủy đơn hàng: " + ex.Message;
            }

            return RedirectToAction(nameof(ChiTietDonHang), new { id = maDonHang });
        }

        // ============================================================
        //  LOYALTY POINTS & REFERRAL PROGRAM (Điểm tích lũy & Giới thiệu)
        // ============================================================

        /// <summary>
        /// GET: KhachHangs/DiemThuong
        /// Hiển thị Trang Điểm tích lũy thành viên & Giới thiệu bạn bè
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DiemThuong([FromServices] DoAnCoSo.Services.LoyaltyService loyaltyService)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Login", "Auth");

            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            var summary = await loyaltyService.GetLoyaltySummaryAsync(user.MaNguoiDung, baseUrl);

            ViewBag.User = user;
            ViewBag.VipStatus = DoAnCoSo.Services.VipService.GetVipStatus(user.TongChiTieu);

            return View(summary);
        }

        /// <summary>
        /// POST: KhachHangs/NhanDiemTestAjax
        /// Cấp nhanh +50.000 điểm trải nghiệm để kiểm thử tính năng tiêu điểm
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> NhanDiemTestAjax()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để nhận điểm thưởng." });
            }

            user.DiemTichLuy += 50000;
            _context.NguoiDungs.Update(user);
            _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
            {
                MaNguoiDung = user.MaNguoiDung,
                SoDiem = 50000,
                LoaiGiaoDich = "ThuongDacBiet",
                MoTa = "Quà tặng 50.000 điểm thưởng thành viên trải nghiệm",
                NgayGiaoDich = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, newPoints = user.DiemTichLuy, message = "Đã nhận thành công +50.000 điểm thưởng!" });
        }
    }
}
