using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace DoAnCoSo.Data
{
    /// <summary>
    /// Lớp khởi tạo dữ liệu mặc định ban đầu cho cơ sở dữ liệu (Database Seeding 100% Tiếng Việt).
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Đảm bảo database đã được tạo
            context.Database.EnsureCreated();

            // Cập nhật cấu trúc bảng và các cột mới nếu chưa có
            EnsureSchemaUpToDate(context);

            // 1. Khởi tạo 8 Danh mục sản phẩm thời trang cao cấp
            SeedCategories(context);

            // 2. Khởi tạo Tài khoản Quản trị viên & Khách hàng mẫu
            SeedAccounts(context);

            // 3. Khởi tạo Mã khuyến mãi / Voucher & Quà tặng
            SeedVouchers(context);

            // 4. Khởi tạo Cấu hình hệ thống mặc định & Giới thiệu
            SeedSystemConfigurations(context);

            // 5. Khởi tạo Bài viết / Tin tức thời trang mẫu
            SeedArticles(context);

            // 6. Khởi tạo Bộ sưu tập thời trang mẫu
            SeedLookbookSets(context);

            // 7. Khởi tạo Sản phẩm mẫu kèm biến thể và tồn kho
            SeedProducts(context);

            // 8. Khởi tạo Điểm ghim Lookbook tương tác
            SeedLookbookHotspots(context);

            // 9. Chuẩn hóa trạng thái ngôn ngữ (bỏ qua nếu bảng chưa có dữ liệu)
            try
            {
                StandardizeLanguageStatus(context);
            }
            catch
            {
                // Bỏ qua nếu các bảng chưa được tạo hoặc chưa có dữ liệu
            }

            context.SaveChanges();
        }

        private static LoaiSanPham EnsureCategory(ApplicationDbContext context, string name, string desc, string img, int? parentId)
        {
            var cat = context.LoaiSanPhams.FirstOrDefault(x => x.TenLoaiSanPham == name);
            if (cat != null)
            {
                bool needUpdate = false;
                // Chỉ gán danh mục cha nếu danh mục chưa có cha
                if (!cat.MaLoaiCha.HasValue && parentId.HasValue)
                {
                    cat.MaLoaiCha = parentId;
                    needUpdate = true;
                }
                // Chỉ thiết lập mô tả mặc định nếu chưa có mô tả
                if (string.IsNullOrWhiteSpace(cat.MoTa) && !string.IsNullOrEmpty(desc))
                {
                    cat.MoTa = desc;
                    needUpdate = true;
                }
                // TUYỆT ĐỐI KHÔNG ghi đè ảnh đã được cập nhật bởi Admin
                // Chỉ điền ảnh mặc định nếu danh mục chưa có ảnh
                if (string.IsNullOrWhiteSpace(cat.HinhAnh) && !string.IsNullOrEmpty(img))
                {
                    cat.HinhAnh = img;
                    needUpdate = true;
                }

                if (needUpdate)
                {
                    context.LoaiSanPhams.Update(cat);
                }
            }
            else
            {
                cat = new LoaiSanPham
                {
                    TenLoaiSanPham = name,
                    MoTa = desc,
                    HinhAnh = img,
                    MaLoaiCha = parentId
                };
                context.LoaiSanPhams.Add(cat);
            }
            context.SaveChanges();
            return cat;
        }

        private static void SeedCategories(ApplicationDbContext context)
        {
            // QUAN TRỌNG: Nếu CSDL đã có danh mục thì KHÔNG seed lại
            // Điều này đảm bảo khi Admin xóa bớt danh mục thì danh mục đó KHÔNG bị tự động tạo lại khi restart web
            if (context.LoaiSanPhams.Any())
            {
                return;
            }

            // =========================================================================
            // 1. KHỞI TẠO CÁC DANH MỤC GỐC (ROOT CATEGORIES)
            // =========================================================================
            var catAo = EnsureCategory(context, "Áo", 
                "Bộ sưu tập các dòng áo nam thời trang cao cấp: Áo T-Shirt, Áo Polo, Áo Sơ Mi, Áo Jacket, Áo Blazer, Áo Nỉ với chất liệu mềm mại, thoáng mát và phom dáng chuẩn mực.", 
                "/images/news_office_gentleman.png", null);

            var catQuan = EnsureCategory(context, "Quần", 
                "Bộ sưu tập các dòng quần thời trang phong cách: Quần Jeans, Quần Tây, Quần Khaki, Quần Short, Quần Nỉ phom dáng tôn dáng, chỉn chu và bền bỉ.", 
                "/images/news_smart_active.png", null);

            var catPhuKien = EnsureCategory(context, "Phụ Kiện", 
                "Bộ sưu tập phụ kiện thời trang cao cấp: Giày dép, Dây lưng, Ví da, Đồ lót, Tất và Cà vạt hoàn thiện phong cách phái mạnh.", 
                "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=1600&q=80", null);

            // =========================================================================
            // 2. KHỞI TẠO CÁC DANH MỤC CON CỦA 'ÁO'
            // =========================================================================
            var catTShirt = EnsureCategory(context, "Áo T-Shirt", 
                "Áo thun T-shirt năng động, chất liệu 100% cotton compact thoáng mát, form dáng trẻ trung hiện đại.", 
                "/images/products/dc37be18-19ba-4298-93ac-3ffac7b89c4c.jpg", catAo.MaLoaiSanPham);

            var catPolo = EnsureCategory(context, "Áo Polo", 
                "Áo polo thanh lịch, dệt pique mắt chim co giãn 4 chiều, chuẩn mực phong cách smart casual.", 
                "/images/products/30ec7ac9-08a4-4e6b-847d-d4da3500c3cf.jpg", catAo.MaLoaiSanPham);

            var catSoMi = EnsureCategory(context, "Áo Sơ Mi", 
                "Áo sơ mi công sở và dạo phố với chất liệu sợi tre, sợi sen cao cấp, chống nhăn tự nhiên.", 
                "/images/products/03f8602f-246c-408e-882c-b1d46e7aff3b.jpg", catAo.MaLoaiSanPham);

            var catJacket = EnsureCategory(context, "Áo Jacket", 
                "Áo jacket cá tính, cản gió và chống nước nhẹ, thiết kế năng động và khỏe khoắn.", 
                "/images/products/889da659-7c27-4861-b179-6ace42c83a9a.jpg", catAo.MaLoaiSanPham);

            var catBlazer = EnsureCategory(context, "Áo Blazer", 
                "Áo khoác blazer may đo tinh tế, tôn phom dáng sang trọng cho các sự kiện và công sở.", 
                "/images/news_office_gentleman.png", catAo.MaLoaiSanPham);

            var catNi = EnsureCategory(context, "Áo Nỉ", 
                "Áo nỉ chui đầu và nỉ hoodie ấm áp, êm ái, phong cách streetwear đô thị sành điệu.", 
                "/images/news_smart_active.png", catAo.MaLoaiSanPham);

            var catSoMiTrang = EnsureCategory(context, "Áo Sơ Mi Trắng",
                "Áo sơ mi trắng công sở thanh lịch, chất vải chống nhăn cao cấp chuẩn phong cách quý ông.",
                "/images/fashionstore/blog_white_shirt.jpg", catAo.MaLoaiSanPham);

            var catVeston = EnsureCategory(context, "Bộ Veston",
                "Bộ veston nam sang trọng, phom dáng chuẩn mực cho các dịp quan trọng và công sở.",
                "/images/news_office_gentleman.png", catAo.MaLoaiSanPham);

            var catLen = EnsureCategory(context, "Áo Len",
                "Áo len nam dệt kim cao cấp, mềm mại, giữ ấm hoàn hảo cho mùa thu đông.",
                "/images/fashionstore/mega_menu_ao_banner.jpg", catAo.MaLoaiSanPham);

            var catBoDo = EnsureCategory(context, "Bộ Đồ",
                "Bộ đồ nam thời trang phong cách casual, tiện dụng và thoải mái suốt ngày dài.",
                "/images/news_smart_active.png", catAo.MaLoaiSanPham);

            // =========================================================================
            // 3. KHỞI TẠO CÁC DANH MỤC CON CỦA 'QUẦN'
            // =========================================================================
            var catJeans = EnsureCategory(context, "Quần Jeans", 
                "Quần jeans nam cao cấp co giãn, wash màu tinh tế, bền bỉ và tôn phom dáng trẻ trung.", 
                "https://images.unsplash.com/photo-1542272604-780c96856592?w=1200&q=80", catQuan.MaLoaiSanPham);

            var catTay = EnsureCategory(context, "Quần Tây", 
                "Quần tây công sở thiết kế cạp chun ẩn thông minh, chất vải chéo co giãn đứng phom lịch lãm.", 
                "https://images.unsplash.com/photo-1594938298603-c8148c4dae35?w=1200&q=80", catQuan.MaLoaiSanPham);

            var catKhaki = EnsureCategory(context, "Quần Khaki", 
                "Quần khaki chino phom dáng hiện đại, chất liệu cotton spandex êm ái, đa dụng trong mọi hoàn cảnh.", 
                "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?w=1200&q=80", catQuan.MaLoaiSanPham);

            var catShort = EnsureCategory(context, "Quần Short", 
                "Quần short kaki và thể thao dạo phố thoáng mát, năng động cho ngày hè sảng khoái.", 
                "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=1200&q=80", catQuan.MaLoaiSanPham);

            var catQuanNi = EnsureCategory(context, "Quần Nỉ", 
                "Quần jogger nỉ da cá dày dặn, bo gấu gọn gàng, phong cách athleisure khỏe khoắn.", 
                "https://images.unsplash.com/photo-1552902865-b72c031ac5ea?w=1200&q=80", catQuan.MaLoaiSanPham);

            // =========================================================================
            // 4. KHỞI TẠO CÁC DANH MỤC CON CỦA 'PHỤ KIỆN'
            // =========================================================================
            var catGiay = EnsureCategory(context, "Giày", 
                "Bộ sưu tập các dòng giày thời trang nam: Giày Sneaker, Giày Tây, Giày Lười, Giày Boots, Sandal & Dép.", 
                "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=1200&q=80", catPhuKien.MaLoaiSanPham);

            var catDayLung = EnsureCategory(context, "Dây Lưng", 
                "Thắt lưng dây da bò nguyên tấm cao cấp, mặt khóa kim loại tự động mạ crom sang trọng.", 
                "https://images.unsplash.com/photo-1624222247344-550fb60583dc?w=1200&q=80", catPhuKien.MaLoaiSanPham);

            var catViDa = EnsureCategory(context, "Ví Da", 
                "Ví da nam may thủ công tỉ mỉ, chất da saffiano chống trầy xước, nhiều ngăn tiện lợi.", 
                "https://images.unsplash.com/photo-1627123424574-724758594e93?w=1200&q=80", catPhuKien.MaLoaiSanPham);

            var catDoLot = EnsureCategory(context, "Đồ Lót", 
                "Quần lót nam Boxer vải sợi tre / modal kháng khuẩn, thoáng khí và co giãn 4 chiều.", 
                "https://images.unsplash.com/photo-1583496661160-fb5886a0aaaa?w=1200&q=80", catPhuKien.MaLoaiSanPham);

            var catTat = EnsureCategory(context, "Tất", 
                "Tất dệt kim cotton khử mùi, êm chân và chống trơn trượt hoàn hảo.", 
                "https://images.unsplash.com/photo-1582966772680-860e372bb558?w=1200&q=80", catPhuKien.MaLoaiSanPham);

            var catCaVat = EnsureCategory(context, "Cà Vạt", 
                "Cà vạt lụa dệt hoa văn jacquard tinh tế, điểm nhấn hoàn hảo cho bộ âu phục quý ông.", 
                "https://images.unsplash.com/photo-1589756033938-5fed1e082c14?w=1200&q=80", catPhuKien.MaLoaiSanPham);

            // 4.1 Danh mục con thuộc 'Giày' (Cấp 3)
            var catSneaker = EnsureCategory(context, "Giày Sneaker", 
                "Giày sneaker thể thao phom dáng classic và chunky, đệm êm nâng đỡ từng bước chân.", 
                "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=1200&q=80", catGiay.MaLoaiSanPham);

            var catGiayTay = EnsureCategory(context, "Giày Tây", 
                "Giày tây Oxford & Derby da bò nhập khẩu, đế phíp thủ công sắc sảo chuẩn mực công sở.", 
                "https://images.unsplash.com/photo-1533867617858-e7b97e060509?w=1200&q=80", catGiay.MaLoaiSanPham);

            var catGiayLuoi = EnsureCategory(context, "Giày Lười", 
                "Giày lười Loafer da bóng êm ái, tiện lợi xỏ vào và tháo ra, thanh lịch mọi góc nhìn.", 
                "https://images.unsplash.com/photo-1614252235316-8c857d38b5f4?w=1200&q=80", catGiay.MaLoaiSanPham);

            var catGiayBoots = EnsureCategory(context, "Giày Boots", 
                "Giày Chelsea boots da lộn / da bóng cá tính, thời thượng và tôn dáng mạnh mẽ.", 
                "https://images.unsplash.com/photo-1638247025967-b4e38f787b76?w=1200&q=80", catGiay.MaLoaiSanPham);

            var catSandalDep = EnsureCategory(context, "Sandal & Dép", 
                "Sandal da và dép đúc nguyên khối chống nước, êm nhẹ cho những chuyến dạo chơi thoải mái.", 
                "https://images.unsplash.com/photo-1603487742131-4160ec999306?w=1200&q=80", catGiay.MaLoaiSanPham);

            // =========================================================================
            // 5. ĐỒNG BỘ LẠI SẢN PHẨM HIỆN CÓ THEO TỪ KHÓA
            // =========================================================================
            // Chỉ đồng bộ cho những sản phẩm chưa được gán loại hoặc loại không tồn tại
            var unassignedProducts = context.SanPhams
                .Where(p => p.MaLoaiSanPham <= 0 || !context.LoaiSanPhams.Any(l => l.MaLoaiSanPham == p.MaLoaiSanPham))
                .ToList();
            foreach (var p in unassignedProducts)
            {
                var pLower = p.TenSanPham.ToLower();
                
                // Áo
                if (pLower.Contains("sơ mi")) p.MaLoaiSanPham = catSoMi.MaLoaiSanPham;
                else if (pLower.Contains("polo")) p.MaLoaiSanPham = catPolo.MaLoaiSanPham;
                else if (pLower.Contains("t-shirt") || pLower.Contains("thun") || pLower.Contains("tshirt")) p.MaLoaiSanPham = catTShirt.MaLoaiSanPham;
                else if (pLower.Contains("jacket") || pLower.Contains("khoác gió") || pLower.Contains("khoác")) p.MaLoaiSanPham = catJacket.MaLoaiSanPham;
                else if (pLower.Contains("blazer")) p.MaLoaiSanPham = catBlazer.MaLoaiSanPham;
                else if (pLower.Contains("nỉ") && (pLower.Contains("áo") || pLower.Contains("hoodie") || pLower.Contains("sweatshirt"))) p.MaLoaiSanPham = catNi.MaLoaiSanPham;
                
                // Quần
                else if (pLower.Contains("jean")) p.MaLoaiSanPham = catJeans.MaLoaiSanPham;
                else if (pLower.Contains("quần tây") || pLower.Contains("quần âu")) p.MaLoaiSanPham = catTay.MaLoaiSanPham;
                else if (pLower.Contains("khaki") || pLower.Contains("kaki")) p.MaLoaiSanPham = catKhaki.MaLoaiSanPham;
                else if (pLower.Contains("short") || pLower.Contains("đùi") || pLower.Contains("ngố")) p.MaLoaiSanPham = catShort.MaLoaiSanPham;
                else if (pLower.Contains("nỉ") && pLower.Contains("quần") || pLower.Contains("jogger")) p.MaLoaiSanPham = catQuanNi.MaLoaiSanPham;
                else if (pLower.Contains("quần")) p.MaLoaiSanPham = catJeans.MaLoaiSanPham;

                // Phụ kiện / Giày
                else if (pLower.Contains("sneaker")) p.MaLoaiSanPham = catSneaker.MaLoaiSanPham;
                else if (pLower.Contains("giày tây") || pLower.Contains("oxford") || pLower.Contains("derby")) p.MaLoaiSanPham = catGiayTay.MaLoaiSanPham;
                else if (pLower.Contains("loafer") || pLower.Contains("giày lười")) p.MaLoaiSanPham = catGiayLuoi.MaLoaiSanPham;
                else if (pLower.Contains("boots") || pLower.Contains("boot")) p.MaLoaiSanPham = catGiayBoots.MaLoaiSanPham;
                else if (pLower.Contains("sandal") || pLower.Contains("dép")) p.MaLoaiSanPham = catSandalDep.MaLoaiSanPham;
                else if (pLower.Contains("giày")) p.MaLoaiSanPham = catSneaker.MaLoaiSanPham;
                else if (pLower.Contains("dây lưng") || pLower.Contains("thắt lưng")) p.MaLoaiSanPham = catDayLung.MaLoaiSanPham;
                else if (pLower.Contains("ví")) p.MaLoaiSanPham = catViDa.MaLoaiSanPham;
                else if (pLower.Contains("lót") || pLower.Contains("boxer")) p.MaLoaiSanPham = catDoLot.MaLoaiSanPham;
                else if (pLower.Contains("tất") || pLower.Contains("vớ")) p.MaLoaiSanPham = catTat.MaLoaiSanPham;
                else if (pLower.Contains("cà vạt") || pLower.Contains("caravat") || pLower.Contains("nơ")) p.MaLoaiSanPham = catCaVat.MaLoaiSanPham;
            }
            context.SaveChanges();

            // 5.1 Xóa bỏ các danh mục cũ không thuộc cấu trúc mới (Áo Nam, Áo Nữ, Quần Nam, Quần Nữ, Váy & Đầm, Đồ Thể Thao, Áo Khoác)
            var legacyCategories = context.LoaiSanPhams.Where(c => c.TenLoaiSanPham == "Áo Nam" || c.TenLoaiSanPham == "Áo Nữ" || c.TenLoaiSanPham == "Áo Khoác" || c.TenLoaiSanPham == "Quần Nam" || c.TenLoaiSanPham == "Quần Nữ" || c.TenLoaiSanPham == "Váy & Đầm" || c.TenLoaiSanPham == "Đồ Thể Thao").ToList();
            if (legacyCategories.Any())
            {
                foreach (var oldCat in legacyCategories)
                {
                    var oldProds = context.SanPhams.Where(p => p.MaLoaiSanPham == oldCat.MaLoaiSanPham).ToList();
                    foreach (var op in oldProds)
                    {
                        var opLower = op.TenSanPham.ToLower();
                        if (opLower.Contains("jacket") || opLower.Contains("khoác")) op.MaLoaiSanPham = catJacket.MaLoaiSanPham;
                        else if (opLower.Contains("sơ mi")) op.MaLoaiSanPham = catSoMi.MaLoaiSanPham;
                        else if (opLower.Contains("polo")) op.MaLoaiSanPham = catPolo.MaLoaiSanPham;
                        else if (opLower.Contains("jean")) op.MaLoaiSanPham = catJeans.MaLoaiSanPham;
                        else if (opLower.Contains("quần")) op.MaLoaiSanPham = catTay.MaLoaiSanPham;
                        else op.MaLoaiSanPham = catTShirt.MaLoaiSanPham;
                    }
                }
                context.SaveChanges();
                context.LoaiSanPhams.RemoveRange(legacyCategories);
                context.SaveChanges();
            }
            var leafCategories = new (LoaiSanPham Cat, string ProdName, decimal Price, string Img, string Desc, string[] Sizes, string Color)[]
            {
                (catBlazer, "Áo Blazer Nam May Đo Smart Casual", 850000, "/images/news_office_gentleman.png", "Áo khoác Blazer nam cao cấp phom dáng may đo chuẩn mực, ve áo thanh thoát, tôn nét đĩnh đạc và cuốn hút trong các buổi họp hay dạ tiệc.", new[] { "M", "L", "XL" }, "Xanh Navy"),
                (catNi, "Áo Nỉ Nam Chui Đầu Premium Fleece", 420000, "/images/news_smart_active.png", "Áo nỉ chui đầu chất vải nỉ da cá dày dặn, lót bông ấm áp, mềm mịn không xơ xù, mang lại phong cách đường phố năng động.", new[] { "M", "L", "XL" }, "Xám Khói"),
                (catJeans, "Quần Jeans Nam Slim Fit Co Giãn Fashion Store", 550000, "https://images.unsplash.com/photo-1542272604-780c96856592?w=1200&q=80", "Quần Jeans nam phom Slim Fit co giãn 4 chiều mềm mại, wash màu hiện đại tôn dáng nam tính, thoải mái vận động cả ngày.", new[] { "29", "30", "31", "32" }, "Xanh Denim"),
                (catTay, "Quần Tây Nam Công Sở Cạp Chun Ẩn Smart Fit", 490000, "https://images.unsplash.com/photo-1594938298603-c8148c4dae35?w=1200&q=80", "Quần tây công sở thiết kế cạp chun thông minh linh hoạt, chất vải chéo co giãn đứng li, không nhăn nhàu suốt 8 tiếng làm việc.", new[] { "29", "30", "31", "32" }, "Đen"),
                (catKhaki, "Quần Khaki Chino Dáng Regular Co Giãn", 460000, "https://images.unsplash.com/photo-1473966968600-fa801b869a1a?w=1200&q=80", "Quần khaki Chino phom Regular tôn dáng lịch thiệp, dễ dàng kết hợp cùng áo polo hay sơ mi trong mọi hoạt động thường nhật.", new[] { "29", "30", "31", "32" }, "Be Cát"),
                (catShort, "Quần Short Kaki Dạo Phố Thoáng Mát", 320000, "https://images.unsplash.com/photo-1591195853828-11db59a44f6b?w=1200&q=80", "Quần short kaki năng động, độ dài trên gối vừa vặn, chất liệu cotton thoáng khí, đồng hành cùng bạn trong các buổi picnic, dạo phố.", new[] { "M", "L", "XL" }, "Xanh Rêu"),
                (catQuanNi, "Quần Jogger Nỉ Da Cá Urban Streetwear", 390000, "https://images.unsplash.com/photo-1552902865-b72c031ac5ea?w=1200&q=80", "Quần jogger nỉ da cá dày dặn, bo gấu thun chắc chắn, mang đến phong cách thể thao athleisure sành điệu và ấm áp.", new[] { "M", "L", "XL" }, "Xám Tiêu"),
                (catSneaker, "Giày Sneaker Thể Thao Classic Low-Top", 690000, "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=1200&q=80", "Giày sneaker dáng classic màu trắng tối giản, đệm lót êm ái đàn hồi cao, chống trơn trượt, cực kỳ dễ phối cùng mọi set đồ.", new[] { "39", "40", "41", "42" }, "Trắng"),
                (catGiayTay, "Giày Tây Nam Da Bò Oxford Cao Cấp", 950000, "https://images.unsplash.com/photo-1533867617858-e7b97e060509?w=1200&q=80", "Giày Oxford nam chế tác từ da bò thượng hạng, phom mũi thon gọn tôn lên vẻ lịch lãm và quý phái của quý ông thành đạt.", new[] { "39", "40", "41", "42" }, "Nâu Hạt Dẻ"),
                (catGiayLuoi, "Giày Lười Penny Loafer Da Thật Lịch Lãm", 880000, "https://images.unsplash.com/photo-1614252235316-8c857d38b5f4?w=1200&q=80", "Giày lười Penny Loafer phong cách Ý sang trọng, da thật bóng mịn, dễ xỏ chân, tạo cảm giác nhẹ nhàng êm ái khi di chuyển.", new[] { "39", "40", "41", "42" }, "Đen"),
                (catGiayBoots, "Giày Chelsea Boots Da Lộn Sang Trọng", 1050000, "https://images.unsplash.com/photo-1638247025967-b4e38f787b76?w=1200&q=80", "Chelsea boots da lộn cao cấp, thun co giãn 2 bên hông tiện dụng, kiểu dáng bụi bặm nhưng vô cùng thời thượng và nam tính.", new[] { "39", "40", "41", "42" }, "Nâu Đất"),
                (catSandalDep, "Dép Quai Ngang Đúc Nguyên Khối Êm Chân", 250000, "https://images.unsplash.com/photo-1603487742131-4160ec999306?w=1200&q=80", "Dép quai ngang đúc nguyên khối EVA siêu nhẹ, chống thấm nước tuyệt đối, nâng đỡ vòm bàn chân hoàn hảo.", new[] { "39", "40", "41", "42" }, "Đen"),
                (catDayLung, "Thắt Lưng Nam Da Bò Khóa Tự Động Tinh Tế", 380000, "https://images.unsplash.com/photo-1624222247344-550fb60583dc?w=1200&q=80", "Dây nịt nam da bò lớp 1 dập vân sắc nét, đầu khóa kim loại hợp kim titan chống gỉ, cơ chế khóa ray trượt tiện lợi.", new[] { "Freesize" }, "Đen"),
                (catViDa, "Ví Da Bò Nam Đứng Mini Bỏ Túi Cao Cấp", 350000, "https://images.unsplash.com/photo-1627123424574-724758594e93?w=1200&q=80", "Ví da bò nam dáng đứng nhỏ gọn, công nghệ may viền thủ công, ngăn chứa tiền và thẻ ngân hàng bảo mật RFID an toàn.", new[] { "Freesize" }, "Nâu"),
                (catDoLot, "Set 3 Quần Lót Nam Boxer Modal Kháng Khuẩn", 220000, "https://images.unsplash.com/photo-1583496661160-fb5886a0aaaa?w=1200&q=80", "Combo 3 quần lót đùi nam Boxer chất liệu sợi gỗ sồi Modal mát lạnh, cạp thun co giãn không hằn vết, kháng khuẩn tự nhiên.", new[] { "M", "L", "XL" }, "Đen/Xám/Xanh"),
                (catTat, "Combo 5 Đôi Tất Cổ Ngắn Dệt Kim Thoáng Khí", 120000, "https://images.unsplash.com/photo-1582966772680-860e372bb558?w=1200&q=80", "Bộ 5 đôi tất nam dệt kim cotton mềm mịn, đệm gót chống trượt, thấm hút mồ hôi tối ưu cho cả ngày dài mang giày.", new[] { "Freesize" }, "Trắng/Đen/Xám"),
                (catCaVat, "Cà Vạt Lụa Tơ Tằm Dệt Họa Tiết Quý Tộc", 280000, "https://images.unsplash.com/photo-1589756033938-5fed1e082c14?w=1200&q=80", "Cà vạt lụa bản 7cm dệt họa tiết chìm sang trọng, độ rũ tự nhiên, là điểm nhấn quý phái cho bộ veston trong các dịp trang trọng.", new[] { "Freesize" }, "Xanh Đậm")
            };

            foreach (var item in leafCategories)
            {
                if (item.Cat != null && !context.SanPhams.Any(p => p.MaLoaiSanPham == item.Cat.MaLoaiSanPham))
                {
                    var sp = new SanPham
                    {
                        TenSanPham = item.ProdName,
                        MaLoaiSanPham = item.Cat.MaLoaiSanPham,
                        Gia = item.Price,
                        HinhAnh = item.Img,
                        MoTa = item.Desc,
                        GioiTinh = "Nam",
                        TrangThai = "Đang bán",
                        IsNoiBat = true,
                        IsBanChay = true
                    };
                    context.SanPhams.Add(sp);
                    context.SaveChanges();

                    foreach (var sz in item.Sizes)
                    {
                        var bt = new BienTheSanPham
                        {
                            MaSanPham = sp.MaSanPham,
                            KichCo = sz,
                            MauSac = item.Color,
                            Gia = item.Price,
                            HinhAnh = sp.HinhAnh,
                            TrangThai = "Đang bán"
                        };
                        context.BienTheSanPhams.Add(bt);
                        context.SaveChanges();

                        context.KhoHangs.Add(new KhoHang
                        {
                            MaBienThe = bt.MaBienThe,
                            SoLuongTon = 40,
                            NgayCapNhat = DateTime.Now
                        });
                    }
                    context.SaveChanges();
                }
            }
        }

        private static void SeedAccounts(ApplicationDbContext context)
        {
            // 1. Tài khoản Quản trị viên (Admin)
            if (!context.TaiKhoans.Any(t => t.TenDangNhap == "admin"))
            {
                var adminTk = new TaiKhoan
                {
                    TenDangNhap = "admin",
                    MatKhau = "admin123",
                    Email = "admin@fashionstore.vn",
                    SoDienThoai = "0988888888",
                    HoTen = "Quản Trị Viên",
                    VaiTro = "Quản trị viên",
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                };
                context.TaiKhoans.Add(adminTk);
                context.SaveChanges();

                context.NguoiDungs.Add(new NguoiDung
                {
                    MaTaiKhoan = adminTk.MaTaiKhoan,
                    HoTen = "Quản Trị Viên Hệ Thống",
                    GioiTinh = "Nam",
                    NgaySinh = new DateTime(1995, 1, 1),
                    DiaChi = "Tòa nhà Viwaseen, 48 Tố Hữu, Nam Từ Liêm, Hà Nội"
                });
            }

            // 2. Tài khoản Khách hàng mẫu
            if (!context.TaiKhoans.Any(t => t.TenDangNhap == "khachhang"))
            {
                var khTk = new TaiKhoan
                {
                    TenDangNhap = "khachhang",
                    MatKhau = "123456",
                    Email = "khachhang@gmail.com",
                    SoDienThoai = "0912345678",
                    HoTen = "Nguyễn Văn Khách",
                    VaiTro = "Khách hàng",
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                };
                context.TaiKhoans.Add(khTk);
                context.SaveChanges();

                context.NguoiDungs.Add(new NguoiDung
                {
                    MaTaiKhoan = khTk.MaTaiKhoan,
                    HoTen = "Nguyễn Văn Khách",
                    GioiTinh = "Nam",
                    NgaySinh = new DateTime(1998, 5, 20),
                    DiaChi = "Số 123 Đường Cầu Giấy, Quận Cầu Giấy, Hà Nội",
                    ChieuCao = 172,
                    CanNang = 68
                });
            }
        }

        private static void SeedVouchers(ApplicationDbContext context)
        {
            var targetVouchers = new List<KhuyenMai>
            {
                // 1. Mã giảm giá theo phần trăm
                new KhuyenMai 
                { 
                    Code = "CHAOMUNG", 
                    TenChuongTrinh = "Chào mừng khách hàng mới", 
                    LoaiKhuyenMai = "GiamGia", 
                    PhanTramGiam = 10, 
                    GiaTriDonToiThieu = 0, 
                    GiamToiDa = 200000, 
                    NgayBatDau = DateTime.Now.AddDays(-10), 
                    NgayKetThuc = DateTime.Now.AddYears(1), 
                    SoLuong = 1000, 
                    MoTa = "Giảm ngay 10% tối đa 200.000₫ cho tất cả đơn hàng.", 
                    HienThiTrangChu = true, 
                    TrangThai = true 
                },
                new KhuyenMai 
                { 
                    Code = "HE2026", 
                    TenChuongTrinh = "Ưu đãi mùa hè rực rỡ", 
                    LoaiKhuyenMai = "GiamGia", 
                    PhanTramGiam = 20, 
                    GiaTriDonToiThieu = 500000, 
                    GiamToiDa = 300000, 
                    NgayBatDau = DateTime.Now.AddDays(-5), 
                    NgayKetThuc = DateTime.Now.AddMonths(6), 
                    SoLuong = 500, 
                    MoTa = "Giảm 20% cho đơn từ 500.000₫, áp dụng bộ sưu tập hè.", 
                    HienThiTrangChu = true, 
                    TrangThai = true 
                },
                new KhuyenMai 
                { 
                    Code = "VIPFS", 
                    TenChuongTrinh = "Đặc quyền thành viên thân thiết", 
                    LoaiKhuyenMai = "GiamGia", 
                    PhanTramGiam = 15, 
                    GiaTriDonToiThieu = 1000000, 
                    GiamToiDa = 500000, 
                    NgayBatDau = DateTime.Now.AddDays(-30), 
                    NgayKetThuc = DateTime.Now.AddYears(1), 
                    SoLuong = 200, 
                    MoTa = "Giảm thêm 15% cho thành viên cao cấp cho đơn từ 1.000.000₫.", 
                    HienThiTrangChu = true, 
                    TrangThai = true 
                },

                // 2. Chương trình ưu đãi tặng quà độc quyền theo giá trị đơn hàng
                new KhuyenMai 
                { 
                    Code = "TANGVO", 
                    TenChuongTrinh = "Quà tặng: Tất thể thao dệt kim Fashion Store", 
                    LoaiKhuyenMai = "TangQua", 
                    PhanTramGiam = 0, 
                    GiaTriDonToiThieu = 1000000, 
                    QuaTangKem = "Tất thể thao dệt kim kháng khuẩn Fashion Store Premium", 
                    HinhAnhQuaTang = "https://images.unsplash.com/photo-1582966772680-860e372bb558?w=600&q=80", 
                    NgayBatDau = DateTime.Now.AddDays(-10), 
                    NgayKetThuc = DateTime.Now.AddMonths(3), 
                    SoLuong = 300, 
                    MoTa = "Tặng ngay 01 đôi tất thể thao dệt kim kháng khuẩn Fashion Store trị giá 89.000₫ cho đơn hàng từ 1.000.000₫.", 
                    HienThiTrangChu = true, 
                    TrangThai = true 
                },
                new KhuyenMai 
                { 
                    Code = "BINHNUOC", 
                    TenChuongTrinh = "Quà tặng: Bình giữ nhiệt thể thao cao cấp", 
                    LoaiKhuyenMai = "TangQua", 
                    PhanTramGiam = 0, 
                    GiaTriDonToiThieu = 2000000, 
                    QuaTangKem = "Bình giữ nhiệt kim loại thể thao phong cách Matte Black", 
                    HinhAnhQuaTang = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=600&q=80", 
                    NgayBatDau = DateTime.Now.AddDays(-10), 
                    NgayKetThuc = DateTime.Now.AddMonths(3), 
                    SoLuong = 150, 
                    MoTa = "Tặng ngay 01 bình giữ nhiệt thể thao Inox 304 cao cấp trị giá 250.000₫ cho đơn hàng từ 2.000.000₫.", 
                    HienThiTrangChu = true, 
                    TrangThai = true 
                },
                new KhuyenMai 
                { 
                    Code = "TOTEBAG", 
                    TenChuongTrinh = "Quà tặng: Túi Tote Canvas thời trang", 
                    LoaiKhuyenMai = "TangQua", 
                    PhanTramGiam = 0, 
                    GiaTriDonToiThieu = 3000000, 
                    QuaTangKem = "Túi Tote Canvas thời trang phiên bản giới hạn", 
                    HinhAnhQuaTang = "https://images.unsplash.com/photo-1544816155-12df9643f363?w=600&q=80", 
                    NgayBatDau = DateTime.Now.AddDays(-10), 
                    NgayKetThuc = DateTime.Now.AddMonths(3), 
                    SoLuong = 100, 
                    MoTa = "Tặng ngay 01 túi Tote Canvas thời trang sành điệu trị giá 350.000₫ cho đơn từ 3.000.000₫.", 
                    HienThiTrangChu = true, 
                    TrangThai = true 
                },

                // 3. Voucher cứu giỏ hàng và quà tặng giới thiệu
                new KhuyenMai 
                { 
                    Code = "RECOVER10", 
                    TenChuongTrinh = "Voucher Cứu Giỏ Hàng - Giảm 10%", 
                    LoaiKhuyenMai = "GiamGia", 
                    PhanTramGiam = 10, 
                    GiaTriDonToiThieu = 0, 
                    GiamToiDa = 200000, 
                    NgayBatDau = DateTime.Now.AddDays(-10), 
                    NgayKetThuc = DateTime.Now.AddYears(1), 
                    SoLuong = 1000, 
                    MoTa = "Ưu đãi độc quyền kích cầu giỏ hàng: Giảm ngay 10% tối đa 200.000₫ khi hoàn tất thanh toán.", 
                    HienThiTrangChu = false, 
                    TrangThai = true 
                },
                new KhuyenMai 
                { 
                    Code = "BANMOI50K", 
                    TenChuongTrinh = "Quà tặng bạn mới từ mã giới thiệu", 
                    LoaiKhuyenMai = "GiamGia", 
                    PhanTramGiam = 20, 
                    GiaTriDonToiThieu = 200000, 
                    GiamToiDa = 50000, 
                    NgayBatDau = DateTime.Now.AddDays(-10), 
                    NgayKetThuc = DateTime.Now.AddYears(1), 
                    SoLuong = 1000, 
                    MoTa = "Mã giảm 20% tối đa 50.000₫ cho đơn hàng đầu tiên từ 200.000₫ khi đăng ký qua mã giới thiệu.", 
                    HienThiTrangChu = false, 
                    ChiDanhChoKhachMoi = true,
                    TrangThai = true 
                }
            };

            foreach (var v in targetVouchers)
            {
                var existing = context.KhuyenMais.FirstOrDefault(x => x.Code == v.Code);
                if (existing == null)
                {
                    context.KhuyenMais.Add(v);
                }
                else
                {
                    existing.TenChuongTrinh = v.TenChuongTrinh;
                    existing.LoaiKhuyenMai = v.LoaiKhuyenMai;
                    existing.GiaTriDonToiThieu = v.GiaTriDonToiThieu;
                    existing.GiamToiDa = v.GiamToiDa;
                    existing.QuaTangKem = v.QuaTangKem;
                    existing.HinhAnhQuaTang = v.HinhAnhQuaTang;
                    existing.MoTa = v.MoTa;
                    existing.HienThiTrangChu = v.HienThiTrangChu;
                    context.KhuyenMais.Update(existing);
                }
            }
        }

        private static void SeedSystemConfigurations(ApplicationDbContext context)
        {
            var targetConfigs = new List<CauHinhHeThong>
            {
                new CauHinhHeThong { Key = "SiteName", Value = "Fashion Store", Description = "Tên thương hiệu website" },
                new CauHinhHeThong { Key = "Hotline", Value = "1900 8079", Description = "Số điện thoại tổng đài CSKH" },
                new CauHinhHeThong { Key = "Email", Value = "cskh@fashionstore.vn", Description = "Email hỗ trợ khách hàng" },
                new CauHinhHeThong { Key = "Address", Value = "Tầng 17, Tòa nhà Viwaseen, 48 Tố Hữu, Nam Từ Liêm, Hà Nội", Description = "Địa chỉ trụ sở chính" },
                new CauHinhHeThong { Key = "ShippingFee", Value = "30000", Description = "Phí giao hàng mặc định (VNĐ)" },
                new CauHinhHeThong { Key = "FreeShippingThreshold", Value = "500000", Description = "Mức đơn hàng tối thiểu để được miễn phí ship (VNĐ)" },
                new CauHinhHeThong { Key = "Gemini_API_Key", Value = "", Description = "Khóa API Google Gemini" },

                // Cấu hình nội dung trang Giới thiệu (About Us)
                new CauHinhHeThong { Key = "About_Hero_Eyebrow", Value = "Fashion Store — Câu chuyện của chúng tôi", Description = "Tiêu đề phụ trên banner giới thiệu" },
                new CauHinhHeThong { Key = "About_Hero_Title", Value = "Về Chúng Tôi", Description = "Tiêu đề chính banner giới thiệu" },
                new CauHinhHeThong { Key = "About_Hero_Image", Value = "/images/store-interior.jpg.jpg", Description = "Hình ảnh nền banner giới thiệu" },
                new CauHinhHeThong { Key = "About_Stat_Products", Value = "50+", Description = "Chỉ số thống kê: Sản phẩm" },
                new CauHinhHeThong { Key = "About_Stat_Customers", Value = "10.000+", Description = "Chỉ số thống kê: Khách hàng" },
                new CauHinhHeThong { Key = "About_Stat_Years", Value = "5+", Description = "Chỉ số thống kê: Năm kinh nghiệm" },
                new CauHinhHeThong { Key = "About_Stat_Provinces", Value = "63+", Description = "Chỉ số thống kê: Tỉnh thành" },
                new CauHinhHeThong { Key = "About_Mission_Title", Value = "Thời trang Không giới hạn", Description = "Tiêu đề sứ mệnh" },
                new CauHinhHeThong { Key = "About_Mission_P1", Value = "Chúng tôi tin rằng thời trang không chỉ là quần áo — đó là ngôn ngữ của bản sắc. Mỗi sản phẩm được thiết kế với tư duy tối giản nhưng đậm chất cá tính, mang đến sự tự tin cho người mặc trong từng khoảnh khắc.", Description = "Đoạn 1 sứ mệnh" },
                new CauHinhHeThong { Key = "About_Mission_P2", Value = "Từ những bước chạy bộ buổi sáng đến những buổi tối đô thị sôi động — Fashion Store đồng hành cùng bạn với chất lượng vượt trội và phong cách không bao giờ lỗi thời.", Description = "Đoạn 2 sứ mệnh" },
                new CauHinhHeThong { Key = "About_Story_Title", Value = "Sinh ra từ đam mê thời trang", Description = "Tiêu đề câu chuyện thương hiệu" },
                new CauHinhHeThong { Key = "About_Story_P1", Value = "Khởi đầu từ một cửa hàng nhỏ với ước mơ lớn vào năm 2020, Fashion Store đã vươn mình trở thành một trong những thương hiệu thời trang được yêu thích nhất tại Việt Nam.", Description = "Đoạn 1 câu chuyện" },
                new CauHinhHeThong { Key = "About_Story_P2", Value = "Chúng tôi không ngừng tìm kiếm những chất liệu tốt nhất, những thiết kế tinh tế nhất — tất cả vì một mục tiêu duy nhất: mang lại giá trị thực sự cho khách hàng.", Description = "Đoạn 2 câu chuyện" },
                new CauHinhHeThong { Key = "About_Story_Image", Value = "/images/store-interior.jpg.jpg", Description = "Hình ảnh câu chuyện" },
                new CauHinhHeThong { Key = "About_Value1_Title", Value = "Chất lượng tuyệt đối", Description = "Giá trị 1: Tiêu đề" },
                new CauHinhHeThong { Key = "About_Value1_Desc", Value = "Mỗi sản phẩm đều trải qua quy trình kiểm định nghiêm ngặt trước khi đến tay khách hàng. Không có chỗ cho sự thỏa hiệp.", Description = "Giá trị 1: Mô tả" },
                new CauHinhHeThong { Key = "About_Value2_Title", Value = "Thiết kế hiện đại", Description = "Giá trị 2: Tiêu đề" },
                new CauHinhHeThong { Key = "About_Value2_Desc", Value = "Cập nhật liên tục các xu hướng thời trang quốc tế, dung hòa hoàn hảo với phom dáng và khí hậu Việt Nam.", Description = "Giá trị 2: Mô tả" },
                new CauHinhHeThong { Key = "About_Value3_Title", Value = "Tận tâm phục vụ", Description = "Giá trị 3: Tiêu đề" },
                new CauHinhHeThong { Key = "About_Value3_Desc", Value = "Khách hàng là trung tâm của mọi quyết định. Chính sách đổi trả linh hoạt và hỗ trợ tư vấn 24/7.", Description = "Giá trị 3: Mô tả" }
            };

            foreach (var c in targetConfigs)
            {
                var existing = context.CauHinhHeThongs.FirstOrDefault(x => x.Key == c.Key);
                if (existing == null)
                {
                    context.CauHinhHeThongs.Add(c);
                }
            }
        }

        private static void SeedArticles(ApplicationDbContext context)
        {
            if (!context.BaiViets.Any())
            {
                context.BaiViets.AddRange(
                    new BaiViet
                    {
                        TieuDe = "Top 5 Xu Hướng Thời Trang Nam Công Sở Đỉnh Cao Năm 2026",
                        Slug = "top-5-xu-huong-thoi-trang-nam-cong-so-2026",
                        TomTat = "Khám phá cách phối đồ chuẩn quý ông hiện đại với sơ mi sợi tre, quần tây co giãn và áo khoác blazer dáng suông thời thượng.",
                        NoiDung = @"<p>Năm 2026 chứng kiến sự lên ngôi mạnh mẽ của phong cách <strong>Smart Casual</strong> tinh tế và tiện dụng. Không còn sự gò bó của những bộ suit cứng nhắc, các quý ông hiện đại ưu tiên những trang phục có chất liệu thoáng mát, thân thiện với môi trường nhưng vẫn giữ được nét lịch lãm và chuẩn mực.</p>
<h3>1. Sơ mi sợi tre và sợi sen thoáng khí</h3>
<p>Chất liệu vải sinh học với khả năng thấm hút mồ hôi gấp 3 lần cotton thông thường đang là lựa chọn số một cho mùa hè nhiệt đới. Không nhăn, giữ phom cực tốt sau nhiều lần giặt.</p>
<h3>2. Quần tây cạp chun ẩn linh hoạt</h3>
<p>Thiết kế cạp quần co giãn thông minh giúp quý ông thoải mái vận động suốt 8 tiếng làm việc mà vẫn giữ được đường li quần sắc nét.</p>
<h3>3. Tone màu trung tính thời thượng</h3>
<p>Xanh navy, xám khói, be cát và xanh rêu ô liu là những gam màu chủ đạo giúp tạo nên vẻ ngoài đĩnh đạc và dễ phối đồ.</p>",
                        HinhAnh = "/images/news_office_gentleman.png",
                        TacGia = "Nguyễn Hoàng Nam",
                        ChuyenMuc = "Xu hướng thời trang",
                        LuotXem = 1250,
                        NoiBat = true,
                        TrangThai = true,
                        NgayTao = DateTime.Now.AddDays(-15)
                    },
                    new BaiViet
                    {
                        TieuDe = "Bí Quyết Giữ Form Quần Áo Và Bảo Quản Đồ Cao Cấp Luôn Mới",
                        Slug = "bi-quyet-giu-form-quan-ao-va-bao-quan-do-cao-cap",
                        TomTat = "Hướng dẫn chi tiết từ chuyên gia thời trang cách giặt ủi, bảo quản quần áo cao cấp không bị phai màu và giữ nguyên phom dáng ban đầu.",
                        NoiDung = @"<p>Trang phục cao cấp đòi hỏi sự chăm sóc tỉ mỉ để giữ được độ bền đẹp qua nhiều năm sử dụng. Dưới đây là những nguyên tắc vàng bạn không thể bỏ qua:</p>
<h3>1. Phân loại trang phục trước khi giặt</h3>
<p>Tuyệt đối không giặt chung đồ trắng với đồ có màu. Nên giặt các loại vải mỏng như lụa, sợi sen bằng tay hoặc dùng túi giặt chuyên dụng với chế độ giặt nhẹ.</p>
<h3>2. Nhiệt độ nước và chất tẩy</h3>
<p>Nên sử dụng nước lạnh hoặc ấm dưới 30 độ C. Tránh xa các loại thuốc tẩy có nồng độ Clo cao vì sẽ phá vỡ cấu trúc sợi vải.</p>
<h3>3. Phơi và bảo quản đúng cách</h3>
<p>Tránh phơi trực tiếp dưới ánh nắng gay gắt ban trưa. Dùng móc gỗ hoặc móc có đệm vai cho áo vest và sơ mi để tránh bị gãy form vai.</p>",
                        HinhAnh = "/images/news_smart_active.png",
                        TacGia = "Trần Minh Trang",
                        ChuyenMuc = "Mẹo hay thời trang",
                        LuotXem = 890,
                        NoiBat = false,
                        TrangThai = true,
                        NgayTao = DateTime.Now.AddDays(-7)
                    },
                    new BaiViet
                    {
                        TieuDe = "Phong Cách Athleisure: Sự Hòa Quyện Hoàn Hảo Giữa Thể Thao Và Đô Thị",
                        Slug = "phong-cach-athleisure-hoa-quyen-the-thao-va-do-thi",
                        TomTat = "Athleisure không chỉ là trang phục tập luyện — đây là một lối sống năng động, khỏe khoắn và phóng khoáng đang thống trị giới trẻ toàn cầu.",
                        NoiDung = @"<p>Ra đời từ sự giao thoa giữa đồ thể thao (Athletic) và thời gian thư giãn (Leisure), phong cách Athleisure mang đến sự tự do tối đa cho người mặc trong mọi hoàn cảnh: từ phòng gym, văn phòng sáng tạo cho đến buổi hẹn cà phê cuối tuần.</p>
<h3>Tại sao Athleisure lại được yêu thích đến vậy?</h3>
<ul>
<li><strong>Thoải mái tối đa:</strong> Chất liệu co giãn 4 chiều, nhanh khô và thoáng khí.</li>
<li><strong>Thời trang và linh hoạt:</strong> Dễ dàng kết hợp áo polo thể thao với quần jogger hoặc kaki cropped.</li>
<li><strong>Năng động trẻ trung:</strong> Mang lại nguồn năng lượng tích cực cho cả ngày dài.</li>
</ul>",
                        HinhAnh = "https://images.unsplash.com/photo-1483721310020-03333e577078?w=1200&q=80",
                        TacGia = "Lê Quốc Bảo",
                        ChuyenMuc = "Phong cách sống",
                        LuotXem = 1640,
                        NoiBat = true,
                        TrangThai = true,
                        NgayTao = DateTime.Now.AddDays(-2)
                    }
                );
            }
        }

        private static void EnsureSchemaUpToDate(ApplicationDbContext context)
        {
            try
            {
                var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BaiViets')
BEGIN
    CREATE TABLE BaiViets (
        MaBaiViet INT IDENTITY(1,1) PRIMARY KEY,
        TieuDe NVARCHAR(255) NOT NULL,
        Slug NVARCHAR(255) NULL,
        TomTat NVARCHAR(500) NULL,
        NoiDung NVARCHAR(MAX) NOT NULL,
        HinhAnh NVARCHAR(500) NULL,
        TacGia NVARCHAR(100) NULL,
        ChuyenMuc NVARCHAR(100) NULL,
        LuotXem INT NOT NULL DEFAULT 0,
        NoiBat BIT NOT NULL DEFAULT 0,
        TrangThai BIT NOT NULL DEFAULT 1,
        NgayTao DATETIME2 NOT NULL DEFAULT GETDATE(),
        NgayCapNhat DATETIME2 NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'TenChuongTrinh')
    ALTER TABLE KhuyenMais ADD TenChuongTrinh NVARCHAR(150) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'LoaiKhuyenMai')
    ALTER TABLE KhuyenMais ADD LoaiKhuyenMai NVARCHAR(50) NOT NULL DEFAULT 'GiamGia';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'GiaTriDonToiThieu')
    ALTER TABLE KhuyenMais ADD GiaTriDonToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'GiamToiDa')
    ALTER TABLE KhuyenMais ADD GiamToiDa DECIMAL(18,2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'QuaTangKem')
    ALTER TABLE KhuyenMais ADD QuaTangKem NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'HinhAnhQuaTang')
    ALTER TABLE KhuyenMais ADD HinhAnhQuaTang NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'NgayBatDau')
    ALTER TABLE KhuyenMais ADD NgayBatDau DATETIME2 NOT NULL DEFAULT GETDATE();

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'MoTa')
    ALTER TABLE KhuyenMais ADD MoTa NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'HienThiTrangChu')
    ALTER TABLE KhuyenMais ADD HienThiTrangChu BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'QuaTangKem')
    ALTER TABLE DonHangs ADD QuaTangKem NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'ChietKhauVip')
    ALTER TABLE DonHangs ADD ChietKhauVip INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'SoTienGiamVip')
    ALTER TABLE DonHangs ADD SoTienGiamVip DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'PhiGiaoHang')
    ALTER TABLE DonHangs ADD PhiGiaoHang DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'DonViVanChuyen')
    ALTER TABLE DonHangs ADD DonViVanChuyen NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'MaVanDon')
    ALTER TABLE DonHangs ADD MaVanDon NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'MaGiaoDichThanhToan')
    ALTER TABLE DonHangs ADD MaGiaoDichThanhToan NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'TinhThanh')
    ALTER TABLE DonHangs ADD TinhThanh NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'QuanHuyen')
    ALTER TABLE DonHangs ADD QuanHuyen NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'PhuongXa')
    ALTER TABLE DonHangs ADD PhuongXa NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'LichSuVanChuyen')
    ALTER TABLE DonHangs ADD LichSuVanChuyen NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'NhanVienGiaoHang')
    ALTER TABLE DonHangs ADD NhanVienGiaoHang NVARCHAR(150) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'ChiDanhChoKhachMoi')
    ALTER TABLE KhuyenMais ADD ChiDanhChoKhachMoi BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KhuyenMais') AND name = 'MaLoaiSanPhamApDung')
    ALTER TABLE KhuyenMais ADD MaLoaiSanPhamApDung INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NguoiDungs') AND name = 'TongChiTieu')
    ALTER TABLE NguoiDungs ADD TongChiTieu DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NguoiDungs') AND name = 'HangVip')
    ALTER TABLE NguoiDungs ADD HangVip NVARCHAR(50) NULL DEFAULT N'VIP Khởi Đầu';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LoaiSanPhams') AND name = 'MaLoaiCha')
    ALTER TABLE LoaiSanPhams ADD MaLoaiCha INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NguoiDungs') AND name = 'DiemTichLuy')
    ALTER TABLE NguoiDungs ADD DiemTichLuy INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhGias') AND name = 'ChieuCao')
    ALTER TABLE DanhGias ADD ChieuCao FLOAT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhGias') AND name = 'CanNang')
    ALTER TABLE DanhGias ADD CanNang FLOAT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhGias') AND name = 'SizeDaMua')
    ALTER TABLE DanhGias ADD SizeDaMua NVARCHAR(20) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhGias') AND name = 'DoVuaVan')
    ALTER TABLE DanhGias ADD DoVuaVan NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhGias') AND name = 'HinhAnh')
    ALTER TABLE DanhGias ADD HinhAnh NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ThongBaoHetHangs')
BEGIN
    CREATE TABLE ThongBaoHetHangs (
        MaThongBao INT IDENTITY(1,1) PRIMARY KEY,
        MaSanPham INT NOT NULL,
        MaBienThe INT NULL,
        LienHe NVARCHAR(150) NOT NULL,
        Size NVARCHAR(20) NULL,
        MauSac NVARCHAR(50) NULL,
        NgayDangKy DATETIME2 NOT NULL DEFAULT GETDATE(),
        DaThongBao BIT NOT NULL DEFAULT 0,
        NgayThongBao DATETIME2 NULL
    );
END

-- Marketing: Cứu giỏ hàng bị bỏ quên (GioHangs)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GioHangs') AND name = 'NgayCapNhat')
    ALTER TABLE GioHangs ADD NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE();

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GioHangs') AND name = 'DaGuiNhacNho')
    ALTER TABLE GioHangs ADD DaGuiNhacNho BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GioHangs') AND name = 'NgayGuiNhacNho')
    ALTER TABLE GioHangs ADD NgayGuiNhacNho DATETIME NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GioHangs') AND name = 'SoLanNhacNho')
    ALTER TABLE GioHangs ADD SoLanNhacNho INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('GioHangs') AND name = 'MaVoucherCuuGio')
    ALTER TABLE GioHangs ADD MaVoucherCuuGio NVARCHAR(50) NULL;

-- Marketing: Điểm tích lũy & Giới thiệu (NguoiDungs & DonHangs)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NguoiDungs') AND name = 'MaGioiThieu')
    ALTER TABLE NguoiDungs ADD MaGioiThieu NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NguoiDungs') AND name = 'NguoiGioiThieuId')
    ALTER TABLE NguoiDungs ADD NguoiGioiThieuId INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NguoiDungs') AND name = 'SoNguoiDaGioiThieu')
    ALTER TABLE NguoiDungs ADD SoNguoiDaGioiThieu INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'DiemSuDung')
    ALTER TABLE DonHangs ADD DiemSuDung INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DonHangs') AND name = 'SoTienGiamDiem')
    ALTER TABLE DonHangs ADD SoTienGiamDiem DECIMAL(18,2) NOT NULL DEFAULT 0;

-- Bảng Lịch sử giao dịch điểm thưởng (LichSuDiemThuongs)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LichSuDiemThuongs')
BEGIN
    CREATE TABLE LichSuDiemThuongs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiDung INT NOT NULL,
        SoDiem INT NOT NULL,
        LoaiGiaoDich NVARCHAR(50) NOT NULL,
        MoTa NVARCHAR(255) NOT NULL,
        MaDonHang INT NULL,
        NgayGiaoDich DATETIME NOT NULL DEFAULT GETDATE()
    );
END

-- Sản phẩm: Hỗ trợ 4 hình ảnh (HinhAnh2, HinhAnh3, HinhAnh4)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SanPhams') AND name = 'HinhAnh2')
    ALTER TABLE SanPhams ADD HinhAnh2 NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SanPhams') AND name = 'HinhAnh3')
    ALTER TABLE SanPhams ADD HinhAnh3 NVARCHAR(255) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SanPhams') AND name = 'HinhAnh4')
    ALTER TABLE SanPhams ADD HinhAnh4 NVARCHAR(255) NULL;


-- Bảo mật: OAuth Provider (TaiKhoans)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TaiKhoans') AND name = 'OAuthProvider')
    ALTER TABLE TaiKhoans ADD OAuthProvider NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TaiKhoans') AND name = 'OAuthId')
    ALTER TABLE TaiKhoans ADD OAuthId NVARCHAR(100) NULL;

-- Kiểm soát nội bộ: Bảng Nhật ký thao tác hệ thống (NhatKyHeThongs)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NhatKyHeThongs')
BEGIN
    CREATE TABLE NhatKyHeThongs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50) NOT NULL,
        HoTen NVARCHAR(100) NULL,
        VaiTro NVARCHAR(50) NULL,
        HanhDong NVARCHAR(50) NOT NULL,
        MoTa NVARCHAR(500) NOT NULL,
        DoiTuongAnhHuong NVARCHAR(100) NULL,
        GiaTriCu NVARCHAR(1000) NULL,
        GiaTriMoi NVARCHAR(1000) NULL,
        DiaChiIp NVARCHAR(50) NULL,
        UserAgent NVARCHAR(255) NULL,
        MucDo NVARCHAR(20) NOT NULL DEFAULT N'Trung bình',
        ThoiGian DATETIME NOT NULL DEFAULT GETDATE()
    );
END
";
                context.Database.ExecuteSqlRaw(sql);

                // Tự động gán mã giới thiệu cho những khách hàng chưa có mã
                var usersWithoutRef = context.NguoiDungs.Where(u => string.IsNullOrEmpty(u.MaGioiThieu)).ToList();
                foreach (var u in usersWithoutRef)
                {
                    u.MaGioiThieu = "REF-" + u.MaNguoiDung.ToString("D5");
                }
                if (usersWithoutRef.Any())
                {
                    context.SaveChanges();
                }

                // Tự động cấp điểm thưởng trải nghiệm cho tài khoản chưa có điểm (50.000 điểm)
                var usersZeroPoints = context.NguoiDungs.Where(u => u.DiemTichLuy <= 0).ToList();
                foreach (var u in usersZeroPoints)
                {
                    u.DiemTichLuy = 50000;
                    context.LichSuDiemThuongs.Add(new LichSuDiemThuong
                    {
                        MaNguoiDung = u.MaNguoiDung,
                        SoDiem = 50000,
                        LoaiGiaoDich = "ThuongDacBiet",
                        MoTa = "Quà tặng 50.000 điểm thưởng thành viên trải nghiệm",
                        NgayGiaoDich = DateTime.Now
                    });
                }
                if (usersZeroPoints.Any())
                {
                    context.SaveChanges();
                }

                // Khởi tạo một số nhật ký mẫu ban đầu nếu bảng rỗng
                if (!context.NhatKyHeThongs.Any())
                {
                    context.NhatKyHeThongs.AddRange(
                        new NhatKyHeThong
                        {
                            TenDangNhap = "admin",
                            HoTen = "Quản Trị Viên Hệ Thống",
                            VaiTro = "Quản trị viên",
                            HanhDong = "KHOI_DONG_HE_THONG",
                            MoTa = "Hệ thống bảo mật và cơ chế Audit Logs được kích hoạt thành công.",
                            DoiTuongAnhHuong = "HeThong",
                            DiaChiIp = "127.0.0.1",
                            MucDo = "Thấp",
                            ThoiGian = DateTime.Now.AddHours(-5)
                        },
                        new NhatKyHeThong
                        {
                            TenDangNhap = "admin",
                            HoTen = "Quản Trị Viên Hệ Thống",
                            VaiTro = "Quản trị viên",
                            HanhDong = "XUAT_BAO_CAO",
                            MoTa = "Xuất báo cáo doanh thu tài chính và danh sách đơn hàng sang file Excel.",
                            DoiTuongAnhHuong = "BaoCaoDoanhThu",
                            DiaChiIp = "192.168.1.10",
                            MucDo = "Trung bình",
                            ThoiGian = DateTime.Now.AddHours(-2)
                        }
                    );
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EnsureSchemaUpToDate error: " + ex.Message);
            }
        }

        private static void SeedLookbookSets(ApplicationDbContext context)
        {
            if (!context.BoSuuTaps.Any())
            {
                context.BoSuuTaps.Add(new BoSuuTap
                {
                    TenBoSuuTap = "BST Quý Ông Công Sở - Gentleman Look 2026",
                    MoTa = "Phong cách lịch lãm, hiện đại dành cho quý ông thành đạt với những đường may tinh tế và chất liệu vải sợi sen độc quyền.",
                    HinhAnh = "/images/news_office_gentleman.png",
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                });

                context.BoSuuTaps.Add(new BoSuuTap
                {
                    TenBoSuuTap = "BST Năng Động Dạo Phố - Smart Active 2026",
                    MoTa = "Sự kết hợp hoàn hảo giữa nét trẻ trung, năng động và thanh lịch với chất liệu co giãn 4 chiều thoáng mát.",
                    HinhAnh = "/images/news_smart_active.png",
                    TrangThai = "Hoạt động",
                    NgayTao = DateTime.Now
                });
            }
        }

        private static void SeedProducts(ApplicationDbContext context)
        {
            if (context.SanPhams.Any()) return;

            var sp1 = new SanPham
            {
                TenSanPham = "Áo Sơ Mi Nam Linen Thoáng Mát",
                MaLoaiSanPham = 1,
                Gia = 450000,
                HinhAnh = "/images/products/03f8602f-246c-408e-882c-b1d46e7aff3b.jpg",
                MoTa = "Áo sơ mi nam chất liệu sợi linen tự nhiên mềm mại, kháng khuẩn, thấm hút mồ hôi cực tốt. Phù hợp diện công sở hoặc dạo phố thanh lịch.",
                KieuCoAo = "Cổ bẻ",
                ChatLieu = "Linen",
                FormDang = "Slim fit",
                GioiTinh = "Nam",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = true
            };

            var sp2 = new SanPham
            {
                TenSanPham = "Quần Tây Nam Công Sở Smart Fit",
                MaLoaiSanPham = 2,
                Gia = 550000,
                HinhAnh = "/images/products/11d835aa-9981-4575-9a09-0cf6e3836a82.jpg",
                MoTa = "Quần tây nam thiết kế phom dáng hiện đại, chất liệu co giãn nhẹ nhàng mang lại cảm giác thoải mái khi ngồi làm việc cả ngày dài.",
                KieuCoAo = "Không có",
                ChatLieu = "Nano Poly",
                FormDang = "Regular fit",
                GioiTinh = "Nam",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = true
            };

            var sp3 = new SanPham
            {
                TenSanPham = "Áo Polo Nam Phối Bo Cổ Năng Động",
                MaLoaiSanPham = 1,
                Gia = 380000,
                HinhAnh = "/images/products/30ec7ac9-08a4-4e6b-847d-d4da3500c3cf.jpg",
                MoTa = "Áo polo nam chất liệu Cotton Pique dệt mắt chim cao cấp, co giãn 4 chiều thoáng khí, không xù lông và giữ phom chuẩn.",
                KieuCoAo = "Cổ bẻ",
                ChatLieu = "Cotton",
                FormDang = "Slim fit",
                GioiTinh = "Nam",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = true
            };

            var sp4 = new SanPham
            {
                TenSanPham = "Đầm Nữ Dáng Chữ A Thanh Lịch",
                MaLoaiSanPham = 5,
                Gia = 690000,
                HinhAnh = "/images/products/3c27f1d5-5f82-4e17-a6c3-a2e315ea39c1.jpg",
                MoTa = "Váy đầm liền thân dáng chữ A thời thượng, tôn vinh vóc dáng phái đẹp, chất liệu mềm rủ tạo nét thướt tha duyên dáng.",
                KieuCoAo = "Cổ tròn",
                ChatLieu = "Silk",
                FormDang = "Regular fit",
                GioiTinh = "Nữ",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = false
            };

            var sp5 = new SanPham
            {
                TenSanPham = "Áo Khoác Gió Nam Cản Nước 2 Lớp",
                MaLoaiSanPham = 6,
                Gia = 590000,
                HinhAnh = "/images/products/889da659-7c27-4861-b179-6ace42c83a9a.jpg",
                MoTa = "Áo khoác gió cao cấp công nghệ cản gió và chống thấm nước mưa nhẹ, lớp lót trong lưới thoáng khí giữ ấm tối ưu.",
                KieuCoAo = "Cổ dựng",
                ChatLieu = "Polyester",
                FormDang = "Regular fit",
                GioiTinh = "Nam",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = true
            };

            var sp6 = new SanPham
            {
                TenSanPham = "Quần Jean Nam Co Giãn Thoải Mái",
                MaLoaiSanPham = 2,
                Gia = 520000,
                HinhAnh = "/images/products/da6e35c6-fa5d-4c24-ac1e-65549b2fb218.jpg",
                MoTa = "Quần jean nam chất liệu denim cao cấp pha sợi spandex đàn hồi, wash màu tự nhiên trẻ trung và phong cách.",
                KieuCoAo = "Không có",
                ChatLieu = "Denim",
                FormDang = "Slim fit",
                GioiTinh = "Nam",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = true
            };

            var sp7 = new SanPham
            {
                TenSanPham = "Áo Thun Nữ Cotton Compact Siêu Mềm",
                MaLoaiSanPham = 3,
                Gia = 250000,
                HinhAnh = "/images/products/dc37be18-19ba-4298-93ac-3ffac7b89c4c.jpg",
                MoTa = "Áo thun nữ basic chất vải 100% Cotton chải kỹ siêu mềm mại, mịn màng và mát mẻ khi mặc vào mùa hè.",
                KieuCoAo = "Cổ tròn",
                ChatLieu = "Cotton",
                FormDang = "Regular fit",
                GioiTinh = "Nữ",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = false
            };

            var sp8 = new SanPham
            {
                TenSanPham = "Bộ Đồ Thể Thao Nam Co Giãn 4 Chiều",
                MaLoaiSanPham = 7,
                Gia = 490000,
                HinhAnh = "/images/products/ea2a47a5-6289-4011-9a4b-8771e7730a59.jpg",
                MoTa = "Set đồ thể thao nam gồm áo thun và quần đùi chất liệu siêu nhẹ, thấm hút mồ hôi nhanh khô, hoàn hảo cho tập gym và chạy bộ.",
                KieuCoAo = "Cổ tròn",
                ChatLieu = "Spandex",
                FormDang = "Slim fit",
                GioiTinh = "Nam",
                TrangThai = "Đang bán",
                IsNoiBat = true,
                IsBanChay = true
            };

            context.SanPhams.AddRange(sp1, sp2, sp3, sp4, sp5, sp6, sp7, sp8);
            context.SaveChanges();

            // Thêm các biến thể (size, màu) và số lượng tồn kho cho từng sản phẩm
            var products = context.SanPhams.ToList();
            var sizes = new[] { "M", "L", "XL" };
            var colors = new[] { "Trắng", "Đen", "Xanh Navy" };

            foreach (var prod in products)
            {
                foreach (var s in sizes)
                {
                    foreach (var c in colors)
                    {
                        var bt = new BienTheSanPham
                        {
                            MaSanPham = prod.MaSanPham,
                            KichCo = s,
                            MauSac = c,
                            Gia = prod.Gia,
                            HinhAnh = prod.HinhAnh,
                            TrangThai = "Còn hàng"
                        };
                        context.BienTheSanPhams.Add(bt);
                        context.SaveChanges();

                        context.KhoHangs.Add(new KhoHang
                        {
                            MaBienThe = bt.MaBienThe,
                            SoLuongTon = 50,
                            NgayCapNhat = DateTime.Now,
                            GhiChu = "Nhập kho ban đầu"
                        });
                    }
                }
            }
            context.SaveChanges();
        }

        private static void SeedLookbookHotspots(ApplicationDbContext context)
        {
            if (context.DiemGhimBoSuuTaps.Any()) return;

            var bstList = context.BoSuuTaps.ToList();
            var spList = context.SanPhams.ToList();

            if (bstList.Count >= 2 && spList.Count >= 4)
            {
                // BST 1: Quý Ông Công Sở
                context.DiemGhimBoSuuTaps.Add(new DiemGhimBoSuuTap
                {
                    MaBoSuuTap = bstList[0].MaBoSuuTap,
                    MaSanPham = spList[0].MaSanPham,
                    ToaDoTren = 35.5m,
                    ToaDoTrai = 48.0m,
                    TieuDe = spList[0].TenSanPham
                });

                context.DiemGhimBoSuuTaps.Add(new DiemGhimBoSuuTap
                {
                    MaBoSuuTap = bstList[0].MaBoSuuTap,
                    MaSanPham = spList[1].MaSanPham,
                    ToaDoTren = 70.0m,
                    ToaDoTrai = 50.0m,
                    TieuDe = spList[1].TenSanPham
                });

                // BST 2: Năng Động Dạo Phố
                context.DiemGhimBoSuuTaps.Add(new DiemGhimBoSuuTap
                {
                    MaBoSuuTap = bstList[1].MaBoSuuTap,
                    MaSanPham = spList[2].MaSanPham,
                    ToaDoTren = 40.0m,
                    ToaDoTrai = 45.0m,
                    TieuDe = spList[2].TenSanPham
                });

                context.DiemGhimBoSuuTaps.Add(new DiemGhimBoSuuTap
                {
                    MaBoSuuTap = bstList[1].MaBoSuuTap,
                    MaSanPham = spList[5].MaSanPham,
                    ToaDoTren = 72.0m,
                    ToaDoTrai = 46.0m,
                    TieuDe = spList[5].TenSanPham
                });

                context.SaveChanges();
            }
        }

        private static void StandardizeLanguageStatus(ApplicationDbContext context)
        {
            // Chuẩn hóa trạng thái tài khoản
            foreach (var tk in context.TaiKhoans.ToList())
            {
                bool changed = false;
                if (tk.TrangThai == "HoatDong") { tk.TrangThai = "Hoạt động"; changed = true; }
                else if (tk.TrangThai == "BiKhoa") { tk.TrangThai = "Bị khóa"; changed = true; }

                if (tk.VaiTro == "Admin") { tk.VaiTro = "Quản trị viên"; changed = true; }
                else if (tk.VaiTro == "User") { tk.VaiTro = "Nhân viên"; changed = true; }
                else if (tk.VaiTro == "KhachHang") { tk.VaiTro = "Khách hàng"; changed = true; }

                if (changed) context.TaiKhoans.Update(tk);
            }

            // Chuẩn hóa trạng thái sản phẩm
            foreach (var sp in context.SanPhams.ToList())
            {
                bool changed = false;
                if (sp.TrangThai == "DangBan") { sp.TrangThai = "Đang bán"; changed = true; }
                else if (sp.TrangThai == "NgungBan") { sp.TrangThai = "Ngừng bán"; changed = true; }
                if (sp.GioiTinh == "Nu") { sp.GioiTinh = "Nữ"; changed = true; }

                if (changed) context.SanPhams.Update(sp);
            }

            // Chuẩn hóa trạng thái biến thể
            foreach (var bt in context.BienTheSanPhams.ToList())
            {
                bool changed = false;
                if (bt.TrangThai == "ConHang") { bt.TrangThai = "Còn hàng"; changed = true; }
                else if (bt.TrangThai == "HetHang") { bt.TrangThai = "Hết hàng"; changed = true; }

                if (changed) context.BienTheSanPhams.Update(bt);
            }

            // Chuẩn hóa trạng thái đơn hàng
            foreach (var dh in context.DonHangs.ToList())
            {
                bool changed = false;
                if (dh.TrangThaiDonHang == "ChoXacNhan") { dh.TrangThaiDonHang = "Chờ xác nhận"; changed = true; }
                else if (dh.TrangThaiDonHang == "DangGiao") { dh.TrangThaiDonHang = "Đang giao"; changed = true; }
                else if (dh.TrangThaiDonHang == "HoanThanh") { dh.TrangThaiDonHang = "Hoàn thành"; changed = true; }
                else if (dh.TrangThaiDonHang == "DaHuy") { dh.TrangThaiDonHang = "Đã hủy"; changed = true; }

                if (dh.TrangThaiThanhToan == "ChuaThanhToan") { dh.TrangThaiThanhToan = "Chưa thanh toán"; changed = true; }
                else if (dh.TrangThaiThanhToan == "DaThanhToan") { dh.TrangThaiThanhToan = "Đã thanh toán"; changed = true; }

                if (changed) context.DonHangs.Update(dh);
            }
        }
    }
}
