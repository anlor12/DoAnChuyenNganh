using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// AuthController: Quản lý các chức năng xác thực người dùng bao gồm:
    /// - Đăng nhập truyền thống & Đăng nhập một chạm OAuth (Google / Facebook)
    /// - Đăng ký tài khoản mới & tích hợp mã giới thiệu, quà chào mừng
    /// - Đăng xuất và ghi vết nhật ký kiểm soát an toàn bảo mật (Audit Logs)
    /// </summary>
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public AuthController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// GET: /Auth/Login
        /// Hiển thị giao diện trang Đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// POST: /Auth/Login
        /// Xử lý dữ liệu gửi lên từ Form Đăng nhập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string tenDangNhap, string matKhau, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // 1. Tìm tài khoản trùng khớp trong Cơ sở dữ liệu
            var user = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap.Trim() == tenDangNhap.Trim()
                                     && t.MatKhau.Trim() == matKhau.Trim());

            // 2. Kiểm tra nếu tìm thấy tài khoản
            if (user != null)
            {
                // Kiểm tra trạng thái tài khoản
                if (user.TrangThai == "Bị khóa")
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị tạm khóa. Vui lòng liên hệ bộ phận hỗ trợ.";
                    await _auditLogService.LogAsync("DANG_NHAP_BI_KHOA", $"Cố gắng đăng nhập tài khoản bị khóa: {user.TenDangNhap}", "Bảo mật", null, user.TenDangNhap, "Cao");
                    return View();
                }

                // Lưu tên đăng nhập và vai trò vào Session
                HttpContext.Session.SetString("AdminUser", user.TenDangNhap.Trim());
                HttpContext.Session.SetString("VaiTro", user.VaiTro?.Trim() ?? "Khách hàng");

                // Ghi nhật ký bảo mật (Audit Log)
                await _auditLogService.LogAsync("DANG_NHAP", $"Đăng nhập thành công tài khoản: {user.TenDangNhap} ({user.VaiTro})", "Bảo mật", null, user.TenDangNhap, "Thấp");

                // Điều hướng dựa trên returnUrl hoặc Vai trò
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (user.VaiTro?.Trim() == "Quản trị viên")
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                return RedirectToAction("Index", "Home");
            }

            // 3. Nếu sai thông tin, ghi nhật ký cảnh báo và báo lỗi ra giao diện
            await _auditLogService.LogAsync("DANG_NHAP_THAT_BAI", $"Đăng nhập thất bại với tên đăng nhập: {tenDangNhap}", "Bảo mật", null, tenDangNhap, "Trung bình");
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// POST: /Auth/LogoutPost
        /// Xử lý đăng xuất bằng phương thức POST để đảm bảo an toàn bảo mật
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LogoutPost()
        {
            var currentUser = HttpContext.Session.GetString("AdminUser");
            if (!string.IsNullOrEmpty(currentUser))
            {
                await _auditLogService.LogAsync("DANG_XUAT", $"Đăng xuất khỏi hệ thống: {currentUser}", "Bảo mật", null, currentUser, "Thấp");
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        /// <summary>
        /// GET: /Auth/Logout
        /// Hỗ trợ đăng xuất bằng phương thức GET
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var currentUser = HttpContext.Session.GetString("AdminUser");
            if (!string.IsNullOrEmpty(currentUser))
            {
                await _auditLogService.LogAsync("DANG_XUAT", $"Đăng xuất khỏi hệ thống: {currentUser}", "Bảo mật", null, currentUser, "Thấp");
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        /// <summary>
        /// GET: /Auth/Register
        /// Hiển thị giao diện trang Đăng ký tài khoản mới, hỗ trợ nhận mã giới thiệu từ URL
        /// </summary>
        [HttpGet]
        public IActionResult Register(string? @ref = null, string? refCode = null, string? returnUrl = null)
        {
            ViewBag.RefCode = !string.IsNullOrEmpty(@ref) ? @ref : refCode;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// POST: /Auth/Register
        /// Xử lý luồng dữ liệu đăng ký tài khoản khách hàng mới, liên kết người giới thiệu và tặng điểm chào mừng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string tenDangNhap, string matKhau, string nhapLaiMatKhau, string hoTen, string? email, string? maGioiThieu = null, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                ViewBag.RefCode = maGioiThieu;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (matKhau != nhapLaiMatKhau)
            {
                ViewBag.Error = "Mật khẩu và xác nhận mật khẩu không khớp.";
                ViewBag.RefCode = maGioiThieu;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var existingUser = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap.Trim().ToLower() == tenDangNhap.Trim().ToLower());
            if (existingUser != null)
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                ViewBag.RefCode = maGioiThieu;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            string finalEmail = !string.IsNullOrWhiteSpace(email) ? email.Trim() : $"{tenDangNhap.Trim().ToLower()}@customer.local";
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingEmail = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Email.ToLower() == finalEmail.ToLower());
                if (existingEmail != null)
                {
                    ViewBag.Error = "Địa chỉ email này đã được sử dụng bởi tài khoản khác.";
                    ViewBag.RefCode = maGioiThieu;
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }
            }

            var newTaiKhoan = new TaiKhoan
            {
                TenDangNhap = tenDangNhap.Trim(),
                MatKhau = matKhau,
                Email = finalEmail,
                HoTen = !string.IsNullOrWhiteSpace(hoTen) ? hoTen.Trim() : "Khách hàng mới",
                VaiTro = "Khách hàng",
                TrangThai = "Hoạt động",
                NgayTao = DateTime.Now
            };
            _context.TaiKhoans.Add(newTaiKhoan);
            await _context.SaveChangesAsync(); 

            var newNguoiDung = new NguoiDung
            {
                MaTaiKhoan = newTaiKhoan.MaTaiKhoan,
                HoTen = newTaiKhoan.HoTen,
                TongChiTieu = 0,
                HangVip = "VIP Khởi Đầu",
                DiemTichLuy = 0
            };
            _context.NguoiDungs.Add(newNguoiDung);
            await _context.SaveChangesAsync();

            newNguoiDung.MaGioiThieu = "REF-" + newNguoiDung.MaNguoiDung.ToString("D5");

            if (!string.IsNullOrWhiteSpace(maGioiThieu))
            {
                var refCodeClean = maGioiThieu.Trim().ToUpper();
                var referrer = await _context.NguoiDungs.FirstOrDefaultAsync(n => n.MaGioiThieu == refCodeClean);
                if (referrer != null && referrer.MaNguoiDung != newNguoiDung.MaNguoiDung)
                {
                    newNguoiDung.NguoiGioiThieuId = referrer.MaNguoiDung;
                    newNguoiDung.DiemTichLuy += 10000;
                    _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
                    {
                        MaNguoiDung = newNguoiDung.MaNguoiDung,
                        SoDiem = 10000,
                        LoaiGiaoDich = "ChaoMungDangKy",
                        MoTa = $"Quà tặng chào mừng thành viên mới đăng ký qua mã giới thiệu ({referrer.MaGioiThieu})",
                        NgayGiaoDich = DateTime.Now
                    });
                    TempData["SuccessMessage"] = $"Đăng ký thành công! Bạn nhận ngay 10.000 điểm thưởng chào mừng từ người giới thiệu {referrer.HoTen}.";
                }
            }

            _context.NguoiDungs.Update(newNguoiDung);
            await _context.SaveChangesAsync();

            // Ghi nhật ký bảo mật đăng ký
            await _auditLogService.LogAsync("DANG_KY", $"Đăng ký tài khoản thành công: {newTaiKhoan.TenDangNhap} ({newTaiKhoan.Email})", "Tài khoản", null, newTaiKhoan.Email, "Thấp");

            // Lưu thông tin đăng nhập vào Session
            HttpContext.Session.SetString("AdminUser", newTaiKhoan.TenDangNhap.Trim());
            HttpContext.Session.SetString("VaiTro", "Khách hàng");
            
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // =========================================================================
        // NHÓM CHỨC NĂNG OAUTH 1-CHẠM (GOOGLE / FACEBOOK) TĂNG TỐC CHECKOUT
        // =========================================================================

        /// <summary>
        /// GET: /Auth/GoogleLogin
        /// Khởi chạy luồng đăng nhập một chạm Google OAuth
        /// </summary>
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            return RedirectToAction("OAuthDemo", new { provider = "Google", returnUrl });
        }

        /// <summary>
        /// GET: /Auth/FacebookLogin
        /// Khởi chạy luồng đăng nhập một chạm Facebook OAuth
        /// </summary>
        [HttpGet]
        public IActionResult FacebookLogin(string? returnUrl = null)
        {
            return RedirectToAction("OAuthDemo", new { provider = "Facebook", returnUrl });
        }

        /// <summary>
        /// GET: /Auth/OAuthDemo
        /// Giao diện trải nghiệm Google/Facebook One-Tap trực quan với các hồ sơ mẫu sẵn sàng 1-click
        /// </summary>
        [HttpGet]
        public IActionResult OAuthDemo(string provider = "Google", string? returnUrl = null)
        {
            ViewBag.Provider = provider;
            ViewBag.ReturnUrl = returnUrl ?? "/";
            return View();
        }

        /// <summary>
        /// POST: /Auth/FastOAuthLogin
        /// Xử lý đăng nhập / đăng ký 1-chạm qua OAuth (Google / Facebook)
        /// Tự động liên kết tài khoản hoặc cấp tài khoản mới và tặng 10.000 điểm thưởng chào mừng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> FastOAuthLogin(string provider, string email, string name, string? avatar = null, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email xác thực OAuth không hợp lệ.";
                return RedirectToAction("Login");
            }

            provider = string.IsNullOrWhiteSpace(provider) ? "Google" : provider.Trim();
            email = email.Trim().ToLower();
            name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim();

            // 1. Kiểm tra tài khoản đã tồn tại qua Email hoặc cặp Provider + OAuthId
            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.Email.ToLower() == email || (t.OAuthProvider == provider && t.OAuthId == email));

            if (user != null)
            {
                // Cập nhật nhà cung cấp OAuth nếu chưa có
                if (string.IsNullOrEmpty(user.OAuthProvider))
                {
                    user.OAuthProvider = provider;
                    user.OAuthId = email;
                    _context.TaiKhoans.Update(user);
                    await _context.SaveChangesAsync();
                }

                // Đảm bảo có profile NguoiDung
                if (user.NguoiDung == null)
                {
                    var nd = new NguoiDung
                    {
                        MaTaiKhoan = user.MaTaiKhoan,
                        HoTen = user.HoTen,
                        TongChiTieu = 0,
                        HangVip = "VIP Khởi Đầu",
                        DiemTichLuy = 5000,
                        MaGioiThieu = "REF-" + user.MaTaiKhoan.ToString("D5")
                    };
                    _context.NguoiDungs.Add(nd);
                    await _context.SaveChangesAsync();
                }

                // Lưu Session phiên đăng nhập
                HttpContext.Session.SetString("AdminUser", user.TenDangNhap.Trim());
                HttpContext.Session.SetString("VaiTro", user.VaiTro?.Trim() ?? "Khách hàng");

                // Ghi nhật ký hệ thống
                await _auditLogService.LogAsync("DANG_NHAP_OAUTH", $"Đăng nhập 1-chạm thành công qua {provider} cho tài khoản {user.TenDangNhap} ({user.Email})", "Bảo mật", null, user.Email, "Thấp");

                TempData["SuccessMessage"] = $"Đăng nhập 1-chạm qua {provider} thành công! Xin chào {user.HoTen}.";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return user.VaiTro?.Trim() == "Quản trị viên" ? RedirectToAction("Index", "DonHangs") : RedirectToAction("Index", "Home");
            }

            // 2. Tự động sinh tên đăng nhập duy nhất cho khách hàng mới
            string baseUsername = email.Split('@')[0].Replace(".", "").Replace("-", "").Replace("_", "");
            if (baseUsername.Length > 18) baseUsername = baseUsername[..18];
            string uniqueUsername = baseUsername;
            int counter = 1;
            while (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap.ToLower() == uniqueUsername.ToLower()))
            {
                uniqueUsername = $"{baseUsername}{counter++}";
            }

            var newTaiKhoan = new TaiKhoan
            {
                TenDangNhap = uniqueUsername,
                MatKhau = Guid.NewGuid().ToString("N")[..12], // Mật khẩu ngẫu nhiên an toàn
                Email = email,
                HoTen = name,
                VaiTro = "Khách hàng",
                TrangThai = "Hoạt động",
                OAuthProvider = provider,
                OAuthId = email,
                NgayTao = DateTime.Now
            };
            _context.TaiKhoans.Add(newTaiKhoan);
            await _context.SaveChangesAsync();

            // Khởi tạo Hồ sơ người dùng & Tặng ngay 10.000 điểm chào mừng
            var newNguoiDung = new NguoiDung
            {
                MaTaiKhoan = newTaiKhoan.MaTaiKhoan,
                HoTen = name,
                TongChiTieu = 0,
                HangVip = "VIP Khởi Đầu",
                DiemTichLuy = 10000
            };
            _context.NguoiDungs.Add(newNguoiDung);
            await _context.SaveChangesAsync();

            newNguoiDung.MaGioiThieu = "REF-" + newNguoiDung.MaNguoiDung.ToString("D5");
            _context.NguoiDungs.Update(newNguoiDung);

            // Ghi nhận sổ cái lịch sử điểm thưởng
            _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
            {
                MaNguoiDung = newNguoiDung.MaNguoiDung,
                SoDiem = 10000,
                LoaiGiaoDich = "ChaoMungOAuth",
                MoTa = $"Quà tặng chào mừng +10.000 điểm khi đăng nhập một chạm qua {provider}",
                NgayGiaoDich = DateTime.Now
            });
            await _context.SaveChangesAsync();

            // Lưu Session
            HttpContext.Session.SetString("AdminUser", newTaiKhoan.TenDangNhap.Trim());
            HttpContext.Session.SetString("VaiTro", "Khách hàng");

            // Ghi nhật ký bảo mật
            await _auditLogService.LogAsync("DANG_KY_OAUTH", $"Đăng ký & Đăng nhập 1-chạm tự động qua {provider} (Username: {newTaiKhoan.TenDangNhap}, Email: {newTaiKhoan.Email})", "Bảo mật", null, newTaiKhoan.Email, "Trung bình");

            TempData["SuccessMessage"] = $"Đăng ký & Đăng nhập 1-chạm qua {provider} thành công! Bạn nhận ngay 10.000 điểm thưởng thành viên.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// POST: /Auth/OneTapLogin
        /// API JSON nhận tín hiệu Google One-Tap SDK / FedCM phía client
        /// </summary>
        [HttpPost]
        [Route("Auth/OneTapLogin")]
        public async Task<IActionResult> OneTapLogin([FromBody] OAuthCallbackDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return Json(new { success = false, message = "Thông tin xác thực không hợp lệ." });
            }

            string provider = string.IsNullOrWhiteSpace(dto.Provider) ? "Google" : dto.Provider.Trim();
            string email = dto.Email.Trim().ToLower();
            string name = string.IsNullOrWhiteSpace(dto.Name) ? email.Split('@')[0] : dto.Name.Trim();

            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.Email.ToLower() == email || (t.OAuthProvider == provider && t.OAuthId == email));

            if (user != null)
            {
                if (string.IsNullOrEmpty(user.OAuthProvider))
                {
                    user.OAuthProvider = provider;
                    user.OAuthId = email;
                    _context.TaiKhoans.Update(user);
                    await _context.SaveChangesAsync();
                }

                HttpContext.Session.SetString("AdminUser", user.TenDangNhap.Trim());
                HttpContext.Session.SetString("VaiTro", user.VaiTro?.Trim() ?? "Khách hàng");

                await _auditLogService.LogAsync("DANG_NHAP_OAUTH", $"Đăng nhập 1-chạm thành công qua {provider} cho tài khoản {user.TenDangNhap}", "Bảo mật", null, user.Email, "Thấp");

                return Json(new { success = true, isNew = false, message = $"Chào mừng {user.HoTen} đã quay lại!", redirectUrl = dto.ReturnUrl ?? "/" });
            }

            // Đăng ký mới tự động
            string baseUsername = email.Split('@')[0].Replace(".", "").Replace("-", "").Replace("_", "");
            if (baseUsername.Length > 18) baseUsername = baseUsername[..18];
            string uniqueUsername = baseUsername;
            int counter = 1;
            while (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap.ToLower() == uniqueUsername.ToLower()))
            {
                uniqueUsername = $"{baseUsername}{counter++}";
            }

            var newTaiKhoan = new TaiKhoan
            {
                TenDangNhap = uniqueUsername,
                MatKhau = Guid.NewGuid().ToString("N")[..12],
                Email = email,
                HoTen = name,
                VaiTro = "Khách hàng",
                TrangThai = "Hoạt động",
                OAuthProvider = provider,
                OAuthId = email,
                NgayTao = DateTime.Now
            };
            _context.TaiKhoans.Add(newTaiKhoan);
            await _context.SaveChangesAsync();

            var newNguoiDung = new NguoiDung
            {
                MaTaiKhoan = newTaiKhoan.MaTaiKhoan,
                HoTen = name,
                TongChiTieu = 0,
                HangVip = "VIP Khởi Đầu",
                DiemTichLuy = 10000
            };
            _context.NguoiDungs.Add(newNguoiDung);
            await _context.SaveChangesAsync();

            newNguoiDung.MaGioiThieu = "REF-" + newNguoiDung.MaNguoiDung.ToString("D5");
            _context.NguoiDungs.Update(newNguoiDung);

            _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
            {
                MaNguoiDung = newNguoiDung.MaNguoiDung,
                SoDiem = 10000,
                LoaiGiaoDich = "ChaoMungOAuth",
                MoTa = $"Quà tặng chào mừng +10.000 điểm khi đăng nhập một chạm qua {provider}",
                NgayGiaoDich = DateTime.Now
            });
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("AdminUser", newTaiKhoan.TenDangNhap.Trim());
            HttpContext.Session.SetString("VaiTro", "Khách hàng");

            await _auditLogService.LogAsync("DANG_KY_OAUTH", $"Đăng ký 1-chạm thành công qua {provider} (Username: {newTaiKhoan.TenDangNhap})", "Bảo mật", null, newTaiKhoan.Email, "Trung bình");

            return Json(new { success = true, isNew = true, message = "Đăng ký tài khoản thành công! Tặng bạn 10.000 điểm.", redirectUrl = dto.ReturnUrl ?? "/" });
        }
    }
}