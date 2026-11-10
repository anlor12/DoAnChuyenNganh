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
    /// Controller quản lý việc nhập kho, xuất kho và theo dõi số lượng hàng tồn của từng biến thể sản phẩm.
    /// Yêu cầu xác thực Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class KhoHangsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        /// <summary>
        /// Constructor inject DbContext và AuditLogService.
        /// </summary>
        public KhoHangsController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// GET: KhoHangs
        /// Hiển thị danh sách tồn kho của tất cả sản phẩm kèm kích cỡ và màu sắc.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var data = _context.KhoHangs
                .Include(k => k.BienTheSanPham)
                .ThenInclude(b => b!.SanPham);

            return View(await data.ToListAsync());
        }

        /// <summary>
        /// GET: KhoHangs/Create
        /// Hiển thị giao diện thiết lập kho hàng mới cho một biến thể sản phẩm.
        /// </summary>
        public IActionResult Create()
        {
            // Tải danh sách biến thể kèm tên sản phẩm chính để lựa chọn
            ViewBag.MaBienThe = new SelectList(
                _context.BienTheSanPhams.Include(b => b.SanPham).ToList()
                .Select(b => new
                {
                    b.MaBienThe,
                    TenHienThi = b.SanPham!.TenSanPham + " - Size: " + b.KichCo + " - Màu: " + b.MauSac
                }),
                "MaBienThe",
                "TenHienThi"
            );

            return View();
        }

        /// <summary>
        /// POST: KhoHangs/Create
        /// Xử lý tạo mới cấu hình tồn kho.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhoHang model)
        {
            if (ModelState.IsValid)
            {
                model.NgayCapNhat = DateTime.Now;
                _context.KhoHangs.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MaBienThe = new SelectList(
                _context.BienTheSanPhams.Include(b => b.SanPham).ToList()
                .Select(b => new
                {
                    b.MaBienThe,
                    TenHienThi = b.SanPham!.TenSanPham + " - Size: " + b.KichCo + " - Màu: " + b.MauSac
                }),
                "MaBienThe",
                "TenHienThi",
                model.MaBienThe
            );

            return View(model);
        }

        /// <summary>
        /// GET: KhoHangs/Edit/5
        /// Hiển thị trang điều chỉnh số lượng tồn kho theo ID.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var khoHang = await _context.KhoHangs.FindAsync(id);
            if (khoHang == null) return NotFound();

            ViewBag.MaBienThe = new SelectList(
                _context.BienTheSanPhams.Include(b => b.SanPham).ToList()
                .Select(b => new
                {
                    b.MaBienThe,
                    TenHienThi = b.SanPham!.TenSanPham + " - Size: " + b.KichCo + " - Màu: " + b.MauSac
                }),
                "MaBienThe",
                "TenHienThi",
                khoHang.MaBienThe
            );

            return View(khoHang);
        }

        /// <summary>
        /// POST: KhoHangs/Edit/5
        /// Cập nhật số lượng tồn kho mới và ghi nhận thời gian chỉnh sửa.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhoHang model)
        {
            if (id != model.MaKhoHang) return NotFound();

            if (ModelState.IsValid)
            {
                var khoDb = await _context.KhoHangs
                    .Include(k => k.BienTheSanPham)
                        .ThenInclude(b => b!.SanPham)
                    .FirstOrDefaultAsync(k => k.MaKhoHang == id);

                if (khoDb == null) return NotFound();

                int oldQuantity = khoDb.SoLuongTon;

                khoDb.MaBienThe = model.MaBienThe;
                khoDb.SoLuongTon = model.SoLuongTon;
                khoDb.GhiChu = model.GhiChu;
                khoDb.NgayCapNhat = DateTime.Now; // Ghi nhận thời gian cập nhật kho mới

                _context.Update(khoDb);
                await _context.SaveChangesAsync();

                // Ghi vết nhật ký kiểm soát nếu số lượng tồn kho thay đổi
                if (oldQuantity != model.SoLuongTon)
                {
                    string prodName = khoDb.BienTheSanPham?.SanPham?.TenSanPham ?? "Sản phẩm";
                    await _auditLogService.LogInventoryChangeAsync(prodName, khoDb.MaBienThe, oldQuantity, model.SoLuongTon, model.GhiChu);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.MaBienThe = new SelectList(
                _context.BienTheSanPhams.Include(b => b.SanPham).ToList()
                .Select(b => new
                {
                    b.MaBienThe,
                    TenHienThi = b.SanPham!.TenSanPham + " - Size: " + b.KichCo + " - Màu: " + b.MauSac
                }),
                "MaBienThe",
                "TenHienThi",
                model.MaBienThe
            );

            return View(model);
        }

        /// <summary>
        /// GET: KhoHangs/Delete/5
        /// Hiển thị trang xác nhận xóa cấu hình kho hàng.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var khoHang = await _context.KhoHangs
                .Include(k => k.BienTheSanPham)
                .ThenInclude(b => b!.SanPham)
                .FirstOrDefaultAsync(k => k.MaKhoHang == id);

            if (khoHang == null) return NotFound();

            return View(khoHang);
        }

        /// <summary>
        /// POST: KhoHangs/Delete/5
        /// Xóa bản ghi cấu hình kho.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var khoHang = await _context.KhoHangs.FindAsync(id);
            if (khoHang != null)
            {
                _context.KhoHangs.Remove(khoHang);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}