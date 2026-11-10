using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản lý danh sách và chi tiết đơn hàng dành cho Admin.
    /// Yêu cầu xác thực Admin (AdminAuthorize).
    /// </summary>
    [AdminAuthorize]
    public class DonHangsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LoyaltyService _loyaltyService;
        private readonly AuditLogService _auditLogService;

        /// <summary>
        /// Constructor inject DbContext, LoyaltyService và AuditLogService
        /// </summary>
        public DonHangsController(
            ApplicationDbContext context, 
            LoyaltyService loyaltyService,
            AuditLogService auditLogService)
        {
            _context = context;
            _loyaltyService = loyaltyService;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// GET: DonHangs
        /// Hiển thị danh sách tất cả đơn hàng trong hệ thống kèm thông tin khách hàng.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var data = await _context.DonHangs
                .Include(x => x.NguoiDung)
                .ToListAsync();

            return View(data);
        }

        /// <summary>
        /// GET/POST: DonHangs/ExportExcel
        /// Xuất danh sách đơn hàng ra file Excel đầy đủ chi tiết
        /// </summary>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ExportExcel()
        {
            ExcelPackage.License.SetNonCommercialPersonal("FASHION STORE Admin");

            var donHangs = await _context.DonHangs
                .Include(x => x.NguoiDung)
                .OrderByDescending(x => x.NgayDat)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("DanhSachDonHang");

            // Header
            worksheet.Cells[1, 1].Value = "Mã Đơn Hàng";
            worksheet.Cells[1, 2].Value = "Khách Hàng";
            worksheet.Cells[1, 3].Value = "Số Điện Thoại";
            worksheet.Cells[1, 4].Value = "Địa Chỉ Nhận";
            worksheet.Cells[1, 5].Value = "Ngày Đặt Hàng";
            worksheet.Cells[1, 6].Value = "Đơn Vị Vận Chuyển";
            worksheet.Cells[1, 7].Value = "Mã Vận Đơn";
            worksheet.Cells[1, 8].Value = "Phí Ship (VNĐ)";
            worksheet.Cells[1, 9].Value = "Chiết Khấu VIP (%)";
            worksheet.Cells[1, 10].Value = "Quà Tặng Kèm";
            worksheet.Cells[1, 11].Value = "Tổng Tiền (VNĐ)";
            worksheet.Cells[1, 12].Value = "Hình Thức Thanh Toán";
            worksheet.Cells[1, 13].Value = "Trạng Thái Thanh Toán";
            worksheet.Cells[1, 14].Value = "Trạng Thái Đơn Hàng";

            using (var range = worksheet.Cells[1, 1, 1, 14])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(17, 24, 39));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Data
            int row = 2;
            foreach (var dh in donHangs)
            {
                worksheet.Cells[row, 1].Value = "#ĐH" + dh.MaDonHang;
                worksheet.Cells[row, 2].Value = dh.NguoiDung?.HoTen ?? "Khách vãng lai";
                worksheet.Cells[row, 3].Value = dh.SoDienThoaiNhan;
                worksheet.Cells[row, 4].Value = dh.DiaChiGiaoHang;
                worksheet.Cells[row, 5].Value = dh.NgayDat.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[row, 6].Value = dh.DonViVanChuyen ?? "GHTK";
                worksheet.Cells[row, 7].Value = dh.MaVanDon ?? "Chưa tạo";
                worksheet.Cells[row, 8].Value = dh.PhiGiaoHang;
                worksheet.Cells[row, 9].Value = dh.ChietKhauVip + "%";
                worksheet.Cells[row, 10].Value = dh.QuaTangKem ?? "Không có";
                worksheet.Cells[row, 11].Value = dh.TongTien;
                worksheet.Cells[row, 12].Value = dh.PhuongThucThanhToan;
                worksheet.Cells[row, 13].Value = dh.TrangThaiThanhToan;
                worksheet.Cells[row, 14].Value = dh.TrangThaiDonHang;
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            var content = stream.ToArray();

            // Ghi nhật ký xuất báo cáo
            await _auditLogService.LogReportExportAsync($"Báo cáo Đơn hàng & Doanh thu Excel ({donHangs.Count} đơn)");

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_DonHangs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        /// <summary>
        /// GET: DonHangs/Details/5
        /// Xem thông tin chi tiết của một đơn hàng cụ thể bao gồm danh sách các mặt hàng đã mua.
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        public async Task<IActionResult> Details(int id)
        {
            var data = await _context.DonHangs
                .Include(x => x.NguoiDung)
                .Include(x => x.ChiTietDonHangs)
                    .ThenInclude(ct => ct.BienTheSanPham)
                    .ThenInclude(bt => bt!.SanPham)
                .FirstOrDefaultAsync(x => x.MaDonHang == id);

            if (data == null)
            {
                return NotFound();
            }

            return View(data);
        }

        /// <summary>
        /// POST: DonHangs/UpdateStatus
        /// Cập nhật trạng thái đơn hàng (Ví dụ: Đang giao, Đã giao) và trạng thái thanh toán.
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <param name="status">Trạng thái đơn hàng mới</param>
        /// <param name="paymentStatus">Trạng thái thanh toán mới (nếu có)</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string paymentStatus)
        {
            var donHang = await _context.DonHangs.FindAsync(id);

            if (donHang == null)
            {
                return NotFound();
            }

            // Nếu chuyển sang trạng thái "Đã hủy" và trước đó chưa hủy, tự động hoàn trả lại tồn kho
            if (status == "Đã hủy" && donHang.TrangThaiDonHang != "Đã hủy" && donHang.TrangThaiDonHang != "Trả hàng / Hoàn tiền")
            {
                var orderWithDetails = await _context.DonHangs
                    .Include(o => o.ChiTietDonHangs)
                    .FirstOrDefaultAsync(o => o.MaDonHang == id);

                if (orderWithDetails?.ChiTietDonHangs != null)
                {
                    foreach (var item in orderWithDetails.ChiTietDonHangs)
                    {
                        var kho = await _context.KhoHangs.FirstOrDefaultAsync(k => k.MaBienThe == item.MaBienThe);
                        if (kho != null)
                        {
                            kho.SoLuongTon += item.SoLuong;
                            kho.NgayCapNhat = DateTime.Now;
                            _context.KhoHangs.Update(kho);
                        }
                    }
                }
            }

            string oldStatus = donHang.TrangThaiDonHang;

            // Cập nhật trạng thái đơn hàng
            donHang.TrangThaiDonHang = status;
            
            // Cập nhật trạng thái thanh toán nếu được truyền lên
            if (!string.IsNullOrEmpty(paymentStatus))
            {
                donHang.TrangThaiThanhToan = paymentStatus;
            }

            await _context.SaveChangesAsync();

            // Ghi nhật ký kiểm soát thao tác trạng thái
            if (oldStatus != status)
            {
                if (status == "Đã hủy")
                {
                    await _auditLogService.LogOrderCancelledAsync(id, donHang.TongTien, "Quản trị viên chuyển trạng thái Đã hủy");
                }
                else
                {
                    await _auditLogService.LogOrderStatusChangeAsync(id, oldStatus, status);
                }
            }

            // Tự động tích lũy điểm thưởng và xét thưởng người giới thiệu khi đơn Hoàn thành
            if (status == "Hoàn thành")
            {
                await _loyaltyService.CongDiemDonHangHoanThanhAsync(id);
            }
            else if (status == "Đã hủy")
            {
                await _loyaltyService.HoanDiemDonHangHuyAsync(id);
            }

            // Quay trở lại trang chi tiết đơn hàng
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// POST: DonHangs/RefundOrder
        /// Thực hiện đổi trả hàng, cộng lại số lượng tồn kho và hoàn tiền giả lập về tài khoản khách hàng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundOrder(int id)
        {
            var donHang = await _context.DonHangs
                .Include(o => o.ChiTietDonHangs)
                .FirstOrDefaultAsync(o => o.MaDonHang == id);

            if (donHang == null)
            {
                return NotFound();
            }

            // Chỉ hoàn tiền nếu đơn hàng đã thanh toán hoặc đã hoàn thành
            if (donHang.TrangThaiDonHang == "Hoàn thành" || donHang.TrangThaiThanhToan == "Đã thanh toán" || donHang.TrangThaiThanhToan == "DaThanhToan")
            {
                string oldOrderStatus = donHang.TrangThaiDonHang;

                // 1. Cập nhật trạng thái đơn hàng và trạng thái thanh toán
                donHang.TrangThaiDonHang = "Trả hàng / Hoàn tiền";
                donHang.TrangThaiThanhToan = "Đã hoàn tiền";

                // 2. Hoàn lại số lượng tồn kho cho các sản phẩm trong đơn
                if (donHang.ChiTietDonHangs != null)
                {
                    foreach (var item in donHang.ChiTietDonHangs)
                    {
                        var kho = await _context.KhoHangs.FirstOrDefaultAsync(k => k.MaBienThe == item.MaBienThe);
                        if (kho != null)
                        {
                            kho.SoLuongTon += item.SoLuong;
                            _context.KhoHangs.Update(kho);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Hoàn lại điểm tích lũy đã dùng (nếu có)
                await _loyaltyService.HoanDiemDonHangHuyAsync(id);

                // Ghi nhật ký kiểm soát hoàn tiền
                await _auditLogService.LogAsync(
                    hanhDong: "HOAN_TIEN_DON_HANG",
                    moTa: $"Duyệt hoàn trả đơn hàng #{id} trị giá {donHang.TongTien:N0} đ và hoàn trả sản phẩm về kho",
                    doiTuong: $"DonHang #{id}",
                    giaTriCu: oldOrderStatus,
                    giaTriMoi: "Trả hàng / Hoàn tiền",
                    mucDo: "Cao"
                );

                TempData["RefundSuccess"] = $"Yêu cầu trả hàng được duyệt! Hệ thống đã tự động hoàn trả {donHang.TongTien.ToString("N0")} đ về tài khoản của khách hàng và cập nhật lại kho hàng thành công.";
            }
            else
            {
                TempData["RefundError"] = "Đơn hàng chưa được thanh toán hoặc không hợp lệ để thực hiện hoàn tiền.";
            }

            return RedirectToAction(nameof(Details), new { id = donHang.MaDonHang });
        }

        /// <summary>
        /// GET: DonHangs/InPhieuGiaoHang/5
        /// Xuất phiếu dán bưu kiện (Waybill A5/A6) có mã vạch Barcode 128 / QR Code và danh sách nhặt hàng (Pick List)
        /// </summary>
        public async Task<IActionResult> InPhieuGiaoHang(int? id)
        {
            if (id == null) return NotFound();

            var donHang = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.BienTheSanPham!)
                        .ThenInclude(bt => bt.SanPham)
                .FirstOrDefaultAsync(m => m.MaDonHang == id);

            if (donHang == null) return NotFound();

            if (string.IsNullOrEmpty(donHang.MaVanDon))
            {
                donHang.MaVanDon = DoAnCoSo.Services.ShippingService.GenerateTrackingCode(donHang.DonViVanChuyen ?? "GHTK", donHang.MaDonHang);
                _context.Update(donHang);
                await _context.SaveChangesAsync();
            }

            return View("Waybill", donHang);
        }

        /// <summary>
        /// GET: DonHangs/InHoaDon/5
        /// Xuất hóa đơn bán lẻ (Retail Invoice) chuẩn thương mại điện tử
        /// </summary>
        public async Task<IActionResult> InHoaDon(int? id)
        {
            if (id == null) return NotFound();

            var donHang = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.BienTheSanPham!)
                        .ThenInclude(bt => bt.SanPham)
                .FirstOrDefaultAsync(m => m.MaDonHang == id);

            if (donHang == null) return NotFound();

            return View("Invoice", donHang);
        }

        /// <summary>
        /// POST: DonHangs/DayDonVanChuyen
        /// Đẩy đơn hàng sang nhà xe vận chuyển (API Dispatch) & cập nhật mã vận đơn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DayDonVanChuyen(int id, string? carrierCode)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound();

            if (!string.IsNullOrEmpty(carrierCode))
            {
                donHang.DonViVanChuyen = carrierCode;
            }
            if (string.IsNullOrEmpty(donHang.DonViVanChuyen))
            {
                donHang.DonViVanChuyen = "GHTK";
            }

            if (string.IsNullOrEmpty(donHang.MaVanDon))
            {
                donHang.MaVanDon = DoAnCoSo.Services.ShippingService.GenerateTrackingCode(donHang.DonViVanChuyen, donHang.MaDonHang);
            }

            donHang.TrangThaiDonHang = "Đang giao";
            donHang.NhanVienGiaoHang = "Nguyễn Văn Tuấn (0981.234.567)";
            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã đẩy đơn #{donHang.MaDonHang} sang đối tác {donHang.DonViVanChuyen} với mã vận đơn: {donHang.MaVanDon}!";
            return RedirectToAction(nameof(Details), new { id = donHang.MaDonHang });
        }
    }
}