using System.Diagnostics;
using System.IO;
using System.Net.Http;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// HomeController: Điều hướng trang chủ phía khách hàng (Index), trang Giới thiệu,
    /// và cung cấp trang Dashboard Thống kê tổng quan dành riêng cho Quản trị viên (Admin).
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GeminiService _geminiService;

        // DI thông qua Constructor để dùng ApplicationDbContext
        public HomeController(ApplicationDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        private async Task EnsureLocalAssetsAsync()
        {
            var urls = new Dictionary<string, string>
            {
                { "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=1200&q=80", "wwwroot/images/online_exclusive_banner.jpg" },
                { "https://assets.braintreegateway.com/payment_method_logo/visa.png", "wwwroot/images/visa_logo.png" },
                { "https://assets.braintreegateway.com/payment_method_logo/mastercard.png", "wwwroot/images/mastercard_logo.png" },
                { "https://assets.braintreegateway.com/payment_method_logo/jcb.png", "wwwroot/images/jcb_logo.png" },
                { "http://online.gov.vn/PublicImages/2015/08/27/11/20150827110756-dathongbao.png", "wwwroot/images/bo_cong_thuong.png" }
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                foreach (var pair in urls)
                {
                    try
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), pair.Value);
                        if (!System.IO.File.Exists(filePath))
                        {
                            var bytes = await client.GetByteArrayAsync(pair.Key);
                            await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error downloading {pair.Key}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// GET: / (Trang chủ)
        /// Luồng xử lý chính khi người dùng truy cập trang chủ:
        /// 1. Nếu người đăng nhập là Admin -> Tự động chuyển hướng sang trang Dashboard quản trị.
        /// 2. Nếu là Khách hàng -> Tải danh mục sản phẩm, Lookbook nổi bật, sản phẩm nổi bật/mới nhất để hiển thị.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Download assets locally if missing
            await EnsureLocalAssetsAsync();

            // 1. Cho phép cả Quản trị viên và Khách hàng truy cập trực tiếp Trang chủ bán hàng khi bấm Xem Cửa Hàng

            // 2. Tải danh mục sản phẩm từ CSDL để hiển thị lên Menu danh mục
            ViewBag.Categories = await _context.LoaiSanPhams
                .OrderBy(l => l.TenLoaiSanPham)
                .ToListAsync();

            // 3. Tải các bộ sưu tập thời trang nổi bật kèm các điểm ghim sản phẩm thực tế
            ViewBag.Lookbooks = await _context.BoSuuTaps
                .Include(s => s.DiemGhimBoSuuTaps!)
                    .ThenInclude(h => h.SanPham)
                .ToListAsync();

            // 4. Tìm kiếm các sản phẩm được đánh dấu Nổi bật (IsNoiBat) và ở trạng thái "Đang bán"
            var sanPhams = await _context.SanPhams
                .Where(s => s.IsNoiBat && (s.TrangThai == "DangBan" || s.TrangThai == "Đang bán"))
                .Include(s => s.LoaiSanPham)
                .Include(s => s.BienTheSanPhams)
                .ToListAsync();

            // Nếu hệ thống chưa thiết lập sản phẩm nổi bật nào, tự động lấy 8 sản phẩm mới nhất làm mặc định
            if (!sanPhams.Any())
            {
                sanPhams = await _context.SanPhams
                    .Where(s => s.TrangThai == "DangBan" || s.TrangThai == "Đang bán")
                    .Include(s => s.LoaiSanPham)
                    .Include(s => s.BienTheSanPhams)
                    .OrderByDescending(s => s.MaSanPham)
                    .Take(8)
                    .ToListAsync();
            }

            // 5. Tải bài viết mới nhất cho phần tin tức trang chủ
            ViewBag.LatestArticles = await _context.BaiViets
                .Where(b => b.TrangThai)
                .OrderByDescending(b => b.NgayTao)
                .Take(3)
                .ToListAsync();

            return View(sanPhams);
        }

        /// <summary>
        /// GET: /Home/Privacy
        /// Chính sách bảo mật, vận chuyển, đổi trả và điều khoản dịch vụ
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// GET: /Home/DatHang/5
        /// Action giả định khi người dùng nhấn nút Mua ngay trên một sản phẩm nhanh
        /// </summary>
        public IActionResult DatHang(int id)
        {
            // KIỂM TRA ĐĂNG NHẬP: Bắt buộc người dùng phải đăng nhập trước khi mua hàng
            if (HttpContext.Session.GetString("AdminUser") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Nếu đã đăng nhập, chuyển hướng sang trang chủ (hoặc trang giỏ hàng/thanh toán tương ứng)
            return RedirectToAction("Index");
        }

        /// <summary>
        /// GET: /Home/GioiThieu
        /// Hiển thị trang Giới thiệu về cửa hàng thời trang với nội dung động được cấu hình bởi Admin
        /// </summary>
        public async Task<IActionResult> GioiThieu()
        {
            var configs = await _context.CauHinhHeThongs.ToListAsync();
            string GetVal(string key, string fallback) =>
                configs.FirstOrDefault(c => c.Key == key)?.Value ?? fallback;

            var model = new CauHinhGioiThieuViewModel
            {
                HeroEyebrow = GetVal("About_Hero_Eyebrow", "Fashion Store — Câu chuyện của chúng tôi"),
                HeroTitle = GetVal("About_Hero_Title", "Về Chúng Tôi"),
                HeroImage = GetVal("About_Hero_Image", "/images/store-interior.jpg.jpg"),

                StatProducts = GetVal("About_Stat_Products", "50+"),
                StatCustomers = GetVal("About_Stat_Customers", "10.000+"),
                StatYears = GetVal("About_Stat_Years", "5+"),
                StatProvinces = GetVal("About_Stat_Provinces", "63+"),

                MissionTitle = GetVal("About_Mission_Title", "Thời trang Không giới hạn"),
                MissionP1 = GetVal("About_Mission_P1", "Chúng tôi tin rằng thời trang không chỉ là quần áo — đó là ngôn ngữ của bản sắc. Mỗi sản phẩm được thiết kế với tư duy tối giản nhưng đậm chất cá tính, mang đến sự tự tin cho người mặc trong từng khoảnh khắc của cuộc sống."),
                MissionP2 = GetVal("About_Mission_P2", "Từ những bước chạy bộ buổi sáng đến những buổi tối đô thị sôi động — Fashion Store đồng hành cùng bạn với chất lượng vượt trội và phong cách không bao giờ lỗi thời."),

                StoryTitle = GetVal("About_Story_Title", "Sinh ra từ đam mê thời trang"),
                StoryP1 = GetVal("About_Story_P1", "Khởi đầu từ một cửa hàng nhỏ với ước mơ lớn vào năm 2020, Fashion Store đã vươn mình trở thành một trong những thương hiệu thời trang thể thao được yêu thích nhất tại Việt Nam."),
                StoryP2 = GetVal("About_Story_P2", "Chúng tôi không ngừng tìm kiếm những chất liệu tốt nhất, những thiết kế tinh tế nhất — tất cả vì một mục tiêu duy nhất: mang lại giá trị thực sự cho khách hàng."),
                StoryImage = GetVal("About_Story_Image", "/images/store-interior.jpg.jpg"),

                Value1Title = GetVal("About_Value1_Title", "Chất lượng tuyệt đối"),
                Value1Desc = GetVal("About_Value1_Desc", "Mỗi sản phẩm đều trải qua quy trình kiểm định nghiêm ngặt trước khi đến tay khách hàng. Không có chỗ cho sự thỏa hiệp."),

                Value2Title = GetVal("About_Value2_Title", "Thiết kế hiện đại"),
                Value2Desc = GetVal("About_Value2_Desc", "Cập nhật liên tục các xu hướng thời trang quốc tế, dung hòa hoàn hảo với phom dáng và khí hậu Việt Nam."),

                Value3Title = GetVal("About_Value3_Title", "Tận tâm phục vụ"),
                Value3Desc = GetVal("About_Value3_Desc", "Khách hàng là trung tâm của mọi quyết định. Chính sách đổi trả linh hoạt và hỗ trợ tư vấn 24/7.")
            };

            return View(model);
        }

        /// <summary>
        /// GET: /Home/Dashboard (Dành riêng cho Admin)
        /// Tính toán các số liệu thống kê: Doanh thu, số lượng đã bán, tồn kho, đơn hàng chờ,
        /// chuẩn bị dữ liệu mảng vẽ biểu đồ doanh thu theo tháng và sản phẩm bán chạy nhất.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()

        {

            // Bảo mật: Chỉ cho phép người dùng có Vai trò "Quản trị viên" trong Session truy cập trang này

            var vaiTro = HttpContext.Session.GetString("VaiTro");

            if (vaiTro != "Quản trị viên")

            {

                return RedirectToAction("Login", "Auth"); // Chuyển về trang đăng nhập nếu không phải Admin

            }



            // 1. Thống kê doanh thu: Tính tổng tiền của tất cả các đơn hàng có trạng thái "Hoàn thành" hoặc đã thanh toán

            var completedOrders = await _context.DonHangs

                .Where(o => o.TrangThaiDonHang == "Hoàn thành" || o.TrangThaiThanhToan == "Đã thanh toán")

                .ToListAsync();



            decimal totalRevenue = completedOrders.Sum(o => o.TongTien);



            // 2. Tổng số lượng sản phẩm đã bán ra thực tế: Tổng số lượng trong các Chi tiết đơn hàng của các đơn hoàn thành

            var completedOrderIds = completedOrders.Select(o => o.MaDonHang).ToList();

            int totalProductsSold = await _context.ChiTietDonHangs

                .Where(d => completedOrderIds.Contains(d.MaDonHang))

                .SumAsync(d => d.SoLuong);



            // 3. Tổng số lượng đơn hàng trong hệ thống và số đơn hàng ở trạng thái Chờ xác nhận/Đang xử lý

            int totalOrdersCount = await _context.DonHangs.CountAsync();

            int pendingOrdersCount = await _context.DonHangs.CountAsync(o => o.TrangThaiDonHang == "Chờ xác nhận" || o.TrangThaiDonHang == "Đang giao");



            // 4. Tổng số lượng sản phẩm, loại sản phẩm và số lượng sản phẩm còn tồn trong kho

            int totalProductsCount = await _context.SanPhams.CountAsync();

            int totalCategoriesCount = await _context.LoaiSanPhams.CountAsync();

            

            int totalStockCount = 0;

            // Kiểm tra tránh lỗi CSDL rỗng

            if (await _context.KhoHangs.AnyAsync())

            {

                totalStockCount = await _context.KhoHangs.SumAsync(k => k.SoLuongTon);

            }



            // 5. Thống kê doanh thu theo 12 tháng trong năm hiện tại (Dùng làm dữ liệu vẽ Biểu đồ đường/cột)

            var currentYear = DateTime.Now.Year;

            var monthlyRevenueData = await _context.DonHangs

                .Where(o => o.NgayDat.Year == currentYear && (o.TrangThaiDonHang == "Hoàn thành" || o.TrangThaiThanhToan == "Đã thanh toán"))

                .GroupBy(o => o.NgayDat.Month)

                .Select(g => new { Month = g.Key, Revenue = g.Sum(o => o.TongTien) })

                .ToListAsync();



            // Khởi tạo mảng 12 phần tử tương ứng 12 tháng với giá trị doanh thu mặc định bằng 0

            decimal[] monthlyRevenues = new decimal[12];

            foreach (var item in monthlyRevenueData)

            {

                if (item.Month >= 1 && item.Month <= 12)

                {

                    monthlyRevenues[item.Month - 1] = item.Revenue; // Gán doanh thu thực tế vào đúng tháng (1-indexed về 0-indexed)

                }

            }

            ViewBag.MonthlyRevenues = monthlyRevenues; // Truyền mảng doanh thu sang View cho thư viện đồ họa vẽ biểu đồ



            // 6. Thống kê Top 5 sản phẩm bán chạy nhất (Dùng làm dữ liệu vẽ Biểu đồ tròn)

            var topSellingData = await _context.ChiTietDonHangs

                .Include(d => d.BienTheSanPham!)

                    .ThenInclude(b => b.SanPham)

                .Where(d => completedOrderIds.Contains(d.MaDonHang) && d.BienTheSanPham != null && d.BienTheSanPham.SanPham != null)

                .GroupBy(d => d.BienTheSanPham!.SanPham!.TenSanPham)

                .Select(g => new { ProductName = g.Key, Quantity = g.Sum(d => d.SoLuong) })

                .OrderByDescending(x => x.Quantity)

                .Take(5) // Chỉ lấy 5 sản phẩm bán nhiều nhất

                .ToListAsync();



            // Phân tách Tên sản phẩm và Số lượng tương ứng thành 2 mảng để dễ cấu hình biểu đồ

            ViewBag.TopSellingNames = topSellingData.Select(x => x.ProductName).ToArray();

            ViewBag.TopSellingQuantities = topSellingData.Select(x => x.Quantity).ToArray();



            // Gửi dữ liệu các số liệu thống kê thô ra View

            ViewBag.TotalRevenue = totalRevenue;

            ViewBag.TotalProductsSold = totalProductsSold;

            ViewBag.TotalOrdersCount = totalOrdersCount;

            ViewBag.PendingOrdersCount = pendingOrdersCount;

            ViewBag.TotalProductsCount = totalProductsCount;

            ViewBag.TotalCategoriesCount = totalCategoriesCount;

            ViewBag.TotalStockCount = totalStockCount;



            // 7. Lấy danh sách 5 đơn hàng mới đặt gần đây nhất để hiển thị nhanh trên bảng tin Dashboard

            var recentOrders = await _context.DonHangs

                .Include(o => o.NguoiDung)

                .OrderByDescending(o => o.NgayDat)

                .Take(5)

                .ToListAsync();



            return View(recentOrders);

        }


        /// <summary>
        /// API Lấy dự báo kinh doanh từ AI (Tồn kho & Bán hàng).
        /// Chạy ngầm bằng AJAX trên Dashboard để không làm chậm trang.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAiForecast()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Quản trị viên") return Unauthorized();

            try
            {
                var startDate = DateTime.Now.AddDays(-30);

                // Tổng hợp tồn kho
                var inventoryData = await _context.KhoHangs
                    .Include(k => k.BienTheSanPham!)
                        .ThenInclude(b => b.SanPham)
                    .Where(k => k.BienTheSanPham != null && k.BienTheSanPham.SanPham != null)
                    .GroupBy(k => new { k.BienTheSanPham!.SanPham!.MaSanPham, k.BienTheSanPham.SanPham.TenSanPham })
                    .Select(g => new
                    {
                        MaSanPham = g.Key.MaSanPham,
                        TenSanPham = g.Key.TenSanPham,
                        TongTonKho = g.Sum(k => k.SoLuongTon)
                    })
                    .ToListAsync();

                // Tổng hợp doanh số 30 ngày qua
                var salesData = await _context.ChiTietDonHangs
                    .Include(c => c.DonHang)
                    .Include(c => c.BienTheSanPham!)
                        .ThenInclude(b => b.SanPham)
                    .Where(c => c.DonHang != null && c.DonHang.NgayDat >= startDate && (c.DonHang.TrangThaiDonHang == "Hoàn thành" || c.DonHang.TrangThaiThanhToan == "Đã thanh toán"))
                    .GroupBy(c => c.BienTheSanPham!.SanPham!.MaSanPham)
                    .Select(g => new
                    {
                        MaSanPham = g.Key,
                        TongDaBan = g.Sum(c => c.SoLuong)
                    })
                    .ToListAsync();

                // Build report data for AI
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("ID | Tên SP | Tồn kho | Đã bán 30 ngày qua");
                
                foreach(var item in inventoryData)
                {
                    var sold = salesData.FirstOrDefault(s => s.MaSanPham == item.MaSanPham)?.TongDaBan ?? 0;
                    sb.AppendLine($"{item.MaSanPham} | {item.TenSanPham} | {item.TongTonKho} | {sold}");
                }

                var forecast = await _geminiService.AnalyzeInventoryAndDemandAsync(sb.ToString());
                return Ok(new { success = true, data = forecast });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi xử lý báo cáo AI: " + ex.Message });
            }
        }

        /// <summary>
        /// GET: /Home/ExportDoanhThuExcel
        /// Xuất báo cáo doanh thu kế toán theo 12 tháng ra Excel (.xlsx)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportDoanhThuExcel()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Quản trị viên") return RedirectToAction("Login", "Auth");

            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("FASHION STORE Admin");

            var currentYear = DateTime.Now.Year;
            var completedOrders = await _context.DonHangs
                .Include(o => o.NguoiDung)
                .Where(o => o.TrangThaiDonHang == "Hoàn thành" || o.TrangThaiThanhToan == "Đã thanh toán")
                .OrderByDescending(o => o.NgayDat)
                .ToListAsync();

            using var package = new OfficeOpenXml.ExcelPackage();

            // Sheet 1: Doanh thu theo tháng
            var wsMonth = package.Workbook.Worksheets.Add("DoanhThuThang");
            wsMonth.Cells[1, 1].Value = "Tháng";
            wsMonth.Cells[1, 2].Value = "Số Đơn Hoàn Tất";
            wsMonth.Cells[1, 3].Value = "Tổng Doanh Thu (₫)";
            wsMonth.Cells[1, 4].Value = "Giá Trị Đơn Trung Bình (₫)";

            using (var range = wsMonth.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 41, 59));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            int mRow = 2;
            for (int m = 1; m <= 12; m++)
            {
                var ordersInMonth = completedOrders.Where(o => o.NgayDat.Year == currentYear && o.NgayDat.Month == m).ToList();
                decimal rev = ordersInMonth.Sum(o => o.TongTien);
                int count = ordersInMonth.Count;
                decimal aov = count > 0 ? rev / count : 0;

                wsMonth.Cells[mRow, 1].Value = $"Tháng {m:D2}/{currentYear}";
                wsMonth.Cells[mRow, 2].Value = count;
                wsMonth.Cells[mRow, 3].Value = rev;
                wsMonth.Cells[mRow, 3].Style.Numberformat.Format = "#,##0";
                wsMonth.Cells[mRow, 4].Value = aov;
                wsMonth.Cells[mRow, 4].Style.Numberformat.Format = "#,##0";
                mRow++;
            }
            wsMonth.Cells[wsMonth.Dimension.Address].AutoFitColumns();

            var fileContents = package.GetAsByteArray();
            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_DoanhThu_{currentYear}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}