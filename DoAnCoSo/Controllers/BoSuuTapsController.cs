using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý bộ sưu tập thời trang (gồm ảnh chụp lớn và các điểm ghim gắn link sản phẩm) dành cho Admin.
    /// Yêu cầu phân quyền AdminAuthorize.
    /// </summary>
    [AdminAuthorize]
    public class BoSuuTapsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Constructor inject CSDL và môi trường WebHostEnvironment.
        /// </summary>
        public BoSuuTapsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// GET: BoSuuTaps
        /// Hiển thị danh sách tất cả các bộ sưu tập thời trang đã thiết lập.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var sets = await _context.BoSuuTaps
                .Include(s => s.DiemGhimBoSuuTaps)
                .AsNoTracking()
                .ToListAsync();
            return View(sets);
        }

        /// <summary>
        /// GET: BoSuuTaps/Create
        /// Hiển thị giao diện tạo mới bộ sưu tập thời trang.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// POST: BoSuuTaps/Create
        /// Xử lý tạo mới BoSuuTap và upload ảnh nền.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BoSuuTap model, IFormFile? imageFile)
        {
            if (imageFile != null)
            {
                var imagePath = await SaveImageAsync(imageFile);
                if (imagePath == null)
                {
                    ModelState.AddModelError("", "Chỉ được upload file ảnh hợp lệ (jpg, jpeg, png, webp, gif).");
                }
                else
                {
                    model.HinhAnh = imagePath;
                }
            }
            else
            {
                ModelState.AddModelError("HinhAnh", "Vui lòng chọn hình ảnh nền cho bộ sưu tập.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo bộ sưu tập \"{model.TenBoSuuTap}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        /// <summary>
        /// GET: BoSuuTaps/Edit/5
        /// Hiển thị giao diện chỉnh sửa tên bộ sưu tập, ảnh nền, và hiển thị bộ chọn thêm điểm ghim sản phẩm trên ảnh.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var boSuuTap = await _context.BoSuuTaps
                .Include(s => s.DiemGhimBoSuuTaps!)
                    .ThenInclude(h => h.SanPham)
                .FirstOrDefaultAsync(m => m.MaBoSuuTap == id);

            if (boSuuTap == null) return NotFound();

            // Tải danh sách tất cả sản phẩm để gán link cho điểm ghim
            var products = await _context.SanPhams
                .OrderBy(s => s.TenSanPham)
                .ToListAsync();
            ViewBag.Products = new SelectList(products, "MaSanPham", "TenSanPham");

            return View(boSuuTap);
        }

        /// <summary>
        /// POST: BoSuuTaps/Edit/5
        /// Cập nhật thông tin cơ bản của bộ sưu tập.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BoSuuTap model, IFormFile? imageFile)
        {
            if (id != model.MaBoSuuTap) return NotFound();

            var setDb = await _context.BoSuuTaps.FindAsync(id);
            if (setDb == null) return NotFound();

            if (imageFile != null)
            {
                var newImagePath = await SaveImageAsync(imageFile);
                if (newImagePath == null)
                {
                    ModelState.AddModelError("", "Chỉ được upload file ảnh hợp lệ (jpg, jpeg, png, webp, gif).");
                }
                else
                {
                    DeleteImageFile(setDb.HinhAnh);
                    setDb.HinhAnh = newImagePath;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    setDb.TenBoSuuTap = model.TenBoSuuTap;
                    setDb.MoTa = model.MoTa;
                    setDb.TrangThai = model.TrangThai;
                    _context.Update(setDb);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật bộ sưu tập thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BoSuuTapExists(model.MaBoSuuTap))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            model.HinhAnh = setDb.HinhAnh;
            var products = await _context.SanPhams
                .OrderBy(s => s.TenSanPham)
                .ToListAsync();
            ViewBag.Products = new SelectList(products, "MaSanPham", "TenSanPham");
            return View(model);
        }

        /// <summary>
        /// POST: BoSuuTaps/AddHotspot
        /// Thêm một điểm ghim sản phẩm mới trên bức ảnh bộ sưu tập.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHotspot(int maBoSuuTap, decimal positionTop, decimal positionLeft, int maSanPham, string? tieuDe)
        {
            var hotspot = new DiemGhimBoSuuTap
            {
                MaBoSuuTap = maBoSuuTap,
                ToaDoTren = positionTop,
                ToaDoTrai = positionLeft,
                MaSanPham = maSanPham,
                TieuDe = tieuDe
            };

            _context.DiemGhimBoSuuTaps.Add(hotspot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm điểm ghim sản phẩm thành công!";
            return RedirectToAction(nameof(Edit), new { id = maBoSuuTap });
        }

        /// <summary>
        /// POST: BoSuuTaps/DeleteHotspot/5
        /// Xóa một điểm ghim trên ảnh.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHotspot(int id, int maBoSuuTap)
        {
            var hotspot = await _context.DiemGhimBoSuuTaps.FindAsync(id);
            if (hotspot != null)
            {
                _context.DiemGhimBoSuuTaps.Remove(hotspot);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa điểm ghim thành công!";
            }
            return RedirectToAction(nameof(Edit), new { id = maBoSuuTap });
        }

        /// <summary>
        /// GET: BoSuuTaps/Delete/5
        /// Hiển thị trang xác thực xóa bộ sưu tập.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var boSuuTap = await _context.BoSuuTaps
                .FirstOrDefaultAsync(m => m.MaBoSuuTap == id);

            if (boSuuTap == null) return NotFound();

            return View(boSuuTap);
        }

        /// <summary>
        /// POST: BoSuuTaps/Delete/5
        /// Xóa bộ sưu tập cùng tệp tin ảnh và các điểm ghim tương ứng.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var boSuuTap = await _context.BoSuuTaps.FindAsync(id);
            if (boSuuTap != null)
            {
                DeleteImageFile(boSuuTap.HinhAnh);
                _context.BoSuuTaps.Remove(boSuuTap);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa bộ sưu tập thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kiểm tra sự tồn tại của BoSuuTap.
        /// </summary>
        private bool BoSuuTapExists(int id)
        {
            return _context.BoSuuTaps.Any(e => e.MaBoSuuTap == id);
        }

        /// <summary>
        /// Lưu file ảnh nền bộ sưu tập lên server.
        /// </summary>
        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "lookbooks");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/lookbooks/{uniqueFileName}";
        }

        /// <summary>
        /// Xóa file ảnh khỏi máy chủ.
        /// </summary>
        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;

            var relativePath = imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
