using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý chương trình khuyến mãi, mã giảm giá và ưu đãi tặng quà dành cho Admin.
    /// </summary>
    [AdminAuthorize]
    public class KhuyenMaisController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public KhuyenMaisController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// GET: KhuyenMais
        /// Danh sách các chương trình khuyến mãi & ưu đãi quà tặng
        /// </summary>
        public async Task<IActionResult> Index(string? tuKhoa, string? loai, string? filter)
        {
            var query = _context.KhuyenMais.Include(k => k.LoaiSanPhamApDung).AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim().ToUpper();
                query = query.Where(k => k.Code.Contains(tuKhoa) || 
                                         (k.TenChuongTrinh != null && k.TenChuongTrinh.Contains(tuKhoa)) ||
                                         (k.QuaTangKem != null && k.QuaTangKem.Contains(tuKhoa)));
                ViewBag.TuKhoa = tuKhoa;
            }

            if (!string.IsNullOrWhiteSpace(loai) && loai != "All")
            {
                query = query.Where(k => k.LoaiKhuyenMai == loai);
                ViewBag.Loai = loai;
            }

            if (filter == "active")
            {
                query = query.Where(k => k.TrangThai && k.NgayKetThuc >= DateTime.Now && k.SoLuong > 0);
            }
            else if (filter == "expired")
            {
                query = query.Where(k => !k.TrangThai || k.NgayKetThuc < DateTime.Now || k.SoLuong <= 0);
            }
            ViewBag.Filter = filter;

            var list = await query.OrderByDescending(k => k.TrangThai)
                                  .ThenByDescending(k => k.NgayKetThuc)
                                  .ToListAsync();

            return View(list);
        }

        /// <summary>
        /// GET: KhuyenMais/Create
        /// </summary>
        public async Task<IActionResult> Create()
        {
            ViewBag.LoaiSanPhams = await _context.LoaiSanPhams.OrderBy(l => l.TenLoaiSanPham).ToListAsync();

            var model = new KhuyenMai
            {
                Code = "KM" + new Random().Next(1000, 9999),
                LoaiKhuyenMai = "GiamGia",
                PhanTramGiam = 10,
                GiaTriDonToiThieu = 0,
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddMonths(1),
                SoLuong = 100,
                HienThiTrangChu = true,
                TrangThai = true,
                ChiDanhChoKhachMoi = false
            };
            return View(model);
        }

        /// <summary>
        /// POST: KhuyenMais/Create
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai khuyenMai, IFormFile? anhQuaTangFile)
        {
            if (ModelState.IsValid)
            {
                khuyenMai.Code = khuyenMai.Code.Trim().ToUpper();

                if (await _context.KhuyenMais.AnyAsync(k => k.Code == khuyenMai.Code))
                {
                    ModelState.AddModelError("Code", "Mã khuyến mãi này đã tồn tại trong hệ thống!");
                    ViewBag.LoaiSanPhams = await _context.LoaiSanPhams.OrderBy(l => l.TenLoaiSanPham).ToListAsync();
                    return View(khuyenMai);
                }

                // Xử lý upload ảnh quà tặng nếu có
                if (anhQuaTangFile != null && anhQuaTangFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "gifts");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = "gift_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(anhQuaTangFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await anhQuaTangFile.CopyToAsync(fileStream);
                    }
                    khuyenMai.HinhAnhQuaTang = "/images/gifts/" + uniqueFileName;
                }

                _context.Add(khuyenMai);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo chương trình khuyến mãi / ưu đãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.LoaiSanPhams = await _context.LoaiSanPhams.OrderBy(l => l.TenLoaiSanPham).ToListAsync();
            return View(khuyenMai);
        }

        /// <summary>
        /// GET: KhuyenMais/Edit/CHAOMUNG
        /// </summary>
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null) return NotFound();

            ViewBag.LoaiSanPhams = await _context.LoaiSanPhams.OrderBy(l => l.TenLoaiSanPham).ToListAsync();
            return View(khuyenMai);
        }

        /// <summary>
        /// POST: KhuyenMais/Edit/CHAOMUNG
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, KhuyenMai khuyenMai, IFormFile? anhQuaTangFile)
        {
            if (id != khuyenMai.Code) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.KhuyenMais.FindAsync(id);
                    if (existing == null) return NotFound();

                    // Xử lý upload ảnh quà tặng nếu có
                    if (anhQuaTangFile != null && anhQuaTangFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "gifts");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = "gift_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(anhQuaTangFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await anhQuaTangFile.CopyToAsync(fileStream);
                        }
                        existing.HinhAnhQuaTang = "/images/gifts/" + uniqueFileName;
                    }
                    else if (!string.IsNullOrWhiteSpace(khuyenMai.HinhAnhQuaTang))
                    {
                        existing.HinhAnhQuaTang = khuyenMai.HinhAnhQuaTang;
                    }

                    existing.TenChuongTrinh = khuyenMai.TenChuongTrinh;
                    existing.LoaiKhuyenMai = khuyenMai.LoaiKhuyenMai;
                    existing.PhanTramGiam = khuyenMai.PhanTramGiam;
                    existing.GiaTriDonToiThieu = khuyenMai.GiaTriDonToiThieu;
                    existing.GiamToiDa = khuyenMai.GiamToiDa;
                    existing.QuaTangKem = khuyenMai.QuaTangKem;
                    existing.NgayBatDau = khuyenMai.NgayBatDau;
                    existing.NgayKetThuc = khuyenMai.NgayKetThuc;
                    existing.SoLuong = khuyenMai.SoLuong;
                    existing.MoTa = khuyenMai.MoTa;
                    existing.HienThiTrangChu = khuyenMai.HienThiTrangChu;
                    existing.ChiDanhChoKhachMoi = khuyenMai.ChiDanhChoKhachMoi;
                    existing.MaLoaiSanPhamApDung = khuyenMai.MaLoaiSanPhamApDung;
                    existing.TrangThai = khuyenMai.TrangThai;

                    _context.Update(existing);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật chương trình khuyến mãi thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhuyenMaiExists(khuyenMai.Code))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(khuyenMai);
        }

        /// <summary>
        /// POST: KhuyenMais/Delete/CHAOMUNG
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai != null)
            {
                _context.KhuyenMais.Remove(khuyenMai);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa khuyến mãi thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: KhuyenMais/ToggleTrangThai/CHAOMUNG
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleTrangThai(string id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });

            km.TrangThai = !km.TrangThai;
            await _context.SaveChangesAsync();
            return Json(new { success = true, trangThai = km.TrangThai });
        }

        private bool KhuyenMaiExists(string id)
        {
            return _context.KhuyenMais.Any(e => e.Code == id);
        }
    }
}
