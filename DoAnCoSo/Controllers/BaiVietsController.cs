using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý Bài viết / Tin tức dành riêng cho Admin.
    /// </summary>
    [AdminAuthorize]
    public class BaiVietsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BaiVietsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// GET: BaiViets
        /// Danh sách tất cả bài viết kèm bộ lọc tìm kiếm
        /// </summary>
        public async Task<IActionResult> Index(string? tuKhoa, string? chuyenMuc, bool? trangThai)
        {
            var query = _context.BaiViets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim().ToLower();
                query = query.Where(b => b.TieuDe.ToLower().Contains(tuKhoa) || 
                                         (b.TomTat != null && b.TomTat.ToLower().Contains(tuKhoa)) ||
                                         (b.TacGia != null && b.TacGia.ToLower().Contains(tuKhoa)));
                ViewBag.TuKhoa = tuKhoa;
            }

            if (!string.IsNullOrWhiteSpace(chuyenMuc) && chuyenMuc != "All")
            {
                query = query.Where(b => b.ChuyenMuc == chuyenMuc);
                ViewBag.ChuyenMuc = chuyenMuc;
            }

            if (trangThai.HasValue)
            {
                query = query.Where(b => b.TrangThai == trangThai.Value);
                ViewBag.TrangThai = trangThai.Value;
            }

            var list = await query.OrderByDescending(b => b.NoiBat)
                                  .ThenByDescending(b => b.NgayTao)
                                  .ToListAsync();

            ViewBag.Categories = await _context.BaiViets
                .Where(b => !string.IsNullOrEmpty(b.ChuyenMuc))
                .Select(b => b.ChuyenMuc!)
                .Distinct()
                .ToListAsync();

            return View(list);
        }

        /// <summary>
        /// GET: BaiViets/Create
        /// Form tạo bài viết mới
        /// </summary>
        public IActionResult Create()
        {
            var model = new BaiViet
            {
                TacGia = HttpContext.Session.GetString("AdminUser") ?? "Ban biên tập",
                ChuyenMuc = "Xu hướng thời trang",
                TrangThai = true,
                NoiBat = false
            };
            return View(model);
        }

        /// <summary>
        /// POST: BaiViets/Create
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BaiViet baiViet, IFormFile? anhDaiDienFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh nếu có
                if (anhDaiDienFile != null && anhDaiDienFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "news");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString().Substring(0, 8) + "_" + Path.GetFileName(anhDaiDienFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await anhDaiDienFile.CopyToAsync(fileStream);
                    }
                    baiViet.HinhAnh = "/images/news/" + uniqueFileName;
                }

                // Tự sinh slug nếu chưa có
                if (string.IsNullOrWhiteSpace(baiViet.Slug))
                {
                    baiViet.Slug = GenerateSlug(baiViet.TieuDe);
                }

                baiViet.NgayTao = DateTime.Now;
                _context.Add(baiViet);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm bài viết mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(baiViet);
        }

        /// <summary>
        /// GET: BaiViets/Edit/5
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var baiViet = await _context.BaiViets.FindAsync(id);
            if (baiViet == null) return NotFound();

            return View(baiViet);
        }

        /// <summary>
        /// POST: BaiViets/Edit/5
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BaiViet baiViet, IFormFile? anhDaiDienFile)
        {
            if (id != baiViet.MaBaiViet) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.BaiViets.FindAsync(id);
                    if (existing == null) return NotFound();

                    // Xử lý upload ảnh mới nếu có
                    if (anhDaiDienFile != null && anhDaiDienFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "news");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString().Substring(0, 8) + "_" + Path.GetFileName(anhDaiDienFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await anhDaiDienFile.CopyToAsync(fileStream);
                        }
                        existing.HinhAnh = "/images/news/" + uniqueFileName;
                    }
                    else if (!string.IsNullOrWhiteSpace(baiViet.HinhAnh))
                    {
                        existing.HinhAnh = baiViet.HinhAnh;
                    }

                    existing.TieuDe = baiViet.TieuDe;
                    existing.Slug = string.IsNullOrWhiteSpace(baiViet.Slug) ? GenerateSlug(baiViet.TieuDe) : baiViet.Slug;
                    existing.TomTat = baiViet.TomTat;
                    existing.NoiDung = baiViet.NoiDung;
                    existing.TacGia = baiViet.TacGia;
                    existing.ChuyenMuc = baiViet.ChuyenMuc;
                    existing.NoiBat = baiViet.NoiBat;
                    existing.TrangThai = baiViet.TrangThai;
                    existing.NgayCapNhat = DateTime.Now;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BaiVietExists(baiViet.MaBaiViet)) return NotFound();
                    else throw;
                }
            }
            return View(baiViet);
        }

        /// <summary>
        /// POST: BaiViets/Delete/5
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var baiViet = await _context.BaiViets.FindAsync(id);
            if (baiViet != null)
            {
                _context.BaiViets.Remove(baiViet);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa bài viết thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: BaiViets/ToggleTrangThai/5
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleTrangThai(int id)
        {
            var baiViet = await _context.BaiViets.FindAsync(id);
            if (baiViet == null) return Json(new { success = false, message = "Không tìm thấy bài viết" });

            baiViet.TrangThai = !baiViet.TrangThai;
            await _context.SaveChangesAsync();
            return Json(new { success = true, trangThai = baiViet.TrangThai });
        }

        /// <summary>
        /// POST: BaiViets/ToggleNoiBat/5
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleNoiBat(int id)
        {
            var baiViet = await _context.BaiViets.FindAsync(id);
            if (baiViet == null) return Json(new { success = false, message = "Không tìm thấy bài viết" });

            baiViet.NoiBat = !baiViet.NoiBat;
            await _context.SaveChangesAsync();
            return Json(new { success = true, noiBat = baiViet.NoiBat });
        }

        private bool BaiVietExists(int id)
        {
            return _context.BaiViets.Any(e => e.MaBaiViet == id);
        }

        private static string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title)) return "";
            string slug = title.ToLower().Trim();

            // Thay thế ký tự có dấu tiếng Việt
            string[] signs = new string[] {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < signs.Length; i++)
            {
                for (int j = 0; j < signs[i].Length; j++)
                {
                    slug = slug.Replace(signs[i][j], signs[0][i - 1]);
                }
            }

            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            return slug;
        }
    }
}
