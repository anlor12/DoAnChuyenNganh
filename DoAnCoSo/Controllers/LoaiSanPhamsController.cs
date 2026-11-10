using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý danh mục Loại sản phẩm (ví dụ: Quần, Áo, Phụ kiện) dành cho Admin.
    /// Yêu cầu xác thực Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class LoaiSanPhamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Constructor của LoaiSanPhamsController.
        /// </summary>
        public LoaiSanPhamsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// GET: LoaiSanPhams
        /// Hiển thị danh sách tất cả các loại danh mục sản phẩm hiện có theo phân cấp.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var data = await _context.LoaiSanPhams
                .Include(l => l.LoaiCha)
                .Include(l => l.LoaiCons)
                .OrderBy(l => l.MaLoaiCha.HasValue ? 1 : 0)
                .ThenBy(l => l.TenLoaiSanPham)
                .ToListAsync();
            return View(data);
        }

        /// <summary>
        /// GET: LoaiSanPhams/Create
        /// Hiển thị form tạo mới loại sản phẩm.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            var categories = await _context.LoaiSanPhams.OrderBy(x => x.TenLoaiSanPham).ToListAsync();
            ViewBag.MaLoaiCha = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "MaLoaiSanPham", "TenLoaiSanPham");
            return View();
        }

        /// <summary>
        /// POST: LoaiSanPhams/Create
        /// Xử lý lưu loại sản phẩm mới và lưu file ảnh danh mục nếu được tải lên.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiSanPham model, IFormFile? imageFile)
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

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var categories = await _context.LoaiSanPhams.OrderBy(x => x.TenLoaiSanPham).ToListAsync();
            ViewBag.MaLoaiCha = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "MaLoaiSanPham", "TenLoaiSanPham", model.MaLoaiCha);
            return View(model);
        }

        /// <summary>
        /// GET: LoaiSanPhams/Edit/5
        /// Hiển thị form chỉnh sửa thông tin loại sản phẩm theo ID.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var loaiSanPham = await _context.LoaiSanPhams.FindAsync(id);
            if (loaiSanPham == null) return NotFound();

            var categories = await _context.LoaiSanPhams.Where(x => x.MaLoaiSanPham != id).OrderBy(x => x.TenLoaiSanPham).ToListAsync();
            ViewBag.MaLoaiCha = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "MaLoaiSanPham", "TenLoaiSanPham", loaiSanPham.MaLoaiCha);
            return View(loaiSanPham);
        }

        /// <summary>
        /// POST: LoaiSanPhams/Edit/5
        /// Xử lý cập nhật thông tin loại sản phẩm và đổi hình ảnh mới nếu có tải lên.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LoaiSanPham model, IFormFile? imageFile)
        {
            if (id != model.MaLoaiSanPham) return NotFound();

            var loaiDb = await _context.LoaiSanPhams.FindAsync(id);
            if (loaiDb == null) return NotFound();

            if (imageFile != null)
            {
                var newImagePath = await SaveImageAsync(imageFile);
                if (newImagePath == null)
                {
                    ModelState.AddModelError("", "Chỉ được upload file ảnh hợp lệ (jpg, jpeg, png, webp, gif).");
                }
                else
                {
                    // Xóa ảnh cũ trên server nếu không phải là ảnh seed URL
                    DeleteImageFile(loaiDb.HinhAnh);
                    loaiDb.HinhAnh = newImagePath;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    loaiDb.TenLoaiSanPham = model.TenLoaiSanPham;
                    loaiDb.MoTa = model.MoTa;
                    loaiDb.MaLoaiCha = model.MaLoaiCha;
                    
                    _context.Update(loaiDb);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiSanPhamExists(model.MaLoaiSanPham))
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
            
            model.HinhAnh = loaiDb.HinhAnh;
            var categories = await _context.LoaiSanPhams.Where(x => x.MaLoaiSanPham != id).OrderBy(x => x.TenLoaiSanPham).ToListAsync();
            ViewBag.MaLoaiCha = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "MaLoaiSanPham", "TenLoaiSanPham", model.MaLoaiCha);
            return View(model);
        }

        /// <summary>
        /// GET: LoaiSanPhams/Details/5
        /// Hiển thị thông tin chi tiết một loại sản phẩm.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var loaiSanPham = await _context.LoaiSanPhams
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaLoaiSanPham == id);

            if (loaiSanPham == null) return NotFound();

            return View(loaiSanPham);
        }

        /// <summary>
        /// GET: LoaiSanPhams/Delete/5
        /// Hiển thị trang xác thực xóa loại sản phẩm.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var loaiSanPham = await _context.LoaiSanPhams
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaLoaiSanPham == id);

            if (loaiSanPham == null) return NotFound();

            return View(loaiSanPham);
        }

        /// <summary>
        /// POST: LoaiSanPhams/Delete/5
        /// Xóa loại sản phẩm và tập tin ảnh liên kết trên đĩa.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loaiSanPham = await _context.LoaiSanPhams
                .Include(l => l.LoaiCons)
                .FirstOrDefaultAsync(l => l.MaLoaiSanPham == id);

            if (loaiSanPham != null)
            {
                // Cập nhật các danh mục con: chuyển về danh mục cha hoặc thành danh mục gốc
                if (loaiSanPham.LoaiCons != null && loaiSanPham.LoaiCons.Any())
                {
                    foreach (var con in loaiSanPham.LoaiCons)
                    {
                        con.MaLoaiCha = loaiSanPham.MaLoaiCha;
                    }
                }

                // Cập nhật sản phẩm thuộc danh mục này: chuyển sang danh mục cha hoặc danh mục còn lại
                var sanPhams = await _context.SanPhams.Where(p => p.MaLoaiSanPham == id).ToListAsync();
                if (sanPhams.Any())
                {
                    int? targetCatId = loaiSanPham.MaLoaiCha;
                    if (!targetCatId.HasValue)
                    {
                        var fallback = await _context.LoaiSanPhams.FirstOrDefaultAsync(c => c.MaLoaiSanPham != id);
                        if (fallback != null)
                        {
                            targetCatId = fallback.MaLoaiSanPham;
                        }
                    }

                    if (targetCatId.HasValue)
                    {
                        foreach (var p in sanPhams)
                        {
                            p.MaLoaiSanPham = targetCatId.Value;
                        }
                    }
                }

                // Xóa ảnh liên kết
                DeleteImageFile(loaiSanPham.HinhAnh);
                _context.LoaiSanPhams.Remove(loaiSanPham);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kiểm tra sự tồn tại của loại sản phẩm theo ID.
        /// </summary>
        private bool LoaiSanPhamExists(int id)
        {
            return _context.LoaiSanPhams.Any(e => e.MaLoaiSanPham == id);
        }

        /// <summary>
        /// Lưu ảnh danh mục vào thư mục images/categories trên server.
        /// </summary>
        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/categories/{uniqueFileName}";
        }

        /// <summary>
        /// Xóa tập tin hình ảnh khỏi đĩa cứng.
        /// </summary>
        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            // Không xóa nếu đây là link ảnh seed online
            if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;

            // Chỉ xóa file nếu nằm trong thư mục uploads của categories (/images/categories/)
            var normalized = imagePath.Replace("\\", "/").Trim();
            if (!normalized.StartsWith("/images/categories/", StringComparison.OrdinalIgnoreCase) &&
                !normalized.StartsWith("images/categories/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = imagePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch
                {
                    // Bỏ qua lỗi nếu file đang bị khóa bởi tiến trình khác
                }
            }
        }
    }
}