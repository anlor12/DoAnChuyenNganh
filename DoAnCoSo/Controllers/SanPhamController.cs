using System.IO;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// SanPhamController: Quản lý hiển thị danh sách sản phẩm phía khách hàng.
    /// Cung cấp các bộ lọc tìm kiếm nâng cao (theo từ khóa, loại sản phẩm, khoảng giá, giới tính)
    /// và các tiêu chí sắp xếp linh hoạt (theo giá, bán chạy, mới nhất).
    /// </summary>
    public class SanPhamController : Controller
    {
        private readonly ApplicationDbContext _context;

        // DI thông qua Constructor để dùng ApplicationDbContext
        public SanPhamController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /SanPham
        /// Hiển thị danh sách sản phẩm kèm theo bộ lọc tìm kiếm và sắp xếp.
        /// </summary>
        /// <param name="tuKhoa">Từ khóa tìm kiếm theo tên sản phẩm</param>
        /// <param name="maLoai">Mã danh mục loại sản phẩm</param>
        /// <param name="giaMin">Giá tối thiểu</param>
        /// <param name="giaMax">Giá tối đa</param>
        /// <param name="sapXep">Tiêu chí sắp xếp (gia_tang, gia_giam, ten_az, ban_chay, moi_nhat)</param>
        /// <param name="gioiTinh">Bộ lọc giới tính (Nam, Nữ, Unisex)</param>
        public async Task<IActionResult> Index(
            string? tuKhoa, int? maLoai, decimal? giaMin, decimal? giaMax, string? sapXep, string? gioiTinh,
            List<string>? kichCo, List<string>? mauSac, List<string>? kieuCoAo, List<string>? chatLieu, List<string>? formDang)
        {
            // 1. Tải danh sách Loại sản phẩm kèm phân cấp cha - con
            var loaiSanPhams = await _context.LoaiSanPhams
                .Include(l => l.LoaiCha)
                .Include(l => l.LoaiCons)
                .OrderBy(l => l.MaLoaiCha.HasValue ? 1 : 0)
                .ThenBy(l => l.TenLoaiSanPham)
                .ToListAsync();
            ViewBag.LoaiSanPhams = loaiSanPhams;

            // 2. Lưu lại các giá trị lọc vào ViewBag để giữ nguyên trạng thái hiển thị trên Form khi tải lại trang
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaLoai = maLoai;
            ViewBag.GiaMin = giaMin;
            ViewBag.GiaMax = giaMax;
            ViewBag.SapXep = sapXep;
            ViewBag.GioiTinh = gioiTinh;

            if (maLoai.HasValue)
            {
                ViewBag.CurrentCategory = loaiSanPhams.FirstOrDefault(x => x.MaLoaiSanPham == maLoai.Value);
            }

            // ViewBag arrays for new functional filters
            ViewBag.KichCo = kichCo ?? new List<string>();
            ViewBag.MauSac = mauSac ?? new List<string>();
            ViewBag.KieuCoAo = kieuCoAo ?? new List<string>();
            ViewBag.ChatLieu = chatLieu ?? new List<string>();
            ViewBag.FormDang = formDang ?? new List<string>();

            // 3. Khởi tạo truy vấn gốc: Chỉ lấy các sản phẩm có trạng thái "Đang bán" (DangBan)
            var query = _context.SanPhams
                .Where(s => s.TrangThai == "Đang bán")
                .Include(s => s.LoaiSanPham)       // Nạp kèm thông tin Loại sản phẩm
                .Include(s => s.BienTheSanPhams)   // Nạp kèm danh sách biến thể (size, màu)
                .AsQueryable();                    // Chuyển về dạng truy vấn động để ghép thêm bộ lọc

            // 4. Lọc theo giới tính
            if (!string.IsNullOrWhiteSpace(gioiTinh))
            {
                if (gioiTinh == "Nam")
                    query = query.Where(s => s.GioiTinh == "Nam" || s.GioiTinh == "Unisex");
                else if (gioiTinh == "Nữ")
                    query = query.Where(s => s.GioiTinh == "Nữ" || s.GioiTinh == "Unisex");
                else
                    query = query.Where(s => s.GioiTinh == gioiTinh);
            }

            // 5. Lọc theo từ khóa tên sản phẩm
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(s => s.TenSanPham.Contains(tuKhoa));
            }

            // 6. Lọc theo loại sản phẩm cụ thể (Tự động gom tất cả danh mục con và cháu của danh mục được chọn)
            if (maLoai.HasValue && maLoai > 0)
            {
                var targetCat = loaiSanPhams.FirstOrDefault(l => l.MaLoaiSanPham == maLoai.Value);
                if (targetCat != null)
                {
                    var allCatIds = new List<int> { targetCat.MaLoaiSanPham };
                    void CollectChildren(int parentId)
                    {
                        var directChildren = loaiSanPhams.Where(l => l.MaLoaiCha == parentId).Select(l => l.MaLoaiSanPham).ToList();
                        foreach (var cId in directChildren)
                        {
                            if (!allCatIds.Contains(cId))
                            {
                                allCatIds.Add(cId);
                                CollectChildren(cId);
                            }
                        }
                    }
                    CollectChildren(targetCat.MaLoaiSanPham);
                    query = query.Where(s => allCatIds.Contains(s.MaLoaiSanPham));
                }
                else
                {
                    query = query.Where(s => s.MaLoaiSanPham == maLoai.Value);
                }
            }

            // 7. Lọc theo khoảng giá
            if (giaMin.HasValue && giaMin > 0)
                query = query.Where(s => s.Gia >= giaMin.Value);
            if (giaMax.HasValue && giaMax > 0)
                query = query.Where(s => s.Gia <= giaMax.Value);

            // ==========================================
            // LỌC CHỨC NĂNG MỚI (NEW FUNCTIONAL FILTERS)
            // ==========================================
            
            // Lọc theo màu sắc (qua BienTheSanPham)
            if (mauSac != null && mauSac.Any())
            {
                query = query.Where(s => s.BienTheSanPhams!.Any(b => mauSac.Contains(b.MauSac)));
            }

            // Lọc theo kích cỡ (qua BienTheSanPham)
            if (kichCo != null && kichCo.Any())
            {
                query = query.Where(s => s.BienTheSanPhams!.Any(b => kichCo.Contains(b.KichCo)));
            }

            // Lọc theo Kiểu cổ áo (DB Column)
            if (kieuCoAo != null && kieuCoAo.Any())
            {
                query = query.Where(s => s.KieuCoAo != null && kieuCoAo.Contains(s.KieuCoAo));
            }

            // Lọc theo Chất liệu (DB Column)
            if (chatLieu != null && chatLieu.Any())
            {
                query = query.Where(s => s.ChatLieu != null && chatLieu.Contains(s.ChatLieu));
            }

            // Lọc theo Form dáng (DB Column)
            if (formDang != null && formDang.Any())
            {
                query = query.Where(s => s.FormDang != null && formDang.Contains(s.FormDang));
            }

            // 8. Áp dụng sắp xếp sản phẩm dựa trên tiêu chí người dùng chọn
            if (sapXep == "ban_chay")
                query = query.Where(s => s.IsBanChay);

            query = sapXep switch
            {
                "gia_tang" => query.OrderBy(s => s.Gia), // Sắp xếp giá tăng dần
                "gia_giam" => query.OrderByDescending(s => s.Gia), // Sắp xếp giá giảm dần
                "ten_az" => query.OrderBy(s => s.TenSanPham), // Sắp xếp tên từ A đến Z
                "ban_chay" => query.OrderByDescending(s => s.BienTheSanPhams!
                    .SelectMany(bt => _context.ChiTietDonHangs.Where(ct => ct.MaBienThe == bt.MaBienThe))
                    .Sum(ct => ct.SoLuong)), // Sắp xếp theo tổng số lượng bán ra
                "moi_nhat" => query.OrderByDescending(s => s.MaSanPham), // Sản phẩm mới nhập lên đầu (ID lớn nhất)
                _ => query.OrderByDescending(s => s.MaSanPham) // Mặc định sắp xếp theo mã sản phẩm mới nhất
            };

            // 9. Thực thi truy vấn CSDL và lấy danh sách kết quả bất đồng bộ
            var sanPhams = await query.ToListAsync();
            return View(sanPhams);
        }

        /// <summary>
        /// GET: /SanPham/Details/5 hoặc /SanPham/ChiTiet/5
        /// Hiển thị thông tin chi tiết của một sản phẩm cụ thể cùng các biến thể đi kèm (size, màu).
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        [HttpGet]
        [Route("SanPham/Details/{id:int}")]
        [Route("SanPham/ChiTiet/{id:int}")]
        [Route("SanPhams/ChiTiet/{id:int}")]
        [Route("SanPhams/Details/{id:int}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Truy vấn CSDL tìm sản phẩm theo ID, đồng thời nạp kèm các dữ liệu liên quan
            var sanPham = await _context.SanPhams
                .Include(s => s.LoaiSanPham)
                .Include(s => s.BienTheSanPhams)
                .FirstOrDefaultAsync(m => m.MaSanPham == id && m.TrangThai == "Đang bán");

            // Nếu không tìm thấy hoặc sản phẩm đã bị ngừng kinh doanh (không còn ở trạng thái Đang bán)
            if (sanPham == null) return NotFound();

            // Tải danh sách đánh giá của sản phẩm này kèm thông tin người dùng
            var danhGias = await _context.DanhGias
                .Include(dg => dg.NguoiDung)
                .Where(dg => dg.MaSanPham == id.Value)
                .OrderByDescending(dg => dg.NgayTao)
                .ToListAsync();

            ViewBag.DanhGias = danhGias;
            ViewBag.TongSoDanhGia = danhGias.Count;
            ViewBag.DiemTrungBinh = danhGias.Any() ? Math.Round(danhGias.Average(dg => dg.DiemDanhGia), 1) : 0.0;

            // Lấy tên đăng nhập từ Session để tính toán size gợi ý dựa vào số đo trong profile
            var username = HttpContext.Session.GetString("AdminUser");
            string recommendedSize = "";
            double userHeight = 170;
            double userWeight = 60;
            string userGender = "Nam";

            if (!string.IsNullOrEmpty(username))
            {
                var user = await _context.NguoiDungs
                    .Include(u => u.TaiKhoan)
                    .FirstOrDefaultAsync(u => u.TaiKhoan != null && u.TaiKhoan.TenDangNhap == username);
                if (user != null)
                {
                    if (user.ChieuCao.HasValue && user.CanNang.HasValue)
                    {
                        recommendedSize = CalculateRecommendedSize(
                            user.ChieuCao.Value, 
                            user.CanNang.Value, 
                            user.GioiTinh ?? "Unisex",
                            sanPham.TenSanPham,
                            sanPham.LoaiSanPham?.TenLoaiSanPham ?? "",
                            sanPham.BienTheSanPhams?.Select(b => b.KichCo).ToList()
                        );
                    }
                    userHeight = user.ChieuCao ?? 170;
                    userWeight = user.CanNang ?? 60;
                    userGender = user.GioiTinh ?? "Nam";
                }
            }
            ViewBag.RecommendedSize = recommendedSize;
            ViewBag.UserHeight = userHeight;
            ViewBag.UserWeight = userWeight;
            ViewBag.UserGender = userGender;

            // 4. Upsell & Cross-sell: Sản phẩm phối đồ (Mix & Match) & Thường mua cùng
            var crossSellProducts = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                .Include(s => s.LoaiSanPham)
                .Where(s => s.MaSanPham != id.Value && s.MaLoaiSanPham != sanPham.MaLoaiSanPham && s.TrangThai == "Đang bán")
                .Take(2)
                .ToListAsync();

            // 5. Thống kê độ vừa vặn thực tế (Fit Feedback)
            int fitReviewsCount = danhGias.Count(dg => !string.IsNullOrEmpty(dg.DoVuaVan));
            int tightCount = danhGias.Count(dg => dg.DoVuaVan != null && (dg.DoVuaVan.ToLower().Contains("chật")));
            int trueCount = danhGias.Count(dg => dg.DoVuaVan != null && (dg.DoVuaVan.ToLower().Contains("vừa")));
            int looseCount = danhGias.Count(dg => dg.DoVuaVan != null && (dg.DoVuaVan.ToLower().Contains("rộng")));

            ViewBag.FitReviewsCount = fitReviewsCount;
            ViewBag.FitTightPercent = fitReviewsCount > 0 ? (int)Math.Round((double)tightCount / fitReviewsCount * 100) : 10;
            ViewBag.FitTruePercent = fitReviewsCount > 0 ? (int)Math.Round((double)trueCount / fitReviewsCount * 100) : 80;
            ViewBag.FitLoosePercent = fitReviewsCount > 0 ? (int)Math.Round((double)looseCount / fitReviewsCount * 100) : 10;

            // 6. Tải số lượng tồn kho của từng biến thể
            var variantIds = sanPham.BienTheSanPhams?.Select(b => b.MaBienThe).ToList() ?? new List<int>();
            var stockRecords = await _context.KhoHangs
                .Where(k => variantIds.Contains(k.MaBienThe))
                .Select(k => new { k.MaBienThe, k.SoLuongTon })
                .ToListAsync();
            var stockMap = stockRecords
                .GroupBy(k => k.MaBienThe)
                .ToDictionary(g => g.Key, g => g.Sum(k => k.SoLuongTon));
            ViewBag.StockMap = stockMap;

            ViewBag.CrossSellProducts = crossSellProducts;

            return View(sanPham);
        }

        /// <summary>
        /// POST: /SanPham/DangKyBaoCoHang
        /// Đăng ký nhận tin khi biến thể hết hàng được nhập thêm hàng lại (Back-in-Stock Alert)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DangKyBaoCoHang(int maSanPham, int? maBienThe, string lienHe, string? size, string? mauSac)
        {
            if (string.IsNullOrWhiteSpace(lienHe))
            {
                return Json(new { success = false, message = "Vui lòng nhập Email hoặc Số điện thoại để nhận thông báo." });
            }

            try
            {
                var subscription = new ThongBaoHetHang
                {
                    MaSanPham = maSanPham,
                    MaBienThe = maBienThe,
                    LienHe = lienHe.Trim(),
                    Size = size?.Trim(),
                    MauSac = mauSac?.Trim(),
                    NgayDangKy = DateTime.Now,
                    DaThongBao = false
                };

                _context.ThongBaoHetHangs.Add(subscription);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đăng ký thành công! Hệ thống sẽ tự động gửi thông báo đến bạn ngay khi sản phẩm này có hàng lại."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        /// <summary>
        /// POST: /SanPham/GuiDanhGia
        /// Khách hàng gửi đánh giá kèm ảnh thực tế & chỉ số form dáng (Fit Feedback)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiDanhGia(
            int maSanPham,
            int diemDanhGia,
            string? noiDung,
            double? chieuCao,
            double? canNang,
            string? sizeDaMua,
            string? doVuaVan,
            IFormFile? hinhAnhFile)
        {
            var username = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(username))
            {
                TempData["ReviewError"] = "Vui lòng đăng nhập để gửi đánh giá sản phẩm.";
                return RedirectToAction(nameof(Details), new { id = maSanPham });
            }

            var user = await _context.TaiKhoans
                .Include(t => t.NguoiDung)
                .FirstOrDefaultAsync(t => t.TenDangNhap == username);

            if (user?.NguoiDung == null)
            {
                TempData["ReviewError"] = "Không tìm thấy thông tin tài khoản người dùng.";
                return RedirectToAction(nameof(Details), new { id = maSanPham });
            }

            if (diemDanhGia < 1 || diemDanhGia > 5)
            {
                diemDanhGia = 5;
            }

            string? uploadedImagePath = null;
            if (hinhAnhFile != null && hinhAnhFile.Length > 0)
            {
                try
                {
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "reviews");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var ext = Path.GetExtension(hinhAnhFile.FileName);
                    var fileName = $"review_{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await hinhAnhFile.CopyToAsync(stream);
                    }

                    uploadedImagePath = $"/images/reviews/{fileName}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Upload review image error: " + ex.Message);
                }
            }

            var danhGia = new DanhGia
            {
                MaSanPham = maSanPham,
                MaNguoiDung = user.NguoiDung.MaNguoiDung,
                DiemDanhGia = diemDanhGia,
                NoiDung = !string.IsNullOrWhiteSpace(noiDung) ? noiDung.Trim() : "Sản phẩm đẹp, chất lượng tốt!",
                NgayTao = DateTime.Now,
                ChieuCao = chieuCao,
                CanNang = canNang,
                SizeDaMua = sizeDaMua,
                DoVuaVan = doVuaVan ?? "Vừa vặn hoàn hảo",
                HinhAnh = uploadedImagePath
            };

            _context.DanhGias.Add(danhGia);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá và chia sẻ thông số mặc thực tế giúp cộng đồng!";
            return RedirectToAction(nameof(Details), new { id = maSanPham });
        }

        private static string CalculateRecommendedSize(
            double height, 
            double weight, 
            string gender, 
            string productName, 
            string categoryName,
            List<string>? availableSizes)
        {
            string cleanProdName = productName.ToLower();
            string cleanCatName = categoryName.ToLower();

            // 1. Kiểm tra xem có phải là Giày dép hay không
            bool isShoe = cleanCatName.Contains("giày") || cleanCatName.Contains("dép") || cleanCatName.Contains("shoe") || cleanCatName.Contains("footwear") ||
                          cleanProdName.Contains("giày") || cleanProdName.Contains("dép") || cleanProdName.Contains("shoe");

            if (isShoe)
            {
                if (gender == "Nữ" || gender == "Nu")
                {
                    if (height < 150) return "35";
                    if (height >= 150 && height < 155) return "36";
                    if (height >= 155 && height < 160) return "37";
                    if (height >= 160 && height < 165) return "38";
                    if (height >= 165 && height < 170) return "39";
                    return "40";
                }
                else // Nam or Unisex
                {
                    if (height < 160) return "39";
                    if (height >= 160 && height < 165) return "40";
                    if (height >= 165 && height < 170) return "41";
                    if (height >= 170 && height < 175) return "42";
                    if (height >= 175 && height < 180) return "43";
                    if (height >= 180 && height < 185) return "44";
                    return "45";
                }
            }

            // 2. Kiểm tra xem các size hiện có có phải là số hay không (ví dụ: quần jean size 29, 30, 31, 32...)
            bool hasNumericSizes = false;
            if (availableSizes != null && availableSizes.Any())
            {
                string firstSize = availableSizes.First().Trim();
                if (firstSize.Length > 0 && char.IsDigit(firstSize[0]))
                {
                    hasNumericSizes = true;
                }
            }

            if (hasNumericSizes)
            {
                // Quy đổi size quần (28 - 36)
                if (gender == "Nữ" || gender == "Nu")
                {
                    if (weight < 43) return "26";
                    if (weight >= 43 && weight < 47) return "27";
                    if (weight >= 47 && weight < 52) return "28";
                    if (weight >= 52 && weight < 57) return "29";
                    if (weight >= 57 && weight < 62) return "30";
                    return "31";
                }
                else // Nam or Unisex
                {
                    if (weight < 52) return "28";
                    if (weight >= 52 && weight < 57) return "29";
                    if (weight >= 57 && weight < 62) return "30";
                    if (weight >= 62 && weight < 67) return "31";
                    if (weight >= 67 && weight < 72) return "32";
                    if (weight >= 72 && weight < 77) return "33";
                    if (weight >= 77 && weight < 82) return "34";
                    return "36";
                }
            }

            // 3. Quy đổi mặc định theo size chữ (S, M, L, XL, XXL) cho Áo/Quần size chữ
            if (gender == "Nữ" || gender == "Nu")
            {
                if (weight < 45) return "S";
                if (weight >= 45 && weight < 53) return "M";
                if (weight >= 53 && weight < 60) return "L";
                if (weight >= 60 && weight < 68) return "XL";
                return "XXL";
            }
            else // Nam or Unisex
            {
                if (weight < 55) return "S";
                if (weight >= 55 && weight < 65) return "M";
                if (weight >= 65 && weight < 75) return "L";
                if (weight >= 75 && weight < 85) return "XL";
                return "XXL";
            }
        }
    }
}

