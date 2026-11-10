using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Services
{
    public class LoyaltySummaryViewModel
    {
        public int DiemHienTai { get; set; }
        public decimal GiaTriDiemVnd => DiemHienTai; // 1 điểm = 1 VNĐ
        public string HangVip { get; set; } = "VIP Khởi Đầu";
        public string MaGioiThieu { get; set; } = string.Empty;
        public string LinkGioiThieu { get; set; } = string.Empty;
        public int SoNguoiDaGioiThieu { get; set; }
        public int TongDiemThuongGioiThieu { get; set; }
        public List<LichSuDiemThuong> LichSu { get; set; } = new();
    }

    /// <summary>
    /// Dịch vụ quản lý Hệ thống Điểm tích lũy & Giới thiệu bạn bè (Loyalty Points & Referral System)
    /// </summary>
    public class LoyaltyService
    {
        private readonly ApplicationDbContext _context;

        public const decimal TICH_DIEM_TI_LE = 0.01m; // Tích 1% giá trị đơn hàng
        public const int THUONG_GIOI_THIEU_DIEM = 50000; // Thưởng 50.000 điểm khi bạn bè hoàn thành đơn đầu tiên
        public const decimal MAX_TIEN_DIEM_TI_LE = 0.50m; // Tối đa giảm 50% đơn hàng từ điểm thưởng

        public LoyaltyService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Đảm bảo người dùng có mã giới thiệu độc nhất
        /// </summary>
        public async Task<string> EnsureReferralCodeAsync(NguoiDung nguoiDung)
        {
            if (string.IsNullOrEmpty(nguoiDung.MaGioiThieu))
            {
                nguoiDung.MaGioiThieu = "REF-" + nguoiDung.MaNguoiDung.ToString("D5");
                _context.NguoiDungs.Update(nguoiDung);
                await _context.SaveChangesAsync();
            }
            return nguoiDung.MaGioiThieu;
        }

        /// <summary>
        /// Lấy tổng quan điểm tích lũy và thông tin giới thiệu của khách hàng
        /// </summary>
        public async Task<LoyaltySummaryViewModel> GetLoyaltySummaryAsync(int maNguoiDung, string baseUrl)
        {
            var user = await _context.NguoiDungs
                .Include(n => n.TaiKhoan)
                .FirstOrDefaultAsync(n => n.MaNguoiDung == maNguoiDung);

            if (user == null)
            {
                return new LoyaltySummaryViewModel();
            }

            var refCode = await EnsureReferralCodeAsync(user);

            var lichSu = await _context.LichSuDiemThuongs
                .Include(l => l.DonHang)
                .Where(l => l.MaNguoiDung == maNguoiDung)
                .OrderByDescending(l => l.NgayGiaoDich)
                .ToListAsync();

            int tongDiemThuongRef = lichSu
                .Where(l => l.LoaiGiaoDich == "ThuongGioiThieu")
                .Sum(l => l.SoDiem);

            string link = $"{baseUrl.TrimEnd('/')}/Auth/Register?ref={refCode}";

            return new LoyaltySummaryViewModel
            {
                DiemHienTai = user.DiemTichLuy,
                HangVip = user.HangVip ?? "VIP Khởi Đầu",
                MaGioiThieu = refCode,
                LinkGioiThieu = link,
                SoNguoiDaGioiThieu = user.SoNguoiDaGioiThieu,
                TongDiemThuongGioiThieu = tongDiemThuongRef,
                LichSu = lichSu
            };
        }

        /// <summary>
        /// Cộng điểm thưởng khi đơn hàng hoàn thành (1% giá trị đơn) và thưởng cho Người giới thiệu nếu là đơn đầu tiên
        /// </summary>
        public async Task<(int DiemCong, string Message)> CongDiemDonHangHoanThanhAsync(int maDonHang)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

            if (donHang == null || donHang.NguoiDung == null)
            {
                return (0, "Không tìm thấy đơn hàng hoặc thông tin khách hàng.");
            }

            // Kiểm tra xem đơn hàng này đã từng tích điểm chưa
            bool daTichDiem = await _context.LichSuDiemThuongs
                .AnyAsync(l => l.MaDonHang == maDonHang && l.LoaiGiaoDich == "TichDiemDonHang");

            if (daTichDiem)
            {
                return (0, "Đơn hàng này đã được cộng điểm tích lũy trước đó.");
            }

            // 1. Tính điểm cộng cho khách mua hàng (1% số tiền hàng thực tế sau chiết khấu)
            decimal tienHangThucTe = Math.Max(0, donHang.TongTien - donHang.PhiGiaoHang);
            int diemCong = (int)Math.Round(tienHangThucTe * TICH_DIEM_TI_LE);

            if (diemCong > 0)
            {
                donHang.NguoiDung.DiemTichLuy += diemCong;
                _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
                {
                    MaNguoiDung = donHang.MaNguoiDung,
                    SoDiem = diemCong,
                    LoaiGiaoDich = "TichDiemDonHang",
                    MoTa = $"Tích 1% điểm thưởng đơn hàng #{donHang.MaDonHang} hoàn tất ({tienHangThucTe:N0} đ)",
                    MaDonHang = maDonHang,
                    NgayGiaoDich = DateTime.Now
                });
            }

            // 2. Kiểm tra thưởng cho Người giới thiệu (nếu đây là đơn hàng hoàn thành đầu tiên của khách)
            if (donHang.NguoiDung.NguoiGioiThieuId.HasValue && donHang.NguoiDung.NguoiGioiThieuId.Value > 0)
            {
                // Kiểm tra xem khách hàng này đã có đơn hàng nào khác hoàn thành chưa
                int countCompleted = await _context.DonHangs
                    .CountAsync(d => d.MaNguoiDung == donHang.MaNguoiDung && d.TrangThaiDonHang == "Hoàn thành" && d.MaDonHang != maDonHang);

                if (countCompleted == 0)
                {
                    // Đây là đơn đầu tiên! Thưởng cho người giới thiệu
                    var nguoiGioiThieu = await _context.NguoiDungs.FindAsync(donHang.NguoiDung.NguoiGioiThieuId.Value);
                    if (nguoiGioiThieu != null)
                    {
                        nguoiGioiThieu.DiemTichLuy += THUONG_GIOI_THIEU_DIEM;
                        nguoiGioiThieu.SoNguoiDaGioiThieu += 1;

                        _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
                        {
                            MaNguoiDung = nguoiGioiThieu.MaNguoiDung,
                            SoDiem = THUONG_GIOI_THIEU_DIEM,
                            LoaiGiaoDich = "ThuongGioiThieu",
                            MoTa = $"Thưởng giới thiệu bạn bè: Khách hàng {donHang.NguoiDung.HoTen} đã hoàn tất đơn hàng đầu tiên #{donHang.MaDonHang}",
                            MaDonHang = maDonHang,
                            NgayGiaoDich = DateTime.Now
                        });

                        _context.NguoiDungs.Update(nguoiGioiThieu);
                    }
                }
            }

            _context.NguoiDungs.Update(donHang.NguoiDung);
            await _context.SaveChangesAsync();

            return (diemCong, $"Đã tích lũy thành công {diemCong:N0} điểm cho khách hàng #{donHang.NguoiDung.HoTen}.");
        }

        /// <summary>
        /// Tiêu điểm tích lũy để giảm trừ tiền đơn hàng
        /// </summary>
        public async Task<(bool Success, string Message)> TieuDiemThanhToanAsync(int maNguoiDung, int soDiemSuDung, int maDonHang, decimal tongTienDon)
        {
            if (soDiemSuDung <= 0) return (true, "Không sử dụng điểm.");

            var customer = await _context.NguoiDungs.FindAsync(maNguoiDung);
            if (customer == null) return (false, "Không tìm thấy khách hàng.");

            if (customer.DiemTichLuy < soDiemSuDung)
            {
                return (false, $"Bạn chỉ có {customer.DiemTichLuy:N0} điểm, không đủ để sử dụng {soDiemSuDung:N0} điểm.");
            }

            decimal maxDiemChoPhep = tongTienDon * MAX_TIEN_DIEM_TI_LE;
            if (soDiemSuDung > maxDiemChoPhep)
            {
                return (false, $"Điểm tích lũy chỉ được trừ tối đa 50% giá trị đơn hàng (tối đa {maxDiemChoPhep:N0} điểm).");
            }

            customer.DiemTichLuy -= soDiemSuDung;
            _context.NguoiDungs.Update(customer);

            _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
            {
                MaNguoiDung = maNguoiDung,
                SoDiem = -soDiemSuDung,
                LoaiGiaoDich = "TieuDiemThanhToan",
                MoTa = $"Sử dụng {soDiemSuDung:N0} điểm tích lũy thanh toán đơn hàng #{maDonHang}",
                MaDonHang = maDonHang,
                NgayGiaoDich = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, $"Đã trừ {soDiemSuDung:N0} điểm cho đơn hàng #{maDonHang}.");
        }

        /// <summary>
        /// Hoàn trả điểm khi đơn hàng bị hủy
        /// </summary>
        public async Task<(bool Success, string Message)> HoanDiemDonHangHuyAsync(int maDonHang)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang);

            if (donHang == null || donHang.NguoiDung == null || donHang.DiemSuDung <= 0)
            {
                return (false, "Đơn hàng không có điểm thưởng cần hoàn trả.");
            }

            // Kiểm tra xem đã từng hoàn điểm đơn này chưa
            bool daHoan = await _context.LichSuDiemThuongs
                .AnyAsync(l => l.MaDonHang == maDonHang && l.LoaiGiaoDich == "HoanDiem");

            if (daHoan)
            {
                return (false, "Đơn hàng này đã được hoàn trả điểm trước đó.");
            }

            donHang.NguoiDung.DiemTichLuy += donHang.DiemSuDung;
            _context.NguoiDungs.Update(donHang.NguoiDung);

            _context.LichSuDiemThuongs.Add(new LichSuDiemThuong
            {
                MaNguoiDung = donHang.MaNguoiDung,
                SoDiem = donHang.DiemSuDung,
                LoaiGiaoDich = "HoanDiem",
                MoTa = $"Hoàn trả {donHang.DiemSuDung:N0} điểm tích lũy do hủy đơn hàng #{maDonHang}",
                MaDonHang = maDonHang,
                NgayGiaoDich = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, $"Đã hoàn trả thành công {donHang.DiemSuDung:N0} điểm cho khách hàng.");
        }
    }
}
