using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý các cấu hình hệ thống & trang Giới thiệu dành cho Admin.
    /// Yêu cầu quyền truy cập Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class CauHinhsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CauHinhsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// GET: CauHinhs
        /// Hiển thị trang cấu hình hệ thống chung (API Key, Zalo, Phí ship).
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var apiKeyConfig = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Gemini_API_Key");
            var zaloConfig = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Zalo_Link");
            var hotlineConfig = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Hotline");
            var emailConfig = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Email");
            var addressConfig = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Address");
            var decartConfig = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Decart_API_Key");

            ViewBag.GeminiApiKey = apiKeyConfig?.Value ?? string.Empty;
            ViewBag.DecartApiKey = decartConfig?.Value ?? "dct_test_EoXNbcuxWjeBFMaspizrmUwHXhbNDrOwOizJUoXLbdesVsagespbmGdvgcpxygqs";
            ViewBag.ZaloLink = zaloConfig?.Value ?? string.Empty;
            ViewBag.Hotline = hotlineConfig?.Value ?? "1900 8079";
            ViewBag.Email = emailConfig?.Value ?? "cskh@fashionstore.vn";
            ViewBag.Address = addressConfig?.Value ?? "Hà Nội, Việt Nam";

            return View();
        }

        /// <summary>
        /// POST: CauHinhs/Save
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(string geminiApiKey, string decartApiKey, string zaloLink, string hotline, string email, string address)
        {
            await SetConfigValueAsync("Gemini_API_Key", geminiApiKey?.Trim() ?? string.Empty, "Khóa API Google Gemini");
            await SetConfigValueAsync("Decart_API_Key", decartApiKey?.Trim() ?? string.Empty, "Khóa API Decart AI (Phòng thử đồ Anywear Live VTON)");
            await SetConfigValueAsync("Zalo_Link", zaloLink?.Trim() ?? string.Empty, "Liên kết Zalo hỗ trợ");
            await SetConfigValueAsync("Hotline", hotline?.Trim() ?? string.Empty, "Số điện thoại CSKH");
            await SetConfigValueAsync("Email", email?.Trim() ?? string.Empty, "Email CSKH");
            await SetConfigValueAsync("Address", address?.Trim() ?? string.Empty, "Địa chỉ văn phòng");

            try
            {
                await _context.SaveChangesAsync();
                TempData["SettingsSuccess"] = "Cập nhật cấu hình hệ thống thành công!";
            }
            catch (Exception ex)
            {
                TempData["SettingsError"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: CauHinhs/GioiThieu
        /// Trang chỉnh sửa nội dung trang Giới Thiệu (About Us)
        /// </summary>
        public async Task<IActionResult> GioiThieu()
        {
            var model = new CauHinhGioiThieuViewModel
            {
                HeroEyebrow = await GetConfigValueAsync("About_Hero_Eyebrow", "Fashion Store — Câu chuyện của chúng tôi"),
                HeroTitle = await GetConfigValueAsync("About_Hero_Title", "Về Chúng Tôi"),
                HeroImage = await GetConfigValueAsync("About_Hero_Image", "/images/store-interior.jpg.jpg"),

                StatProducts = await GetConfigValueAsync("About_Stat_Products", "50+"),
                StatCustomers = await GetConfigValueAsync("About_Stat_Customers", "10.000+"),
                StatYears = await GetConfigValueAsync("About_Stat_Years", "5+"),
                StatProvinces = await GetConfigValueAsync("About_Stat_Provinces", "63+"),

                MissionTitle = await GetConfigValueAsync("About_Mission_Title", "Thời trang Không giới hạn"),
                MissionP1 = await GetConfigValueAsync("About_Mission_P1", "Chúng tôi tin rằng thời trang không chỉ là quần áo — đó là ngôn ngữ của bản sắc. Mỗi sản phẩm được thiết kế với tư duy tối giản nhưng đậm chất cá tính, mang đến sự tự tin cho người mặc trong từng khoảnh khắc."),
                MissionP2 = await GetConfigValueAsync("About_Mission_P2", "Từ những bước chạy bộ buổi sáng đến những buổi tối đô thị sôi động — Fashion Store đồng hành cùng bạn với chất lượng vượt trội và phong cách không bao giờ lỗi thời."),

                StoryTitle = await GetConfigValueAsync("About_Story_Title", "Sinh ra từ đam mê thời trang"),
                StoryP1 = await GetConfigValueAsync("About_Story_P1", "Khởi đầu từ một cửa hàng nhỏ với ước mơ lớn vào năm 2020, Fashion Store đã vươn mình trở thành một trong những thương hiệu thời trang được yêu thích nhất tại Việt Nam."),
                StoryP2 = await GetConfigValueAsync("About_Story_P2", "Chúng tôi không ngừng tìm kiếm những chất liệu tốt nhất, những thiết kế tinh tế nhất — tất cả vì một mục tiêu duy nhất: mang lại giá trị thực sự cho khách hàng."),
                StoryImage = await GetConfigValueAsync("About_Story_Image", "/images/store-interior.jpg.jpg"),

                Value1Title = await GetConfigValueAsync("About_Value1_Title", "Chất lượng tuyệt đối"),
                Value1Desc = await GetConfigValueAsync("About_Value1_Desc", "Mỗi sản phẩm đều trải qua quy trình kiểm định nghiêm ngặt trước khi đến tay khách hàng. Không có chỗ cho sự thỏa hiệp."),

                Value2Title = await GetConfigValueAsync("About_Value2_Title", "Thiết kế hiện đại"),
                Value2Desc = await GetConfigValueAsync("About_Value2_Desc", "Cập nhật liên tục các xu hướng thời trang quốc tế, dung hòa hoàn hảo với phom dáng và khí hậu Việt Nam."),

                Value3Title = await GetConfigValueAsync("About_Value3_Title", "Tận tâm phục vụ"),
                Value3Desc = await GetConfigValueAsync("About_Value3_Desc", "Khách hàng là trung tâm của mọi quyết định. Chính sách đổi trả linh hoạt và hỗ trợ tư vấn 24/7.")
            };

            return View(model);
        }

        /// <summary>
        /// POST: CauHinhs/SaveGioiThieu
        /// Lưu chỉnh sửa trang Giới Thiệu
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGioiThieu(CauHinhGioiThieuViewModel model, IFormFile? heroImageFile, IFormFile? storyImageFile)
        {
            if (ModelState.IsValid)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "about");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (heroImageFile != null && heroImageFile.Length > 0)
                {
                    var fileName = "hero_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(heroImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await heroImageFile.CopyToAsync(stream);
                    }
                    model.HeroImage = "/images/about/" + fileName;
                }

                if (storyImageFile != null && storyImageFile.Length > 0)
                {
                    var fileName = "story_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(storyImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await storyImageFile.CopyToAsync(stream);
                    }
                    model.StoryImage = "/images/about/" + fileName;
                }

                await SetConfigValueAsync("About_Hero_Eyebrow", model.HeroEyebrow ?? "", "Tiêu đề phụ banner giới thiệu");
                await SetConfigValueAsync("About_Hero_Title", model.HeroTitle ?? "", "Tiêu đề chính banner giới thiệu");
                await SetConfigValueAsync("About_Hero_Image", model.HeroImage ?? "", "Ảnh nền banner giới thiệu");

                await SetConfigValueAsync("About_Stat_Products", model.StatProducts ?? "", "Số liệu: Sản phẩm");
                await SetConfigValueAsync("About_Stat_Customers", model.StatCustomers ?? "", "Số liệu: Khách hàng");
                await SetConfigValueAsync("About_Stat_Years", model.StatYears ?? "", "Số liệu: Năm kinh nghiệm");
                await SetConfigValueAsync("About_Stat_Provinces", model.StatProvinces ?? "", "Số liệu: Tỉnh thành");

                await SetConfigValueAsync("About_Mission_Title", model.MissionTitle ?? "", "Tiêu đề sứ mệnh");
                await SetConfigValueAsync("About_Mission_P1", model.MissionP1 ?? "", "Đoạn 1 sứ mệnh");
                await SetConfigValueAsync("About_Mission_P2", model.MissionP2 ?? "", "Đoạn 2 sứ mệnh");

                await SetConfigValueAsync("About_Story_Title", model.StoryTitle ?? "", "Tiêu đề câu chuyện");
                await SetConfigValueAsync("About_Story_P1", model.StoryP1 ?? "", "Đoạn 1 câu chuyện");
                await SetConfigValueAsync("About_Story_P2", model.StoryP2 ?? "", "Đoạn 2 câu chuyện");
                await SetConfigValueAsync("About_Story_Image", model.StoryImage ?? "", "Ảnh câu chuyện");

                await SetConfigValueAsync("About_Value1_Title", model.Value1Title ?? "", "Giá trị 1: Tiêu đề");
                await SetConfigValueAsync("About_Value1_Desc", model.Value1Desc ?? "", "Giá trị 1: Mô tả");

                await SetConfigValueAsync("About_Value2_Title", model.Value2Title ?? "", "Giá trị 2: Tiêu đề");
                await SetConfigValueAsync("About_Value2_Desc", model.Value2Desc ?? "", "Giá trị 2: Mô tả");

                await SetConfigValueAsync("About_Value3_Title", model.Value3Title ?? "", "Giá trị 3: Tiêu đề");
                await SetConfigValueAsync("About_Value3_Desc", model.Value3Desc ?? "", "Giá trị 3: Mô tả");

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật toàn bộ nội dung trang Giới thiệu thành công!";
            }

            return RedirectToAction(nameof(GioiThieu));
        }

        private async Task<string> GetConfigValueAsync(string key, string defaultValue)
        {
            var conf = await _context.CauHinhHeThongs.FirstOrDefaultAsync(c => c.Key == key);
            return conf != null && !string.IsNullOrEmpty(conf.Value) ? conf.Value : defaultValue;
        }

        private async Task SetConfigValueAsync(string key, string value, string description)
        {
            var conf = await _context.CauHinhHeThongs.FirstOrDefaultAsync(c => c.Key == key);
            if (conf == null)
            {
                conf = new CauHinhHeThong { Key = key, Value = value, Description = description };
                _context.CauHinhHeThongs.Add(conf);
            }
            else
            {
                conf.Value = value;
                if (!string.IsNullOrEmpty(description)) conf.Description = description;
                _context.CauHinhHeThongs.Update(conf);
            }
        }
    }
}
