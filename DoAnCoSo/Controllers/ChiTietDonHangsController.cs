using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý chi tiết đơn hàng (các mặt hàng nằm trong hóa đơn) dành cho Admin.
    /// Yêu cầu xác thực quyền quản trị viên AdminAuthorize.
    /// </summary>
    [AdminAuthorize]
    public class ChiTietDonHangsController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor inject DbContext để tương tác với Cơ sở dữ liệu.
        /// </summary>
        public ChiTietDonHangsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: ChiTietDonHangs
        /// Hiển thị danh sách tất cả các dòng chi tiết đơn hàng trong hệ thống.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var data = _context.ChiTietDonHangs
                .Include(c => c.DonHang)
                .Include(c => c.BienTheSanPham)
                .ThenInclude(b => b!.SanPham);

            return View(await data.ToListAsync());
        }

        /// <summary>
        /// GET: ChiTietDonHangs/Create
        /// Hiển thị giao diện thêm mặt hàng mới vào đơn hàng.
        /// </summary>
        public IActionResult Create()
        {
            LoadDropdown(); // Nạp dữ liệu đơn hàng và biến thể sản phẩm vào DropdownList
            return View();
        }

        /// <summary>
        /// POST: ChiTietDonHangs/Create
        /// Xử lý thêm mặt hàng mới vào đơn hàng, tự động trừ số lượng tồn kho tương ứng và tính lại tổng tiền đơn hàng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChiTietDonHang model)
        {
            // Tìm biến thể sản phẩm
            var bienThe = await _context.BienTheSanPhams
                .FirstOrDefaultAsync(b => b.MaBienThe == model.MaBienThe);

            // Tìm thông tin kho của biến thể đó
            var kho = await _context.KhoHangs
                .FirstOrDefaultAsync(k => k.MaBienThe == model.MaBienThe);

            if (bienThe == null)
                ModelState.AddModelError("", "Biến thể không tồn tại.");

            if (kho == null)
                ModelState.AddModelError("", "Chưa có dữ liệu kho.");

            if (model.SoLuong <= 0)
                ModelState.AddModelError("SoLuong", "Số lượng phải > 0.");

            // Kiểm tra tồn kho
            if (kho != null && model.SoLuong > kho.SoLuongTon)
                ModelState.AddModelError("", "Không đủ hàng trong kho.");

            if (ModelState.IsValid && bienThe != null && kho != null)
            {
                // Lấy đơn giá tại thời điểm thêm
                model.DonGia = bienThe.Gia;

                // Trừ số lượng tồn kho
                kho.SoLuongTon -= model.SoLuong;
                kho.NgayCapNhat = DateTime.Now;

                _context.ChiTietDonHangs.Add(model);
                await _context.SaveChangesAsync();

                // Tính toán lại tổng tiền của đơn hàng tương ứng
                await CapNhatTongTien(model.MaDonHang);

                return RedirectToAction(nameof(Index));
            }

            LoadDropdown(model);
            return View(model);
        }

        /// <summary>
        /// GET: ChiTietDonHangs/Edit/5
        /// Hiển thị giao diện chỉnh sửa một mặt hàng chi tiết trong đơn hàng.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var chiTiet = await _context.ChiTietDonHangs.FindAsync(id);
            if (chiTiet == null) return NotFound();

            LoadDropdown(chiTiet);
            return View(chiTiet);
        }

        /// <summary>
        /// POST: ChiTietDonHangs/Edit/5
        /// Xử lý cập nhật chi tiết đơn hàng (hoàn trả kho cũ, trừ kho mới, tính toán lại tổng tiền các hóa đơn liên quan).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChiTietDonHang model)
        {
            if (id != model.MaChiTietDonHang)
                return NotFound();

            // Tìm chi tiết đơn hàng cũ trong CSDL (không tracking để tránh xung đột thực thể)
            var cu = await _context.ChiTietDonHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaChiTietDonHang == id);

            if (cu == null) return NotFound();

            var bienThe = await _context.BienTheSanPhams
                .FirstOrDefaultAsync(b => b.MaBienThe == model.MaBienThe);

            if (bienThe == null)
                ModelState.AddModelError("", "Biến thể không tồn tại.");

            if (model.SoLuong <= 0)
                ModelState.AddModelError("SoLuong", "Số lượng phải > 0.");

            if (ModelState.IsValid && bienThe != null)
            {
                // 1. Hoàn lại số lượng tồn kho cho biến thể cũ trước khi chỉnh sửa
                var khoCu = await _context.KhoHangs
                    .FirstOrDefaultAsync(k => k.MaBienThe == cu.MaBienThe);

                if (khoCu != null)
                    khoCu.SoLuongTon += cu.SoLuong;

                // 2. Kiểm tra và trừ tồn kho cho biến thể mới
                var khoMoi = await _context.KhoHangs
                    .FirstOrDefaultAsync(k => k.MaBienThe == model.MaBienThe);

                if (khoMoi == null)
                {
                    ModelState.AddModelError("", "Kho không tồn tại.");
                }
                else if (khoMoi.SoLuongTon < model.SoLuong)
                {
                    // Trả lại lượng tồn kho nếu lỗi
                    if (khoCu != null) khoCu.SoLuongTon -= cu.SoLuong;
                    ModelState.AddModelError("", "Không đủ hàng.");
                }
                else
                {
                    // Trừ kho mới
                    khoMoi.SoLuongTon -= model.SoLuong;

                    var db = await _context.ChiTietDonHangs.FindAsync(id);
                    if (db == null) return NotFound();

                    db.MaDonHang = model.MaDonHang;
                    db.MaBienThe = model.MaBienThe;
                    db.SoLuong = model.SoLuong;
                    db.DonGia = bienThe.Gia;

                    await _context.SaveChangesAsync();

                    // Cập nhật tổng tiền đơn hàng cũ
                    await CapNhatTongTien(cu.MaDonHang);
                    // Nếu đổi hẳn sang đơn hàng khác, cập nhật tiếp đơn hàng mới
                    if (cu.MaDonHang != model.MaDonHang)
                        await CapNhatTongTien(model.MaDonHang);

                    return RedirectToAction(nameof(Index));
                }
            }

            LoadDropdown(model);
            return View(model);
        }

        /// <summary>
        /// GET: ChiTietDonHangs/Delete/5
        /// Hiển thị trang xác nhận xóa mặt hàng khỏi đơn hàng.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ct = await _context.ChiTietDonHangs
                .Include(c => c.BienTheSanPham)
                .ThenInclude(b => b!.SanPham)
                .FirstOrDefaultAsync(x => x.MaChiTietDonHang == id);

            if (ct == null) return NotFound();

            return View(ct);
        }

        /// <summary>
        /// POST: ChiTietDonHangs/Delete/5
        /// Xử lý xóa mặt hàng, hoàn kho và cập nhật lại tổng tiền đơn hàng.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ct = await _context.ChiTietDonHangs.FindAsync(id);
            if (ct != null)
            {
                // Hoàn lại số lượng đã đặt vào tồn kho
                var kho = await _context.KhoHangs
                    .FirstOrDefaultAsync(k => k.MaBienThe == ct.MaBienThe);

                if (kho != null)
                    kho.SoLuongTon += ct.SoLuong;

                int maDon = ct.MaDonHang;

                _context.ChiTietDonHangs.Remove(ct);
                await _context.SaveChangesAsync();

                // Tính lại tổng tiền của đơn hàng
                await CapNhatTongTien(maDon);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Tính toán lại tổng tiền đơn hàng dựa trên tất cả chi tiết sản phẩm.
        /// </summary>
        private async Task CapNhatTongTien(int maDonHang)
        {
            var don = await _context.DonHangs.FindAsync(maDonHang);
            if (don == null) return;

            don.TongTien = await _context.ChiTietDonHangs
                .Where(x => x.MaDonHang == maDonHang)
                .SumAsync(x => (decimal?)(x.SoLuong * x.DonGia)) ?? 0;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Nạp các DropdownList cho ViewBag dùng trong Form Create/Edit.
        /// </summary>
        private void LoadDropdown(ChiTietDonHang? model = null)
        {
            ViewBag.MaDonHang = new SelectList(_context.DonHangs, "MaDonHang", "MaDonHang", model?.MaDonHang);

            ViewBag.MaBienThe = new SelectList(
                _context.BienTheSanPhams
                    .Include(b => b.SanPham)
                    .ToList()
                    .Select(b => new
                    {
                        b.MaBienThe,
                        Ten = b.SanPham!.TenSanPham + " - " + b.KichCo + " - " + b.MauSac
                    }),
                "MaBienThe",
                "Ten",
                model?.MaBienThe
            );
        }
    }
}