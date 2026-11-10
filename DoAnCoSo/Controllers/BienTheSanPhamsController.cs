using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý các biến thể sản phẩm (Kích thước, Màu sắc, Giá, Ảnh...) dành cho Admin.
    /// Yêu cầu xác thực quyền Admin thông qua bộ lọc AdminAuthorize.
    /// </summary>
    [AdminAuthorize]
    public class BienTheSanPhamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Constructor của BienTheSanPhamsController, inject DbContext và Environment để lưu trữ file ảnh.
        /// </summary>
        public BienTheSanPhamsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// GET: BienTheSanPhams
        /// Hiển thị danh sách tất cả các biến thể sản phẩm có trong hệ thống kèm tên sản phẩm chính.
        /// Có hỗ trợ lọc theo mã sản phẩm cụ thể nếu truyền maSanPham.
        /// </summary>
        public async Task<IActionResult> Index(int? maSanPham)
        {
            var query = _context.BienTheSanPhams
                .Include(b => b.SanPham) // Lấy thêm thông tin sản phẩm cha
                .AsNoTracking();

            if (maSanPham.HasValue && maSanPham.Value > 0)
            {
                query = query.Where(b => b.MaSanPham == maSanPham.Value);
                ViewBag.SelectedSanPham = await _context.SanPhams.FindAsync(maSanPham.Value);
            }

            var data = await query.ToListAsync();
            return View(data);
        }

        /// <summary>
        /// GET: BienTheSanPhams/Create
        /// Hiển thị form thêm mới biến thể sản phẩm.
        /// Nhận maSanPham tùy chọn để chọn trước sản phẩm và kế thừa giá / ảnh gốc.
        /// </summary>
        public async Task<IActionResult> Create(int? maSanPham)
        {
            var model = new BienTheSanPham();
            if (maSanPham.HasValue && maSanPham.Value > 0)
            {
                var sp = await _context.SanPhams.FindAsync(maSanPham.Value);
                if (sp != null)
                {
                    model.MaSanPham = sp.MaSanPham;
                    model.Gia = sp.Gia;
                    model.HinhAnh = sp.HinhAnh;
                }
                LoadSanPhamDropDown(maSanPham.Value);
            }
            else
            {
                LoadSanPhamDropDown(); // Nạp danh sách sản phẩm vào dropdown list
            }
            return View(model);
        }

        /// <summary>
        /// POST: BienTheSanPhams/Create
        /// Xử lý dữ liệu gửi lên để thêm mới biến thể sản phẩm (bao gồm cả upload file hình ảnh).
        /// Tự động khởi tạo tồn kho nếu chưa có.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BienTheSanPham model, IFormFile? imageFile)
        {
            // Kiểm tra và lưu file ảnh nếu có
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

            // Nếu dữ liệu model không hợp lệ
            if (!ModelState.IsValid)
            {
                LoadSanPhamDropDown(model.MaSanPham);
                return View(model);
            }

            _context.BienTheSanPhams.Add(model);
            await _context.SaveChangesAsync();

            // Tự động tạo bản ghi tồn kho cho biến thể mới nếu chưa có
            if (!_context.KhoHangs.Any(k => k.MaBienThe == model.MaBienThe))
            {
                _context.KhoHangs.Add(new KhoHang
                {
                    MaBienThe = model.MaBienThe,
                    SoLuongTon = 50, // Khởi tạo 50 sản phẩm trong kho
                    NgayCapNhat = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { maSanPham = model.MaSanPham });
        }

        /// <summary>
        /// GET: BienTheSanPhams/Edit/5
        /// Hiển thị form chỉnh sửa thông tin biến thể sản phẩm theo ID.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var bienThe = await _context.BienTheSanPhams.FindAsync(id);
            if (bienThe == null) return NotFound();

            LoadSanPhamDropDown(bienThe.MaSanPham);
            return View(bienThe);
        }

        /// <summary>
        /// POST: BienTheSanPhams/Edit/5
        /// Xử lý cập nhật thông tin biến thể sản phẩm (bao gồm thay đổi ảnh cũ nếu tải lên ảnh mới).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BienTheSanPham model, IFormFile? imageFile)
        {
            if (id != model.MaBienThe) return NotFound();

            var bienTheDb = await _context.BienTheSanPhams.FindAsync(id);
            if (bienTheDb == null) return NotFound();

            // Nếu người dùng tải lên ảnh mới
            if (imageFile != null)
            {
                var newImagePath = await SaveImageAsync(imageFile);
                if (newImagePath == null)
                {
                    ModelState.AddModelError("", "Chỉ được upload file ảnh hợp lệ (jpg, jpeg, png, webp, gif).");
                }
                else
                {
                    // Xóa ảnh cũ trên server
                    DeleteImageFile(bienTheDb.HinhAnh);
                    bienTheDb.HinhAnh = newImagePath;
                }
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                LoadSanPhamDropDown(model.MaSanPham);
                model.HinhAnh = bienTheDb.HinhAnh; // Giữ lại ảnh cũ
                return View(model);
            }

            // Cập nhật các trường thông tin
            bienTheDb.MaSanPham = model.MaSanPham;
            bienTheDb.KichCo = model.KichCo;
            bienTheDb.MauSac = model.MauSac;
            bienTheDb.Gia = model.Gia;
            bienTheDb.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: BienTheSanPhams/Details/5
        /// Hiển thị thông tin chi tiết một biến thể sản phẩm theo ID.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var bienThe = await _context.BienTheSanPhams
                .Include(b => b.SanPham)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.MaBienThe == id);

            if (bienThe == null) return NotFound();

            return View(bienThe);
        }

        /// <summary>
        /// GET: BienTheSanPhams/Delete/5
        /// Hiển thị trang xác nhận xóa một biến thể sản phẩm.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var bienThe = await _context.BienTheSanPhams
                .Include(b => b.SanPham)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.MaBienThe == id);

            if (bienThe == null) return NotFound();

            return View(bienThe);
        }

        /// <summary>
        /// POST: BienTheSanPhams/Delete/5
        /// Xử lý xóa biến thể sản phẩm khỏi cơ sở dữ liệu và xóa ảnh tương ứng trên ổ cứng.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bienThe = await _context.BienTheSanPhams.FindAsync(id);
            if (bienThe == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // Xóa file ảnh trên đĩa cứng
            DeleteImageFile(bienThe.HinhAnh);

            _context.BienTheSanPhams.Remove(bienThe);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Nạp danh sách sản phẩm vào DropdownList của ViewBag.
        /// </summary>
        private void LoadSanPhamDropDown(object? selectedValue = null)
        {
            ViewBag.MaSanPham = new SelectList(
                _context.SanPhams.AsNoTracking().ToList(),
                "MaSanPham",
                "TenSanPham",
                selectedValue
            );
        }

        /// <summary>
        /// Lưu trữ hình ảnh biến thể tải lên hệ thống.
        /// </summary>
        /// <returns>Đường dẫn tương đối tới ảnh lưu được, hoặc null nếu không hợp lệ</returns>
        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "variants");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/variants/{uniqueFileName}";
        }

        /// <summary>
        /// Xóa file hình ảnh khỏi máy chủ nếu tồn tại.
        /// </summary>
        private void DeleteImageFile(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
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