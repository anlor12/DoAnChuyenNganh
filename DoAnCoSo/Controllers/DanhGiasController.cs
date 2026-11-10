using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý đánh giá sản phẩm (xem và xóa) dành cho Admin.
    /// Yêu cầu quyền truy cập Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class DanhGiasController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor inject DbContext.
        /// </summary>
        public DanhGiasController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: DanhGias
        /// Hiển thị danh sách tất cả các đánh giá trong hệ thống.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var data = await _context.DanhGias
                .Include(x => x.NguoiDung)
                .Include(x => x.SanPham)
                .OrderByDescending(x => x.NgayTao)
                .ToListAsync();

            return View(data);
        }

        /// <summary>
        /// POST: DanhGias/Delete/5
        /// Xử lý xóa đánh giá khỏi cơ sở dữ liệu.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var danhGia = await _context.DanhGias.FindAsync(id);
            if (danhGia == null)
            {
                TempData["AdminReviewError"] = "Không tìm thấy đánh giá cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.DanhGias.Remove(danhGia);
                await _context.SaveChangesAsync();
                TempData["AdminReviewSuccess"] = "Xóa đánh giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["AdminReviewError"] = "Có lỗi xảy ra khi xóa đánh giá: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
