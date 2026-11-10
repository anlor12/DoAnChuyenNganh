-- =========================================================================
-- SCRIPT KHỞI TẠO TOÀN BỘ CƠ SỞ DỮ LIỆU MỚI 100% TIẾNG VIỆT
-- Dự án: Hệ Thống Website Thời Trang Cao Cấp Fashion Store
-- Tên cơ sở dữ liệu: DoAnCoSoDB
-- =========================================================================

USE master;
GO

-- 1. ĐÓNG MỌI KẾT NỐI VÀ XÓA DATABASE CŨ NẾU ĐÃ TỒN TẠI
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'DoAnCoSoDB')
BEGIN
    ALTER DATABASE DoAnCoSoDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DoAnCoSoDB;
    PRINT N'===> ĐÃ XÓA DATABASE CŨ THÀNH CÔNG! <===';
END
GO

-- 2. TẠO CƠ SỞ DỮ LIỆU MỚI
CREATE DATABASE DoAnCoSoDB
COLLATE Vietnamese_CI_AS;
GO

USE DoAnCoSoDB;
GO

-- =========================================================================
-- TẠO CẤU TRÚC 15 BẢNG THỰC THỂ TIẾNG VIỆT 100%
-- =========================================================================

-- 1. Bảng TaiKhoans (Tài khoản người dùng đăng nhập)
CREATE TABLE TaiKhoans (
    MaTaiKhoan INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhau NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    SoDienThoai NVARCHAR(15) NULL,
    HoTen NVARCHAR(100) NOT NULL,
    VaiTro NVARCHAR(20) NOT NULL DEFAULT N'Khách hàng',
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Hoạt động',
    NgayTao DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 2. Bảng NguoiDungs (Thông tin hồ sơ cá nhân người dùng)
CREATE TABLE NguoiDungs (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    MaTaiKhoan INT NOT NULL UNIQUE,
    HoTen NVARCHAR(100) NOT NULL,
    GioiTinh NVARCHAR(10) NULL,
    NgaySinh DATETIME2 NULL,
    DiaChi NVARCHAR(255) NULL,
    ChieuCao FLOAT NULL,
    CanNang FLOAT NULL,
    AnhDaiDien NVARCHAR(255) NULL,
    CONSTRAINT FK_NguoiDungs_TaiKhoans FOREIGN KEY (MaTaiKhoan) 
        REFERENCES TaiKhoans(MaTaiKhoan) ON DELETE CASCADE
);
GO

-- 3. Bảng LoaiSanPhams (Phân loại danh mục sản phẩm)
CREATE TABLE LoaiSanPhams (
    MaLoaiSanPham INT IDENTITY(1,1) PRIMARY KEY,
    TenLoaiSanPham NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(255) NULL,
    HinhAnh NVARCHAR(255) NULL
);
GO

-- 4. Bảng SanPhams (Thông tin sản phẩm chính)
CREATE TABLE SanPhams (
    MaSanPham INT IDENTITY(1,1) PRIMARY KEY,
    MaLoaiSanPham INT NOT NULL,
    TenSanPham NVARCHAR(150) NOT NULL,
    MoTa NVARCHAR(1000) NULL,
    Gia DECIMAL(18,2) NOT NULL,
    HinhAnh NVARCHAR(255) NULL,
    KieuCoAo NVARCHAR(50) NULL,
    ChatLieu NVARCHAR(50) NULL,
    FormDang NVARCHAR(50) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Đang bán',
    GioiTinh NVARCHAR(15) NOT NULL DEFAULT N'Unisex',
    IsNoiBat BIT NOT NULL DEFAULT 0,
    IsBanChay BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SanPhams_LoaiSanPhams FOREIGN KEY (MaLoaiSanPham) 
        REFERENCES LoaiSanPhams(MaLoaiSanPham) ON DELETE NO ACTION
);
GO

-- 5. Bảng BienTheSanPhams (Biến thể kích cỡ và màu sắc sản phẩm)
CREATE TABLE BienTheSanPhams (
    MaBienThe INT IDENTITY(1,1) PRIMARY KEY,
    MaSanPham INT NOT NULL,
    KichCo NVARCHAR(20) NOT NULL,
    MauSac NVARCHAR(50) NOT NULL,
    Gia DECIMAL(18,2) NOT NULL,
    HinhAnh NVARCHAR(255) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Còn hàng',
    CONSTRAINT FK_BienTheSanPhams_SanPhams FOREIGN KEY (MaSanPham) 
        REFERENCES SanPhams(MaSanPham) ON DELETE CASCADE
);
GO

-- 6. Bảng KhoHangs (Quản lý tồn kho từng biến thể)
CREATE TABLE KhoHangs (
    MaKhoHang INT IDENTITY(1,1) PRIMARY KEY,
    MaBienThe INT NOT NULL,
    SoLuongTon INT NOT NULL DEFAULT 0,
    NgayCapNhat DATETIME2 NOT NULL DEFAULT GETDATE(),
    GhiChu NVARCHAR(255) NULL,
    CONSTRAINT FK_KhoHangs_BienTheSanPhams FOREIGN KEY (MaBienThe) 
        REFERENCES BienTheSanPhams(MaBienThe) ON DELETE CASCADE
);
GO

-- 7. Bảng GioHangs (Giỏ hàng của khách hàng)
CREATE TABLE GioHangs (
    MaGioHang INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT NOT NULL,
    NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Hoạt động',
    CONSTRAINT FK_GioHangs_NguoiDungs FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDungs(MaNguoiDung) ON DELETE CASCADE
);
GO

-- 8. Bảng ChiTietGioHangs (Các mặt hàng trong giỏ)
CREATE TABLE ChiTietGioHangs (
    MaChiTietGioHang INT IDENTITY(1,1) PRIMARY KEY,
    MaGioHang INT NOT NULL,
    MaBienThe INT NOT NULL,
    SoLuong INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ChiTietGioHangs_GioHangs FOREIGN KEY (MaGioHang) 
        REFERENCES GioHangs(MaGioHang) ON DELETE CASCADE,
    CONSTRAINT FK_ChiTietGioHangs_BienTheSanPhams FOREIGN KEY (MaBienThe) 
        REFERENCES BienTheSanPhams(MaBienThe) ON DELETE CASCADE
);
GO

-- 9. Bảng DonHangs (Đơn đặt hàng của khách)
CREATE TABLE DonHangs (
    MaDonHang INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT NOT NULL,
    NgayDat DATETIME2 NOT NULL DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiaChiGiaoHang NVARCHAR(255) NOT NULL,
    SoDienThoaiNhan NVARCHAR(15) NOT NULL,
    TrangThaiDonHang NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận',
    PhuongThucThanhToan NVARCHAR(50) NOT NULL DEFAULT N'COD',
    TrangThaiThanhToan NVARCHAR(50) NOT NULL DEFAULT N'Chưa thanh toán',
    GhiChu NVARCHAR(255) NULL,
    MaKhuyenMaiApDung NVARCHAR(50) NULL,
    SoTienGiam DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_DonHangs_NguoiDungs FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDungs(MaNguoiDung) ON DELETE NO ACTION
);
GO

-- 10. Bảng ChiTietDonHangs (Chi tiết từng sản phẩm đặt mua)
CREATE TABLE ChiTietDonHangs (
    MaChiTietDonHang INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang INT NOT NULL,
    MaBienThe INT NOT NULL,
    SoLuong INT NOT NULL DEFAULT 1,
    DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_ChiTietDonHangs_DonHangs FOREIGN KEY (MaDonHang) 
        REFERENCES DonHangs(MaDonHang) ON DELETE CASCADE,
    CONSTRAINT FK_ChiTietDonHangs_BienTheSanPhams FOREIGN KEY (MaBienThe) 
        REFERENCES BienTheSanPhams(MaBienThe) ON DELETE NO ACTION
);
GO

-- 11. Bảng KhuyenMais (Mã giảm giá voucher)
CREATE TABLE KhuyenMais (
    MaGiamGia NVARCHAR(50) PRIMARY KEY,
    PhanTramGiam INT NOT NULL,
    NgayKetThuc DATETIME2 NOT NULL,
    SoLuong INT NOT NULL DEFAULT 0,
    TrangThai BIT NOT NULL DEFAULT 1
);
GO

-- 12. Bảng DanhGias (Đánh giá sản phẩm và phân tích AI)
CREATE TABLE DanhGias (
    MaDanhGia INT IDENTITY(1,1) PRIMARY KEY,
    MaSanPham INT NOT NULL,
    MaNguoiDung INT NOT NULL,
    DiemDanhGia INT NOT NULL CHECK (DiemDanhGia BETWEEN 1 AND 5),
    NoiDung NVARCHAR(500) NULL,
    NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
    CamXucAi NVARCHAR(50) NULL,
    TuKhoaAi NVARCHAR(200) NULL,
    CONSTRAINT FK_DanhGias_SanPhams FOREIGN KEY (MaSanPham) 
        REFERENCES SanPhams(MaSanPham) ON DELETE CASCADE,
    CONSTRAINT FK_DanhGias_NguoiDungs FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDungs(MaNguoiDung) ON DELETE CASCADE
);
GO

-- 13. Bảng GoiYKichCos (Gợi ý kích cỡ thông minh)
CREATE TABLE GoiYKichCos (
    MaGoiY INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT NOT NULL,
    MaBienThe INT NOT NULL,
    KichCoGoiY NVARCHAR(20) NOT NULL,
    LyDoGoiY NVARCHAR(255) NULL,
    ThoiGianGoiY DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_GoiYKichCos_NguoiDungs FOREIGN KEY (MaNguoiDung) 
        REFERENCES NguoiDungs(MaNguoiDung) ON DELETE CASCADE,
    CONSTRAINT FK_GoiYKichCos_BienTheSanPhams FOREIGN KEY (MaBienThe) 
        REFERENCES BienTheSanPhams(MaBienThe) ON DELETE CASCADE
);
GO

-- 14. Bảng BoSuuTaps (Bộ sưu tập thời trang)
CREATE TABLE BoSuuTaps (
    MaBoSuuTap INT IDENTITY(1,1) PRIMARY KEY,
    TenBoSuuTap NVARCHAR(150) NOT NULL,
    MoTa NVARCHAR(500) NULL,
    HinhAnh NVARCHAR(255) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Hoạt động',
    NgayTao DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 15. Bảng DiemGhimBoSuuTaps (Điểm ghim sản phẩm trên ảnh bộ sưu tập)
CREATE TABLE DiemGhimBoSuuTaps (
    MaDiemGhim INT IDENTITY(1,1) PRIMARY KEY,
    MaBoSuuTap INT NOT NULL,
    MaSanPham INT NOT NULL,
    ToaDoTren DECIMAL(18,4) NOT NULL,
    ToaDoTrai DECIMAL(18,4) NOT NULL,
    TieuDe NVARCHAR(150) NULL,
    CONSTRAINT FK_DiemGhim_BoSuuTaps FOREIGN KEY (MaBoSuuTap) 
        REFERENCES BoSuuTaps(MaBoSuuTap) ON DELETE CASCADE,
    CONSTRAINT FK_DiemGhim_SanPhams FOREIGN KEY (MaSanPham) 
        REFERENCES SanPhams(MaSanPham) ON DELETE CASCADE
);
GO

-- 16. Bảng CauHinhHeThongs (Cấu hình chung hệ thống)
CREATE TABLE CauHinhHeThongs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL UNIQUE,
    [Value] NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(255) NULL
);
GO

-- =========================================================================
-- NẠP DỮ LIỆU MẪU BAN ĐẦU (SEEDING DATA 100% TIẾNG VIỆT)
-- =========================================================================

-- 1. Nạp Tài khoản Quản trị viên & Khách hàng
INSERT INTO TaiKhoans (TenDangNhap, MatKhau, Email, SoDienThoai, HoTen, VaiTro, TrangThai, NgayTao)
VALUES 
(N'admin', N'admin123', N'admin@fashionstore.vn', N'0988888888', N'Quản Trị Viên', N'Quản trị viên', N'Hoạt động', GETDATE()),
(N'khachhang', N'123456', N'khachhang@gmail.com', N'0912345678', N'Nguyễn Văn Khách', N'Khách hàng', N'Hoạt động', GETDATE());

INSERT INTO NguoiDungs (MaTaiKhoan, HoTen, GioiTinh, NgaySinh, DiaChi, ChieuCao, CanNang)
VALUES 
(1, N'Quản Trị Viên Hệ Thống', N'Nam', '1995-01-01', N'Tòa nhà Viwaseen, 48 Tố Hữu, Nam Từ Liêm, Hà Nội', 175, 70),
(2, N'Nguyễn Văn Khách', N'Nam', '1998-05-20', N'Số 123 Đường Cầu Giấy, Quận Cầu Giấy, Hà Nội', 172, 68);

-- 2. Nạp 8 Danh mục sản phẩm thời trang chuẩn
SET IDENTITY_INSERT LoaiSanPhams ON;
INSERT INTO LoaiSanPhams (MaLoaiSanPham, TenLoaiSanPham, MoTa, HinhAnh)
VALUES 
(1, N'Áo Nam', N'Áo Fashion Store đa dạng kiểu dáng từ hiện đại đến truyền thống, bạn có thể dễ dàng lựa chọn một mẫu áo sơ mi hay áo thun theo phong cách riêng với chất liệu mềm mại, thoáng mát, phom dáng vừa ý tại Fashion Store.', N'/images/news_office_gentleman.png'),
(2, N'Quần Nam', N'Các dòng quần tây, quần jean, quần khaki Fashion Store với thiết kế phom dáng chuẩn, chất liệu co giãn thoải mái, tôn dáng lịch lãm và chỉn chu trong mọi hoàn cảnh.', N'/images/news_smart_active.png'),
(3, N'Áo Nữ', N'Áo thun, áo sơ mi nữ thanh lịch và điệu đà, mang lại diện mạo trẻ trung, duyên dáng cho các cô nàng công sở cũng như dạo phố.', N'https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?w=1600&q=80'),
(4, N'Quần Nữ', N'Bộ sưu tập quần nữ thời trang tôn dáng, các kiểu dáng quần tây, quần jean nữ thời thượng, chất liệu êm ái mang đến sự thoải mái tối đa cả ngày dài.', N'https://images.unsplash.com/photo-1541099649105-f69ad21f3246?w=1600&q=80'),
(5, N'Váy & Đầm', N'Váy chữ A, đầm công sở, đầm dạ hội quyến rũ với đường cắt may tinh tế tôn vinh vóc dáng phái đẹp.', N'https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=1600&q=80'),
(6, N'Áo Khoác', N'Áo khoác gió, áo khoác dạ, jacket cao cấp giữ ấm hoàn hảo, cản gió linh hoạt và khẳng định phong cách riêng.', N'https://images.unsplash.com/photo-1551028719-00167b16eac5?w=1600&q=80'),
(7, N'Đồ Thể Thao', N'Trang phục thể thao co giãn 4 chiều, thấm hút mồ hôi tốt, năng động và sành điệu.', N'https://images.unsplash.com/photo-1483721310020-03333e577078?w=1600&q=80'),
(8, N'Phụ Kiện', N'Thắt lưng da, ví da, cà vạt, nơ và phụ kiện cao cấp giúp tạo điểm nhấn hoàn hảo cho trang phục.', N'https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=1600&q=80');
SET IDENTITY_INSERT LoaiSanPhams OFF;

-- 3. Nạp Mã giảm giá / Voucher
INSERT INTO KhuyenMais (MaGiamGia, PhanTramGiam, NgayKetThuc, SoLuong, TrangThai)
VALUES 
(N'CHAOMUNG', 10, DATEADD(YEAR, 1, GETDATE()), 1000, 1),
(N'HE2026', 20, DATEADD(MONTH, 6, GETDATE()), 500, 1),
(N'VIPFS', 15, DATEADD(YEAR, 1, GETDATE()), 200, 1);

-- 4. Nạp Cấu hình hệ thống
INSERT INTO CauHinhHeThongs ([Key], [Value], [Description])
VALUES 
(N'SiteName', N'Fashion Store', N'Tên thương hiệu website'),
(N'Hotline', N'1900 8079', N'Số điện thoại tổng đài CSKH'),
(N'Email', N'cskh@fashionstore.vn', N'Email hỗ trợ khách hàng'),
(N'Address', N'Tầng 17, Tòa nhà Viwaseen, 48 Tố Hữu, Nam Từ Liêm, Hà Nội', N'Địa chỉ trụ sở chính'),
(N'ShippingFee', N'30000', N'Phí giao hàng mặc định (VNĐ)'),
(N'FreeShippingThreshold', N'500000', N'Mức đơn hàng tối thiểu để được miễn phí ship (VNĐ)');

-- 5. Nạp 2 Bộ sưu tập mẫu
INSERT INTO BoSuuTaps (TenBoSuuTap, MoTa, HinhAnh, TrangThai, NgayTao)
VALUES 
(N'BST Quý Ông Công Sở - Gentleman Look 2026', N'Phong cách lịch lãm, hiện đại dành cho quý ông thành đạt với những đường may tinh tế và chất liệu vải sợi sen độc quyền.', N'/images/news_office_gentleman.png', N'Hoạt động', GETDATE()),
(N'BST Năng Động Dạo Phố - Smart Active 2026', N'Sự kết hợp hoàn hảo giữa nét trẻ trung, năng động và thanh lịch với chất liệu co giãn 4 chiều thoáng mát.', N'/images/news_smart_active.png', N'Hoạt động', GETDATE());

PRINT N'====================================================================';
PRINT N'===> KHỞI TẠO DATABASE MỚI 100% TIẾNG VIỆT THÀNH CÔNG RỰC RỠ! <===';
PRINT N'====================================================================';
GO
