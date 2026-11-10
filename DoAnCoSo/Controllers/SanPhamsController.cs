using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản trị danh sách sản phẩm (CRUD) dành cho Admin.
    /// Yêu cầu quyền truy cập Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class SanPhamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AuditLogService _auditLogService;

        /// <summary>
        /// Constructor inject DbContext, WebHostEnvironment và AuditLogService.
        /// </summary>
        public SanPhamsController(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            AuditLogService auditLogService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// GET: SanPhams
        /// Hiển thị danh sách tất cả các sản phẩm có kèm thông tin loại sản phẩm.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var sanPhams = await _context.SanPhams
                .Include(s => s.LoaiSanPham)
                .AsNoTracking()
                .ToListAsync();

            return View(sanPhams);
        }

        /// <summary>
        /// GET: SanPhams/Create
        /// Hiển thị giao diện form thêm sản phẩm mới.
        /// </summary>
        public IActionResult Create()
        {
            LoadLoaiSanPhamDropDown();
            return View();
        }

        /// <summary>
        /// POST: SanPhams/Create
        /// Xử lý tạo mới sản phẩm và upload tối đa 4 file ảnh (hoặc chọn hàng loạt tối đa 4 ảnh).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SanPham model, 
            IFormFile? imageFile, 
            IFormFile? imageFile2, 
            IFormFile? imageFile3, 
            IFormFile? imageFile4,
            List<IFormFile>? batchImages)
        {
            // 1. Xử lý tải ảnh hàng loạt nếu có
            if (batchImages != null && batchImages.Any())
            {
                var validFiles = batchImages.Where(f => f != null && f.Length > 0).Take(4).ToList();
                for (int i = 0; i < validFiles.Count; i++)
                {
                    var savedPath = await SaveImageAsync(validFiles[i]);
                    if (savedPath != null)
                    {
                        if (i == 0 && string.IsNullOrEmpty(model.HinhAnh)) model.HinhAnh = savedPath;
                        else if (i == 1 && string.IsNullOrEmpty(model.HinhAnh2)) model.HinhAnh2 = savedPath;
                        else if (i == 2 && string.IsNullOrEmpty(model.HinhAnh3)) model.HinhAnh3 = savedPath;
                        else if (i == 3 && string.IsNullOrEmpty(model.HinhAnh4)) model.HinhAnh4 = savedPath;
                    }
                }
            }

            // 2. Xử lý từng slot ảnh riêng biệt (nếu người dùng chọn theo từng ô 1..4)
            if (imageFile != null)
            {
                var img1 = await SaveImageAsync(imageFile);
                if (img1 != null) model.HinhAnh = img1;
            }
            if (imageFile2 != null)
            {
                var img2 = await SaveImageAsync(imageFile2);
                if (img2 != null) model.HinhAnh2 = img2;
            }
            if (imageFile3 != null)
            {
                var img3 = await SaveImageAsync(imageFile3);
                if (img3 != null) model.HinhAnh3 = img3;
            }
            if (imageFile4 != null)
            {
                var img4 = await SaveImageAsync(imageFile4);
                if (img4 != null) model.HinhAnh4 = img4;
            }

            if (!ModelState.IsValid)
            {
                LoadLoaiSanPhamDropDown(model.MaLoaiSanPham);
                return View(model);
            }

            _context.SanPhams.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: SanPhams/Edit/5
        /// Hiển thị form cập nhật thông tin sản phẩm.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sanPham = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);
            if (sanPham == null) return NotFound();

            LoadLoaiSanPhamDropDown(sanPham.MaLoaiSanPham);
            return View(sanPham);
        }

        /// <summary>
        /// POST: SanPhams/Edit/5
        /// Xử lý lưu các chỉnh sửa thông tin sản phẩm và cập nhật tối đa 4 ảnh.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, 
            SanPham model, 
            IFormFile? imageFile, 
            IFormFile? imageFile2, 
            IFormFile? imageFile3, 
            IFormFile? imageFile4,
            List<IFormFile>? batchImages,
            bool removeImage2 = false,
            bool removeImage3 = false,
            bool removeImage4 = false)
        {
            if (id != model.MaSanPham) return NotFound();

            var sanPhamDb = await _context.SanPhams.FindAsync(id);
            if (sanPhamDb == null) return NotFound();

            // 1. Tải ảnh hàng loạt nếu có
            if (batchImages != null && batchImages.Any())
            {
                var validFiles = batchImages.Where(f => f != null && f.Length > 0).Take(4).ToList();
                for (int i = 0; i < validFiles.Count; i++)
                {
                    var savedPath = await SaveImageAsync(validFiles[i]);
                    if (savedPath != null)
                    {
                        if (i == 0) { DeleteImageFile(sanPhamDb.HinhAnh); sanPhamDb.HinhAnh = savedPath; }
                        else if (i == 1) { DeleteImageFile(sanPhamDb.HinhAnh2); sanPhamDb.HinhAnh2 = savedPath; }
                        else if (i == 2) { DeleteImageFile(sanPhamDb.HinhAnh3); sanPhamDb.HinhAnh3 = savedPath; }
                        else if (i == 3) { DeleteImageFile(sanPhamDb.HinhAnh4); sanPhamDb.HinhAnh4 = savedPath; }
                    }
                }
            }

            // 2. Cập nhật từng ảnh theo ô
            if (imageFile != null)
            {
                var newImg1 = await SaveImageAsync(imageFile);
                if (newImg1 != null)
                {
                    DeleteImageFile(sanPhamDb.HinhAnh);
                    sanPhamDb.HinhAnh = newImg1;
                }
            }

            if (removeImage2)
            {
                DeleteImageFile(sanPhamDb.HinhAnh2);
                sanPhamDb.HinhAnh2 = null;
            }
            else if (imageFile2 != null)
            {
                var newImg2 = await SaveImageAsync(imageFile2);
                if (newImg2 != null)
                {
                    DeleteImageFile(sanPhamDb.HinhAnh2);
                    sanPhamDb.HinhAnh2 = newImg2;
                }
            }

            if (removeImage3)
            {
                DeleteImageFile(sanPhamDb.HinhAnh3);
                sanPhamDb.HinhAnh3 = null;
            }
            else if (imageFile3 != null)
            {
                var newImg3 = await SaveImageAsync(imageFile3);
                if (newImg3 != null)
                {
                    DeleteImageFile(sanPhamDb.HinhAnh3);
                    sanPhamDb.HinhAnh3 = newImg3;
                }
            }

            if (removeImage4)
            {
                DeleteImageFile(sanPhamDb.HinhAnh4);
                sanPhamDb.HinhAnh4 = null;
            }
            else if (imageFile4 != null)
            {
                var newImg4 = await SaveImageAsync(imageFile4);
                if (newImg4 != null)
                {
                    DeleteImageFile(sanPhamDb.HinhAnh4);
                    sanPhamDb.HinhAnh4 = newImg4;
                }
            }

            if (!ModelState.IsValid)
            {
                LoadLoaiSanPhamDropDown(model.MaLoaiSanPham);

                model.HinhAnh = sanPhamDb.HinhAnh;
                model.HinhAnh2 = sanPhamDb.HinhAnh2;
                model.HinhAnh3 = sanPhamDb.HinhAnh3;
                model.HinhAnh4 = sanPhamDb.HinhAnh4;
                return View(model);
            }

            decimal oldPrice = sanPhamDb.Gia;

            sanPhamDb.TenSanPham = model.TenSanPham;
            sanPhamDb.MaLoaiSanPham = model.MaLoaiSanPham;
            sanPhamDb.Gia = model.Gia;
            sanPhamDb.MoTa = model.MoTa;
            sanPhamDb.TrangThai = model.TrangThai;
            sanPhamDb.GioiTinh = model.GioiTinh;
            sanPhamDb.IsNoiBat = model.IsNoiBat;
            sanPhamDb.IsBanChay = model.IsBanChay;
            sanPhamDb.KieuCoAo = model.KieuCoAo;
            sanPhamDb.ChatLieu = model.ChatLieu;
            sanPhamDb.FormDang = model.FormDang;

            await _context.SaveChangesAsync();

            // Ghi nhật ký kiểm soát nếu giá sản phẩm bị thay đổi
            if (oldPrice != model.Gia)
            {
                await _auditLogService.LogPriceChangeAsync(sanPhamDb.TenSanPham, sanPhamDb.MaSanPham, oldPrice, model.Gia);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: SanPhams/Details/5
        /// Xem chi tiết sản phẩm.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sanPham = await _context.SanPhams
                .Include(s => s.LoaiSanPham)
                .Include(s => s.BienTheSanPhams)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null) return NotFound();

            return View(sanPham);
        }

        /// <summary>
        /// GET: SanPhams/Delete/5
        /// Hiển thị trang xác thực xóa sản phẩm.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var sanPham = await _context.SanPhams
                .Include(s => s.LoaiSanPham)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null) return NotFound();

            return View(sanPham);
        }

        /// <summary>
        /// POST: SanPhams/Delete/5
        /// Xóa sản phẩm và hình ảnh tương ứng.
        /// <summary>
        /// POST: SanPhams/Delete/5
        /// Xóa sản phẩm và toàn bộ dữ liệu phụ thuộc (biến thể, tồn kho, giỏ hàng, đơn hàng, đánh giá...).
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(s => s.BienTheSanPhams)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sanPham == null)
            {
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bienTheIds = sanPham.BienTheSanPhams.Select(b => b.MaBienThe).ToList();

                if (bienTheIds.Any())
                {
                    // 1. Xóa chi tiết đơn hàng liên quan
                    var chiTietDonHangs = _context.ChiTietDonHangs.Where(c => bienTheIds.Contains(c.MaBienThe));
                    _context.ChiTietDonHangs.RemoveRange(chiTietDonHangs);

                    // 2. Xóa chi tiết giỏ hàng liên quan
                    var chiTietGioHangs = _context.ChiTietGioHangs.Where(c => bienTheIds.Contains(c.MaBienThe));
                    _context.ChiTietGioHangs.RemoveRange(chiTietGioHangs);

                    // 3. Xóa tồn kho liên quan
                    var khoHangs = _context.KhoHangs.Where(k => bienTheIds.Contains(k.MaBienThe));
                    _context.KhoHangs.RemoveRange(khoHangs);

                    // 4. Xóa gợi ý kích cỡ liên quan
                    var goiYKichCos = _context.GoiYKichCos.Where(g => bienTheIds.Contains(g.MaBienThe));
                    _context.GoiYKichCos.RemoveRange(goiYKichCos);

                    // 5. Xóa ảnh các biến thể
                    foreach (var bt in sanPham.BienTheSanPhams)
                    {
                        DeleteImageFile(bt.HinhAnh);
                    }

                    // 6. Xóa các biến thể
                    _context.BienTheSanPhams.RemoveRange(sanPham.BienTheSanPhams);
                }

                // 7. Xóa đánh giá liên quan
                var danhGias = _context.DanhGias.Where(d => d.MaSanPham == id);
                _context.DanhGias.RemoveRange(danhGias);

                // 8. Xóa điểm ghim bộ sưu tập liên quan
                var hotspots = _context.DiemGhimBoSuuTaps.Where(h => h.MaSanPham == id);
                _context.DiemGhimBoSuuTaps.RemoveRange(hotspots);

                // 9. Xóa ảnh sản phẩm chính
                DeleteImageFile(sanPham.HinhAnh);

                // 10. Xóa sản phẩm chính
                _context.SanPhams.Remove(sanPham);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã xóa sản phẩm \"{sanPham.TenSanPham}\" thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Lỗi khi xóa sản phẩm: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: SanPhams/DeleteAll
        /// Xóa TOÀN BỘ sản phẩm và toàn bộ dữ liệu phụ thuộc trong hệ thống, reset chỉ số IDENTITY về 0.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xóa các bảng con liên kết với BienTheSanPham và SanPham
                _context.ChiTietDonHangs.RemoveRange(_context.ChiTietDonHangs);
                _context.ChiTietGioHangs.RemoveRange(_context.ChiTietGioHangs);
                _context.KhoHangs.RemoveRange(_context.KhoHangs);
                _context.GoiYKichCos.RemoveRange(_context.GoiYKichCos);
                _context.DanhGias.RemoveRange(_context.DanhGias);
                _context.DiemGhimBoSuuTaps.RemoveRange(_context.DiemGhimBoSuuTaps);

                // 2. Xóa toàn bộ biến thể sản phẩm
                _context.BienTheSanPhams.RemoveRange(_context.BienTheSanPhams);

                // 3. Xóa toàn bộ sản phẩm chính
                _context.SanPhams.RemoveRange(_context.SanPhams);

                await _context.SaveChangesAsync();

                // 4. Reset chỉ số tự tăng (IDENTITY) về 0 nếu đang dùng SQL Server
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        DBCC CHECKIDENT ('ChiTietDonHangs', RESEED, 0);
                        DBCC CHECKIDENT ('ChiTietGioHangs', RESEED, 0);
                        DBCC CHECKIDENT ('KhoHangs', RESEED, 0);
                        DBCC CHECKIDENT ('GoiYKichCos', RESEED, 0);
                        DBCC CHECKIDENT ('DanhGias', RESEED, 0);
                        DBCC CHECKIDENT ('DiemGhimBoSuuTaps', RESEED, 0);
                        DBCC CHECKIDENT ('BienTheSanPhams', RESEED, 0);
                        DBCC CHECKIDENT ('SanPhams', RESEED, 0);
                    ");
                }
                catch
                {
                    // Tiếp tục nếu môi trường database không hỗ trợ lệnh DBCC CHECKIDENT
                }

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Đã xóa toàn bộ sản phẩm thành công! Bạn có thể bắt đầu thêm sản phẩm mới.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa toàn bộ sản phẩm: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: SanPhams/ResetProducts
        /// Action tiện ích để chuyển hướng về trang danh sách sản phẩm.
        /// </summary>
        [HttpGet("/SanPhams/ResetProducts")]
        public IActionResult ResetProducts()
        {
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Nạp danh sách loại sản phẩm vào ViewBag.
        /// </summary>
        private void LoadLoaiSanPhamDropDown(object? selectedValue = null)
        {
            ViewBag.MaLoaiSanPham = new SelectList(
                _context.LoaiSanPhams.AsNoTracking().ToList(),
                "MaLoaiSanPham",
                "TenLoaiSanPham",
                selectedValue
            );
        }

        /// <summary>
        /// Lưu ảnh sản phẩm lên server.
        /// </summary>
        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/products/{uniqueFileName}";
        }

        /// <summary>
        /// Xóa file ảnh sản phẩm khỏi máy chủ.
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