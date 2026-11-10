using System;
using System.Linq;
using System.Threading.Tasks;
using DoAnCoSo.Data;
using DoAnCoSo.Filters;
using DoAnCoSo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    /// <summary>
    /// Controller quản trị Nhật ký kiểm soát hệ thống (Audit Logs) dành cho Admin.
    /// Giám sát mọi thao tác nhạy cảm: sửa giá, chỉnh kho thủ công, hủy đơn, xuất báo cáo...
    /// </summary>
    [AdminAuthorize]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /AuditLogs
        /// Hiển thị danh sách nhật ký thao tác hệ thống với bộ lọc đa chiều
        /// </summary>
        public async Task<IActionResult> Index(
            string? search, 
            string? actionType, 
            string? severity, 
            DateTime? fromDate, 
            DateTime? toDate)
        {
            var query = _context.NhatKyHeThongs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(l => 
                    l.TenDangNhap.ToLower().Contains(search) || 
                    (l.HoTen != null && l.HoTen.ToLower().Contains(search)) ||
                    l.MoTa.ToLower().Contains(search) ||
                    (l.DoiTuongAnhHuong != null && l.DoiTuongAnhHuong.ToLower().Contains(search)) ||
                    (l.DiaChiIp != null && l.DiaChiIp.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(l => l.HanhDong == actionType);
            }

            if (!string.IsNullOrWhiteSpace(severity))
            {
                query = query.Where(l => l.MucDo == severity);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.ThoiGian >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.ThoiGian <= endOfDay);
            }

            var logs = await query
                .OrderByDescending(l => l.ThoiGian)
                .Take(250)
                .ToListAsync();

            // Thống kê nhanh
            var allQuery = _context.NhatKyHeThongs.AsQueryable();
            var today = DateTime.Today;

            ViewBag.TotalLogs = await allQuery.CountAsync();
            ViewBag.SensitiveLogs = await allQuery.CountAsync(l => l.MucDo == "Cao" || l.MucDo == "Nguy hiểm");
            ViewBag.LogsToday = await allQuery.CountAsync(l => l.ThoiGian >= today);
            ViewBag.DistinctUsers = await allQuery.Select(l => l.TenDangNhap).Distinct().CountAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentActionType = actionType;
            ViewBag.CurrentSeverity = severity;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }
    }
}
