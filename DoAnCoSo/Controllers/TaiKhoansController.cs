using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản trị tài khoản người dùng đăng nhập hệ thống (CRUD và khóa/mở tài khoản) dành cho Admin.
    /// Yêu cầu phân quyền AdminAuthorize.
    /// </summary>
    [AdminAuthorize]
    public class TaiKhoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor inject DbContext.
        /// </summary>
        public TaiKhoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: TaiKhoans
        /// Hiển thị danh sách tất cả các tài khoản trong hệ thống.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var danhSach = await _context.TaiKhoans.ToListAsync();
            return View(danhSach);
        }

        /// <summary>
        /// GET: TaiKhoans/Create
        /// Hiển thị form tạo tài khoản mới.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// POST: TaiKhoans/Create
        /// Xử lý tạo mới tài khoản đăng nhập.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiKhoan taiKhoan)
        {
            // Tự động điền Họ tên nếu người dùng bỏ trống để đảm bảo model hợp lệ
            if (string.IsNullOrWhiteSpace(taiKhoan.HoTen))
            {
                taiKhoan.HoTen = taiKhoan.TenDangNhap;
            }

            if (ModelState.IsValid)
            {
                _context.Add(taiKhoan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taiKhoan);
        }

        /// <summary>
        /// GET: TaiKhoans/Edit/5
        /// Hiển thị form chỉnh sửa tài khoản theo ID.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }
            return View(taiKhoan);
        }

        /// <summary>
        /// POST: TaiKhoans/Edit/5
        /// Xử lý lưu cập nhật thông tin tài khoản.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaiKhoan taiKhoan)
        {
            if (id != taiKhoan.MaTaiKhoan)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(taiKhoan.HoTen))
            {
                taiKhoan.HoTen = taiKhoan.TenDangNhap;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taiKhoan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaiKhoanExists(taiKhoan.MaTaiKhoan))
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
            return View(taiKhoan);
        }

        /// <summary>
        /// GET: TaiKhoans/Delete/5
        /// Hiển thị trang xác thực xóa tài khoản.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .FirstOrDefaultAsync(m => m.MaTaiKhoan == id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        /// <summary>
        /// POST: TaiKhoans/Delete/5
        /// Xóa tài khoản khỏi cơ sở dữ liệu.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan != null)
            {
                _context.TaiKhoans.Remove(taiKhoan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: TaiKhoans/DoiTrangThai/5
        /// Chuyển đổi nhanh trạng thái hoạt động của tài khoản (Hoạt động sang Bị khóa và ngược lại).
        /// </summary>
        public async Task<IActionResult> DoiTrangThai(int id)
        {
            var tk = await _context.TaiKhoans.FindAsync(id);
            if (tk != null)
            {
                tk.TrangThai = (tk.TrangThai == "Hoạt động") ? "Bị khóa" : "Hoạt động";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kiểm tra tài khoản tồn tại theo ID.
        /// </summary>
        private bool TaiKhoanExists(int id)
        {
            return _context.TaiKhoans.Any(e => e.MaTaiKhoan == id);
        }
    }
}