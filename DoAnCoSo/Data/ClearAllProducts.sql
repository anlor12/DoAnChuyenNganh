-- =========================================================================
-- SCRIPT XÓA TOÀN BỘ SẢN PHẨM VÀ DỮ LIỆU LIÊN QUAN TRONG CƠ SỞ DỮ LIỆU
-- Dự án: DoAnCoSo - Thời trang Fashion Store
-- Lưu ý: Thứ tự xóa tuân thủ ràng buộc khóa ngoại (Foreign Key Constraints)
-- =========================================================================

USE DoAnCoSoDB;
GO

BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Xóa các bảng con liên kết với BienTheSanPham và SanPham
    DELETE FROM ChiTietDonHangs;
    DELETE FROM ChiTietGioHangs;
    DELETE FROM KhoHangs;
    DELETE FROM GoiYKichCos;
    DELETE FROM DanhGias;
    DELETE FROM LookbookHotspots;

    -- 2. Xóa toàn bộ biến thể sản phẩm (BienTheSanPham)
    DELETE FROM BienTheSanPhams;

    -- 3. Xóa toàn bộ sản phẩm chính (SanPham)
    DELETE FROM SanPhams;

    -- 4. Reset chỉ số tự tăng (IDENTITY) về 0 để khi thêm mới sẽ bắt đầu từ 1
    DBCC CHECKIDENT ('ChiTietDonHangs', RESEED, 0);
    DBCC CHECKIDENT ('ChiTietGioHangs', RESEED, 0);
    DBCC CHECKIDENT ('KhoHangs', RESEED, 0);
    DBCC CHECKIDENT ('GoiYKichCos', RESEED, 0);
    DBCC CHECKIDENT ('DanhGias', RESEED, 0);
    DBCC CHECKIDENT ('LookbookHotspots', RESEED, 0);
    DBCC CHECKIDENT ('BienTheSanPhams', RESEED, 0);
    DBCC CHECKIDENT ('SanPhams', RESEED, 0);

    COMMIT TRANSACTION;
    PRINT N'===> ĐÃ XÓA TOÀN BỘ SẢN PHẨM VÀ RESET IDENTITY THÀNH CÔNG! <===';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'===> CÓ LỖI XẢY RA KHI XÓA SẢN PHẨM: ' + ERROR_MESSAGE();
END CATCH;
GO
