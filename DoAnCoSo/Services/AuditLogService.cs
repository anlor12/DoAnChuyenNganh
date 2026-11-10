using System;
using System.Linq;
using System.Threading.Tasks;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Services
{
    /// <summary>
    /// Dịch vụ ghi vết Nhật ký kiểm soát hệ thống (Audit Logs)
    /// Ghi nhận các hành vi nhạy cảm của Admin / Nhân viên: sửa giá, chỉnh kho, hủy đơn, xuất báo cáo...
    /// </summary>
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Ghi vết một hành động kiểm soát hệ thống
        /// </summary>
        public async Task LogAsync(
            string hanhDong, 
            string moTa, 
            string? doiTuong = null, 
            string? giaTriCu = null, 
            string? giaTriMoi = null, 
            string mucDo = "Trung bình")
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                string username = httpContext?.Session.GetString("AdminUser") ?? "Hệ thống";
                string role = httpContext?.Session.GetString("VaiTro") ?? "Quản trị viên";
                string fullName = username;

                if (username != "Hệ thống")
                {
                    var user = await _context.TaiKhoans
                        .Include(t => t.NguoiDung)
                        .FirstOrDefaultAsync(t => t.TenDangNhap == username);

                    if (user != null)
                    {
                        fullName = user.NguoiDung?.HoTen ?? user.HoTen;
                        role = user.VaiTro ?? role;
                    }
                }

                // Lấy IP của máy khách
                string ip = "127.0.0.1";
                if (httpContext != null)
                {
                    if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                    {
                        ip = httpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
                    }
                    else if (httpContext.Connection.RemoteIpAddress != null)
                    {
                        ip = httpContext.Connection.RemoteIpAddress.ToString();
                        if (ip == "::1") ip = "127.0.0.1 (Localhost)";
                    }
                }

                // Lấy User Agent (Trình duyệt)
                string userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown Client";
                if (userAgent.Length > 250) userAgent = userAgent.Substring(0, 250);

                var log = new NhatKyHeThong
                {
                    TenDangNhap = username,
                    HoTen = fullName,
                    VaiTro = role,
                    HanhDong = hanhDong,
                    MoTa = moTa,
                    DoiTuongAnhHuong = doiTuong,
                    GiaTriCu = giaTriCu,
                    GiaTriMoi = giaTriMoi,
                    DiaChiIp = ip,
                    UserAgent = userAgent,
                    MucDo = mucDo,
                    ThoiGian = DateTime.Now
                };

                _context.NhatKyHeThongs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("AuditLogService error: " + ex.Message);
            }
        }

        /// <summary>
        /// Ghi vết thay đổi giá bán sản phẩm
        /// </summary>
        public async Task LogPriceChangeAsync(string tenSanPham, int maSanPham, decimal giaCu, decimal giaMoi)
        {
            await LogAsync(
                hanhDong: "SUA_GIA_SAN_PHAM",
                moTa: $"Điều chỉnh giá bán sản phẩm '{tenSanPham}' (Mã #{maSanPham})",
                doiTuong: $"SanPham #{maSanPham}",
                giaTriCu: $"{giaCu:N0} đ",
                giaTriMoi: $"{giaMoi:N0} đ",
                mucDo: "Nguy hiểm"
            );
        }

        /// <summary>
        /// Ghi vết chỉnh sửa kho hàng thủ công
        /// </summary>
        public async Task LogInventoryChangeAsync(string tenSanPham, int maBienThe, int tonCu, int tonMoi, string? lyDo = null)
        {
            await LogAsync(
                hanhDong: "CHINH_KHO_THU_CONG",
                moTa: $"Điều chỉnh số lượng tồn kho của '{tenSanPham}' (Biến thể #{maBienThe}). " + (!string.IsNullOrEmpty(lyDo) ? $"Lý do: {lyDo}" : ""),
                doiTuong: $"KhoHang_BienThe #{maBienThe}",
                giaTriCu: $"{tonCu} sản phẩm",
                giaTriMoi: $"{tonMoi} sản phẩm (Biến động {(tonMoi - tonCu):+0;-0;0})",
                mucDo: "Cao"
            );
        }

        /// <summary>
        /// Ghi vết hủy đơn hàng
        /// </summary>
        public async Task LogOrderCancelledAsync(int maDonHang, decimal tongTien, string lyDo)
        {
            await LogAsync(
                hanhDong: "HUY_DON_HANG",
                moTa: $"Hủy đơn hàng #{maDonHang} trị giá {tongTien:N0} đ. Lý do: {lyDo}",
                doiTuong: $"DonHang #{maDonHang}",
                giaTriCu: "Trạng thái đang hoạt động",
                giaTriMoi: "Đã hủy",
                mucDo: "Cao"
            );
        }

        /// <summary>
        /// Ghi vết thay đổi trạng thái đơn hàng
        /// </summary>
        public async Task LogOrderStatusChangeAsync(int maDonHang, string trangThaiCu, string trangThaiMoi)
        {
            await LogAsync(
                hanhDong: "DOI_TRANG_THAI_DON",
                moTa: $"Chuyển trạng thái đơn hàng #{maDonHang} từ '{trangThaiCu}' sang '{trangThaiMoi}'",
                doiTuong: $"DonHang #{maDonHang}",
                giaTriCu: trangThaiCu,
                giaTriMoi: trangThaiMoi,
                mucDo: "Trung bình"
            );
        }

        /// <summary>
        /// Ghi vết xuất báo cáo thống kê
        /// </summary>
        public async Task LogReportExportAsync(string loaiBaoCao)
        {
            await LogAsync(
                hanhDong: "XUAT_BAO_CAO",
                moTa: $"Xuất file báo cáo thống kê dữ liệu: {loaiBaoCao}",
                doiTuong: "BaoCaoHeThong",
                mucDo: "Trung bình"
            );
        }
    }
}
