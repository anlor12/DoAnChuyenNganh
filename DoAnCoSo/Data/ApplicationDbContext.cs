using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Data
{
    /// <summary>
    /// Lớp ngữ cảnh cơ sở dữ liệu (Database Context) của ứng dụng.
    /// Quản lý toàn bộ 15 thực thể dữ liệu với ngôn ngữ 100% Tiếng Việt thuần túy.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Khởi tạo một đối tượng ApplicationDbContext mới với các tùy chọn cấu hình được cung cấp.
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================================
        // ĐĂNG KÝ CÁC THỰC THỂ (TABLES) 100% TIẾNG VIỆT
        // =========================================================================

        /// <summary>Bảng quản lý tài khoản người dùng đăng nhập hệ thống</summary>
        public DbSet<TaiKhoan> TaiKhoans { get; set; }

        /// <summary>Bảng lưu trữ thông tin cá nhân chi tiết của người dùng</summary>
        public DbSet<NguoiDung> NguoiDungs { get; set; }

        /// <summary>Bảng phân loại các danh mục sản phẩm (ví dụ: Áo Nam, Quần Nam, Váy & Đầm...)</summary>
        public DbSet<LoaiSanPham> LoaiSanPhams { get; set; }

        /// <summary>Bảng lưu trữ thông tin sản phẩm chính</summary>
        public DbSet<SanPham> SanPhams { get; set; }

        /// <summary>Bảng chi tiết các biến thể sản phẩm (Kích thước, Màu sắc, Giá tiền, Hình ảnh cụ thể)</summary>
        public DbSet<BienTheSanPham> BienTheSanPhams { get; set; }

        /// <summary>Bảng quản lý số lượng tồn kho của từng biến thể sản phẩm</summary>
        public DbSet<KhoHang> KhoHangs { get; set; }

        /// <summary>Bảng giỏ hàng của từng khách hàng</summary>
        public DbSet<GioHang> GioHangs { get; set; }

        /// <summary>Bảng lưu các sản phẩm được chọn và số lượng tương ứng trong giỏ hàng</summary>
        public DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }

        /// <summary>Bảng lưu thông tin đơn đặt hàng của khách hàng</summary>
        public DbSet<DonHang> DonHangs { get; set; }

        /// <summary>Bảng lưu chi tiết các sản phẩm trong mỗi đơn hàng</summary>
        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

        /// <summary>Bảng quản lý mã giảm giá / Voucher khuyến mãi</summary>
        public DbSet<KhuyenMai> KhuyenMais { get; set; }

        /// <summary>Bảng lưu trữ thông tin đánh giá sản phẩm của khách hàng và phân tích AI</summary>
        public DbSet<DanhGia> DanhGias { get; set; }

        /// <summary>Bảng lưu trữ dữ liệu gợi ý kích cỡ thông minh dựa trên chiều cao, cân nặng</summary>
        public DbSet<GoiYKichCo> GoiYKichCos { get; set; }

        /// <summary>Bảng định nghĩa các bộ sưu tập thời trang (thay thế LookbookSet)</summary>
        public DbSet<BoSuuTap> BoSuuTaps { get; set; }

        /// <summary>Bảng định nghĩa các điểm ghim sản phẩm trên ảnh bộ sưu tập (thay thế LookbookHotspot)</summary>
        public DbSet<DiemGhimBoSuuTap> DiemGhimBoSuuTaps { get; set; }

        /// <summary>Bảng lưu trữ các cấu hình chung của website</summary>
        public DbSet<CauHinhHeThong> CauHinhHeThongs { get; set; }

        /// <summary>Bảng quản lý bài viết / tin tức thời trang</summary>
        public DbSet<BaiViet> BaiViets { get; set; }

        /// <summary>Bảng quản lý đăng ký nhận tin khi có hàng lại (Back-in-Stock Alert)</summary>
        public DbSet<ThongBaoHetHang> ThongBaoHetHangs { get; set; }

        /// <summary>Bảng ghi nhận lịch sử biến động điểm thưởng & tích lũy (Loyalty Points)</summary>
        public DbSet<LichSuDiemThuong> LichSuDiemThuongs { get; set; }

        /// <summary>Bảng nhật ký kiểm soát thao tác hệ thống của Admin/Nhân viên (Audit Logs)</summary>
        public DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }

        // =========================================================================
        // CẤU HÌNH RÀNG BUỘC TOÀN VẸN VÀ QUAN HỆ (FLUENT API)
        // =========================================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. TÀI KHOẢN & NGƯỜI DÙNG (Quan hệ 1-1, Unique Indexes)
            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.HasIndex(e => e.TenDangNhap).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasOne(e => e.NguoiDung)
                      .WithOne(e => e.TaiKhoan)
                      .HasForeignKey<NguoiDung>(e => e.MaTaiKhoan)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 2. LOẠI SẢN PHẨM & SẢN PHẨM (Quan hệ 1-N, Restrict khi xóa danh mục còn sản phẩm)
            modelBuilder.Entity<LoaiSanPham>(entity =>
            {
                entity.HasOne(e => e.LoaiCha)
                      .WithMany(e => e.LoaiCons)
                      .HasForeignKey(e => e.MaLoaiCha)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SanPham>(entity =>
            {
                entity.Property(e => e.Gia).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.LoaiSanPham)
                      .WithMany(e => e.SanPhams)
                      .HasForeignKey(e => e.MaLoaiSanPham)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. SẢN PHẨM & BIẾN THỂ (Cascade Delete khi xóa sản phẩm)
            modelBuilder.Entity<BienTheSanPham>(entity =>
            {
                entity.Property(e => e.Gia).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.SanPham)
                      .WithMany(e => e.BienTheSanPhams)
                      .HasForeignKey(e => e.MaSanPham)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 4. KHO HÀNG & BIẾN THỂ (Cascade Delete)
            modelBuilder.Entity<KhoHang>(entity =>
            {
                entity.HasOne(e => e.BienTheSanPham)
                      .WithMany(e => e.KhoHangs)
                      .HasForeignKey(e => e.MaBienThe)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. GIỎ HÀNG & CHI TIẾT GIỎ HÀNG (Cascade Delete)
            modelBuilder.Entity<GioHang>(entity =>
            {
                entity.HasOne(e => e.NguoiDung)
                      .WithMany(e => e.GioHangs)
                      .HasForeignKey(e => e.MaNguoiDung)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ChiTietGioHang>(entity =>
            {
                entity.HasOne(e => e.GioHang)
                      .WithMany(e => e.ChiTietGioHangs)
                      .HasForeignKey(e => e.MaGioHang)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.BienTheSanPham)
                      .WithMany()
                      .HasForeignKey(e => e.MaBienThe)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 6. ĐƠN HÀNG & CHI TIẾT ĐƠN HÀNG (Decimal Precision & Quan hệ)
            modelBuilder.Entity<DonHang>(entity =>
            {
                entity.Property(e => e.TongTien).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SoTienGiam).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SoTienGiamVip).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PhiGiaoHang).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.NguoiDung)
                      .WithMany(e => e.DonHangs)
                      .HasForeignKey(e => e.MaNguoiDung)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ChiTietDonHang>(entity =>
            {
                entity.Property(e => e.DonGia).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.DonHang)
                      .WithMany(e => e.ChiTietDonHangs)
                      .HasForeignKey(e => e.MaDonHang)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.BienTheSanPham)
                      .WithMany()
                      .HasForeignKey(e => e.MaBienThe)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.Property(e => e.TongChiTieu).HasColumnType("decimal(18,2)");
            });

            // 7. KHUYẾN MÃI (Unique Mã Voucher & Precision)
            modelBuilder.Entity<KhuyenMai>(entity =>
            {
                entity.HasIndex(k => k.Code).IsUnique();
                entity.Property(e => e.GiaTriDonToiThieu).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GiamToiDa).HasColumnType("decimal(18,2)");
            });

            // 8. ĐÁNH GIÁ (Cascade Delete)
            modelBuilder.Entity<DanhGia>(entity =>
            {
                entity.HasOne(d => d.SanPham)
                      .WithMany(s => s.DanhGias)
                      .HasForeignKey(d => d.MaSanPham)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.NguoiDung)
                      .WithMany()
                      .HasForeignKey(d => d.MaNguoiDung)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 9. GỢI Ý KÍCH CỠ (Cascade Delete)
            modelBuilder.Entity<GoiYKichCo>(entity =>
            {
                entity.HasOne(g => g.NguoiDung)
                      .WithMany(n => n.GoiYKichCos)
                      .HasForeignKey(g => g.MaNguoiDung)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(g => g.BienTheSanPham)
                      .WithMany()
                      .HasForeignKey(g => g.MaBienThe)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 10. BỘ SƯU TẬP & ĐIỂM GHIM (Cascade Delete, Decimal coordinates)
            modelBuilder.Entity<BoSuuTap>(entity =>
            {
                entity.HasMany(b => b.DiemGhimBoSuuTaps)
                      .WithOne(d => d.BoSuuTap)
                      .HasForeignKey(d => d.MaBoSuuTap)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DiemGhimBoSuuTap>(entity =>
            {
                entity.Property(d => d.ToaDoTren).HasColumnType("decimal(18,4)");
                entity.Property(d => d.ToaDoTrai).HasColumnType("decimal(18,4)");

                entity.HasOne(d => d.SanPham)
                      .WithMany(s => s.DiemGhimBoSuuTaps)
                      .HasForeignKey(d => d.MaSanPham)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}