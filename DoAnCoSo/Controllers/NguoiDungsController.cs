using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý thông tin cá nhân của người dùng/khách hàng dành cho Admin.
    /// Yêu cầu xác thực Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class NguoiDungsController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor inject DbContext.
        /// </summary>
        public NguoiDungsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: NguoiDungs
        /// Hiển thị danh sách thông tin cá nhân của người dùng kèm tài khoản liên kết.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var data = _context.NguoiDungs.Include(n => n.TaiKhoan);
            return View(await data.ToListAsync());
        }

        /// <summary>
        /// GET: NguoiDungs/Create
        /// Hiển thị form tạo mới thông tin cá nhân người dùng.
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.MaTaiKhoan = new SelectList(_context.TaiKhoans, "MaTaiKhoan", "TenDangNhap");
            return View();
        }

        /// <summary>
        /// POST: NguoiDungs/Create
        /// Xử lý dữ liệu tạo mới thông tin người dùng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguoiDung model)
        {
            if (ModelState.IsValid)
            {
                _context.NguoiDungs.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MaTaiKhoan = new SelectList(_context.TaiKhoans, "MaTaiKhoan", "TenDangNhap", model.MaTaiKhoan);
            return View(model);
        }

        /// <summary>
        /// GET: NguoiDungs/Edit/5
        /// Hiển thị form cập nhật thông tin người dùng theo ID.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung == null) return NotFound();

            ViewBag.MaTaiKhoan = new SelectList(_context.TaiKhoans, "MaTaiKhoan", "TenDangNhap", nguoiDung.MaTaiKhoan);
            return View(nguoiDung);
        }

        /// <summary>
        /// POST: NguoiDungs/Edit/5
        /// Xử lý cập nhật thông tin người dùng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NguoiDung model)
        {
            if (id != model.MaNguoiDung) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MaTaiKhoan = new SelectList(_context.TaiKhoans, "MaTaiKhoan", "TenDangNhap", model.MaTaiKhoan);
            return View(model);
        }

        /// <summary>
        /// GET: NguoiDungs/Delete/5
        /// Hiển thị trang xác nhận xóa thông tin người dùng.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.TaiKhoan)
                .FirstOrDefaultAsync(n => n.MaNguoiDung == id);

            if (nguoiDung == null) return NotFound();

            return View(nguoiDung);
        }

        /// <summary>
        /// POST: NguoiDungs/Delete/5
        /// Thực hiện xóa thông tin người dùng.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            if (nguoiDung != null)
            {
                _context.NguoiDungs.Remove(nguoiDung);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: NguoiDungs/ExportExcel
        /// Xuất danh sách khách hàng ra file Excel (.xlsx) kèm cấp bậc VIP và tổng chi tiêu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("FASHION STORE Admin");

            var users = await _context.NguoiDungs
                .Include(n => n.TaiKhoan)
                .OrderByDescending(n => n.TongChiTieu)
                .ToListAsync();

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("KhachHang");

                string[] headers = new string[] {
                    "Mã KH", "Họ và Tên", "Tên đăng nhập", "Email", "Số điện thoại", 
                    "Địa chỉ", "Tổng chi tiêu (₫)", "Cấp bậc VIP", "Chiết khấu VIP (%)"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(30, 41, 59));
                    worksheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var u in users)
                {
                    var vip = DoAnCoSo.Services.VipService.GetVipStatus(u.TongChiTieu);
                    worksheet.Cells[row, 1].Value = u.MaNguoiDung;
                    worksheet.Cells[row, 2].Value = u.HoTen ?? "";
                    worksheet.Cells[row, 3].Value = u.TaiKhoan?.TenDangNhap ?? "";
                    worksheet.Cells[row, 4].Value = u.TaiKhoan?.Email ?? "";
                    worksheet.Cells[row, 5].Value = u.TaiKhoan?.SoDienThoai ?? "";
                    worksheet.Cells[row, 6].Value = u.DiaChi ?? "";
                    worksheet.Cells[row, 7].Value = u.TongChiTieu;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 8].Value = vip.CurrentTier?.TenHang ?? "VIP Khởi Đầu";
                    worksheet.Cells[row, 9].Value = (vip.CurrentTier?.PhanTramGiam ?? 5) + "%";
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var fileContents = package.GetAsByteArray();
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DanhSach_KhachHang_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }
    }
}