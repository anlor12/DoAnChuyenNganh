using DoAnCoSo.Data;
using DoAnCoSo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    public class ChatImageRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string Base64Image { get; set; } = string.Empty;
    }

    public class GenerateProductAIInfoRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string Base64Image { get; set; } = string.Empty;
    }

    public class VisualSearchRequest
    {
        public string Base64Image { get; set; } = string.Empty;
    }

    /// <summary>
    /// API Controller xử lý các yêu cầu Chatbot AI và Tìm kiếm bằng hình ảnh.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly VtonService _vtonService;
        private readonly ApplicationDbContext _context;

        public AiController(GeminiService geminiService, VtonService vtonService, ApplicationDbContext context)
        {
            _geminiService = geminiService;
            _vtonService = vtonService;
            _context = context;
        }

        /// <summary>
        /// API tiếp nhận cuộc trò chuyện văn bản thông thường (hỗ trợ lịch sử chat).
        /// POST: /api/Ai/Chat
        /// </summary>
        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] List<GeminiMessage> history)
        {
            if (history == null || !history.Any())
            {
                return BadRequest("Lịch sử chat trống.");
            }

            try
            {
                var reply = await _geminiService.ChatAsync(history);
                var userPrompt = history.LastOrDefault(h => h.role == "user")?.parts?.FirstOrDefault()?.text ?? "";
                var recommendedProducts = await GetRecommendedProductsAsync(reply, userPrompt);
                return Ok(new { success = true, reply = reply, products = recommendedProducts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// API tiếp nhận câu hỏi kèm hình ảnh (tư vấn thời trang qua ảnh).
        /// POST: /api/Ai/ChatImage
        /// </summary>
        [HttpPost("ChatImage")]
        public async Task<IActionResult> ChatImage([FromBody] ChatImageRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image))
            {
                return BadRequest("Không nhận được dữ liệu hình ảnh.");
            }

            try
            {
                ParseBase64(request.Base64Image, out string rawBase64, out string mimeType);
                var reply = await _geminiService.ChatWithImageAsync(request.Prompt, rawBase64, mimeType);
                var recommendedProducts = await GetRecommendedProductsAsync(reply, request.Prompt);
                return Ok(new { success = true, reply = reply, products = recommendedProducts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// API Tìm kiếm sản phẩm bằng hình ảnh (Visual Search).
        /// POST: /api/Ai/VisualSearch
        /// </summary>
        [HttpPost("VisualSearch")]
        public async Task<IActionResult> VisualSearch([FromBody] VisualSearchRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image))
            {
                return BadRequest("Không nhận được dữ liệu hình ảnh.");
            }

            try
            {
                ParseBase64(request.Base64Image, out string rawBase64, out string mimeType);
                var keywords = await _geminiService.ExtractKeywordsFromImageAsync(rawBase64, mimeType);

                if (keywords == null || !keywords.Any())
                {
                    return Ok(new { success = false, message = "AI không thể nhận dạng được đặc trưng trang phục trong bức ảnh này. Vui lòng chọn ảnh khác rõ nét hơn." });
                }

                var allProducts = await _context.SanPhams
                    .Where(s => s.TrangThai == "Đang bán")
                    .Include(s => s.LoaiSanPham)
                    .ToListAsync();

                // Tính điểm trùng khớp từ khóa của từng sản phẩm
                var matchedProducts = allProducts
                    .Select(p => new
                    {
                        Product = p,
                        Score = keywords.Count(kw =>
                            p.TenSanPham.Contains(kw, System.StringComparison.OrdinalIgnoreCase) ||
                            (p.MoTa != null && p.MoTa.Contains(kw, System.StringComparison.OrdinalIgnoreCase)) ||
                            (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.Contains(kw, System.StringComparison.OrdinalIgnoreCase))
                        )
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Select(x => new
                    {
                        maSanPham = x.Product.MaSanPham,
                        tenSanPham = x.Product.TenSanPham,
                        gia = x.Product.Gia,
                        hinhAnh = string.IsNullOrEmpty(x.Product.HinhAnh) ? "/images/no-image.png" : x.Product.HinhAnh,
                        loaiSanPham = x.Product.LoaiSanPham?.TenLoaiSanPham ?? "Thời trang",
                        score = x.Score
                    })
                    .Take(8)
                    .ToList();

                if (!matchedProducts.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        keywords = keywords,
                        products = new List<object>(),
                        message = $"Tìm thấy các từ khóa: {string.Join(", ", keywords)} nhưng không có sản phẩm nào tại shop trùng khớp."
                    });
                }

                return Ok(new
                {
                    success = true,
                    keywords = keywords,
                    products = matchedProducts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// API Gợi ý trang phục theo vóc dáng (dựa trên chiều cao, cân nặng, giới tính trong profile).
        /// POST: /api/Ai/BodyRecommendation
        /// </summary>
        [HttpPost("BodyRecommendation")]
        public async Task<IActionResult> BodyRecommendation()
        {
            // Lấy tên đăng nhập từ Session
            var username = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để sử dụng tính năng này." });
            }

            try
            {
                // Tìm thông tin khách hàng trong CSDL
                var user = await _context.NguoiDungs
                    .Include(u => u.TaiKhoan)
                    .FirstOrDefaultAsync(u => u.TaiKhoan != null && u.TaiKhoan.TenDangNhap == username);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thông tin tài khoản." });
                }

                // Kiểm tra xem đã có chiều cao và cân nặng chưa
                if (!user.ChieuCao.HasValue || !user.CanNang.HasValue)
                {
                    return Ok(new { success = false, needProfileUpdate = true, message = "Vui lòng cập nhật chiều cao và cân nặng trong hồ sơ để AI có thể gợi ý chính xác." });
                }

                // Lấy danh sách sản phẩm đang kinh doanh tại shop
                var activeProducts = await _context.SanPhams
                    .Where(s => s.TrangThai == "Đang bán")
                    .Include(s => s.LoaiSanPham)
                    .ToListAsync();

                // Gọi Gemini Service phân tích và chọn sản phẩm
                var recommendation = await _geminiService.GetBodyRecommendationAsync(
                    user.ChieuCao.Value,
                    user.CanNang.Value,
                    user.GioiTinh ?? "Unisex",
                    activeProducts
                );

                // Ghép nối danh sách sản phẩm gợi ý
                var recommendedProducts = new List<object>();
                foreach (var rec in recommendation.recommendations)
                {
                    var prod = activeProducts.FirstOrDefault(p => p.MaSanPham == rec.productId);
                    if (prod != null)
                    {
                        recommendedProducts.Add(new
                        {
                            maSanPham = prod.MaSanPham,
                            tenSanPham = prod.TenSanPham,
                            gia = prod.Gia,
                            hinhAnh = string.IsNullOrEmpty(prod.HinhAnh) ? "/images/no-image.png" : prod.HinhAnh,
                            loaiSanPham = prod.LoaiSanPham?.TenLoaiSanPham ?? "Thời trang",
                            reason = rec.reason
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    advice = recommendation.advice,
                    recommendedSize = recommendation.recommended_size,
                    height = user.ChieuCao.Value,
                    weight = user.CanNang.Value,
                    gender = user.GioiTinh ?? "Chưa xác định",
                    products = recommendedProducts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi xử lý gợi ý AI: " + ex.Message });
            }
        }

        /// <summary>
        /// API Tự động tạo phân loại và mô tả sản phẩm bằng AI.
        /// POST: /api/Ai/GenerateProductAIInfo
        /// </summary>
        [HttpPost("GenerateProductAIInfo")]
        public async Task<IActionResult> GenerateProductAIInfo([FromBody] GenerateProductAIInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image) || string.IsNullOrEmpty(request.ProductName))
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp cả tên sản phẩm và hình ảnh để AI phân tích." });
            }

            try
            {
                ParseBase64(request.Base64Image, out string rawBase64, out string mimeType);
                var aiInfo = await _geminiService.GenerateProductInfoAsync(request.ProductName, rawBase64, mimeType);
                return Ok(new { success = true, data = aiInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi kết nối AI: " + ex.Message });
            }
        }

        /// <summary>
        /// API Quét toàn bộ đánh giá chưa được phân tích cảm xúc (Xử lý hàng loạt / Batch).
        /// POST: /api/Ai/AnalyzeAllReviews
        /// </summary>
        [HttpPost("AnalyzeAllReviews")]
        public async Task<IActionResult> AnalyzeAllReviews()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Quản trị viên") return Unauthorized(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });

            try
            {
                // Lấy tối đa 10 đánh giá mỗi lần quét để tránh timeout và xử lý siêu nhanh
                var unanalyzedReviews = await _context.DanhGias
                    .Where(r => string.IsNullOrEmpty(r.Sentiment))
                    .Take(10)
                    .ToListAsync();

                if (!unanalyzedReviews.Any())
                {
                    return Ok(new { success = true, message = "Tất cả các đánh giá đã được phân tích. Không có đánh giá mới nào." });
                }

                // Xử lý các bình luận trống
                var emptyReviews = unanalyzedReviews.Where(r => string.IsNullOrWhiteSpace(r.NoiDung)).ToList();
                foreach (var review in emptyReviews)
                {
                    review.Sentiment = review.DiemDanhGia >= 4 ? "Tích cực" : (review.DiemDanhGia == 3 ? "Trung lập" : "Tiêu cực");
                    review.AiTags = "Không có nội dung";
                }

                // Đóng gói các bình luận có nội dung để xử lý hàng loạt
                var textReviews = unanalyzedReviews.Where(r => !string.IsNullOrWhiteSpace(r.NoiDung)).ToList();
                if (textReviews.Any())
                {
                    var batchInput = textReviews.Select(r => new BatchReviewInput { Id = r.MaDanhGia, Text = r.NoiDung ?? "" }).ToList();
                    var batchResult = await _geminiService.AnalyzeReviewBatchAsync(batchInput);

                    foreach (var result in batchResult)
                    {
                        var reviewToUpdate = textReviews.FirstOrDefault(r => r.MaDanhGia == result.Id);
                        if (reviewToUpdate != null)
                        {
                            reviewToUpdate.Sentiment = result.Sentiment;
                            reviewToUpdate.AiTags = result.Tags;
                        }
                    }

                    // Đề phòng AI bỏ sót bình luận nào đó (fallback)
                    foreach (var review in textReviews.Where(r => string.IsNullOrEmpty(r.Sentiment)))
                    {
                        review.Sentiment = review.DiemDanhGia >= 4 ? "Tích cực" : (review.DiemDanhGia == 3 ? "Trung lập" : "Tiêu cực");
                        review.AiTags = "AI bỏ sót";
                    }
                }

                await _context.SaveChangesAsync();

                // Kiểm tra xem còn lại bao nhiêu chưa quét
                int totalRemaining = await _context.DanhGias.CountAsync(r => string.IsNullOrEmpty(r.Sentiment));
                
                string msg = $"Đã dùng AI phân tích thành công đợt {unanalyzedReviews.Count} đánh giá.";
                if (totalRemaining > 0)
                {
                    msg += $"\nCòn {totalRemaining} đánh giá chưa quét, hãy bấm lại để tiếp tục.";
                }

                return Ok(new { success = true, message = msg });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// API Phân tích rủi ro khách hàng (Bom hàng) dựa trên ID khách hàng.
        /// GET: /api/Ai/AnalyzeFraudRisk/{userId}
        /// </summary>
        [HttpGet("AnalyzeFraudRisk/{userId}")]
        public async Task<IActionResult> AnalyzeFraudRisk(int userId)
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Quản trị viên") return Unauthorized(new { success = false, message = "Không có quyền" });

            try
            {
                var orders = await _context.DonHangs.Where(d => d.MaNguoiDung == userId).ToListAsync();
                if (!orders.Any())
                {
                    return Ok(new { success = true, riskLevel = "Thấp", reason = "Khách hàng mới, chưa có lịch sử mua hàng." });
                }

                int totalOrders = orders.Count;
                int canceledOrders = orders.Count(o => o.TrangThaiDonHang == "Đã hủy");
                int completedOrders = orders.Count(o => o.TrangThaiDonHang == "Hoàn thành");
                decimal totalSpent = orders.Where(o => o.TrangThaiThanhToan == "Đã thanh toán" || o.TrangThaiThanhToan == "DaThanhToan").Sum(o => o.TongTien);

                var result = await _geminiService.AnalyzeFraudRiskAsync(totalOrders, canceledOrders, completedOrders, totalSpent);

                return Ok(new { 
                    success = true, 
                    riskLevel = result.RiskLevel, 
                    reason = result.Reason 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// API Lấy danh sách sản phẩm theo danh sách ID (để hiển thị trong Chatbot).
        /// GET: /api/Ai/GetProductsByIds
        /// </summary>
        [HttpGet("GetProductsByIds")]
        public async Task<IActionResult> GetProductsByIds([FromQuery] string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return BadRequest("Danh sách ID trống.");
            }

            try
            {
                var idList = ids.Split(',')
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (!idList.Any())
                {
                    return Ok(new { success = true, products = new List<object>() });
                }

                var products = await _context.SanPhams
                    .Where(p => idList.Contains(p.MaSanPham) && p.TrangThai == "Đang bán")
                    .Include(p => p.LoaiSanPham)
                    .Select(p => new
                    {
                        maSanPham = p.MaSanPham,
                        tenSanPham = p.TenSanPham,
                        gia = p.Gia,
                        hinhAnh = string.IsNullOrEmpty(p.HinhAnh) ? "/images/no-image.png" : p.HinhAnh,
                        loaiSanPham = p.LoaiSanPham != null ? p.LoaiSanPham.TenLoaiSanPham : "Thời trang"
                    })
                    .ToListAsync();

                return Ok(new { success = true, products = products });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Tìm kiếm và đề xuất sản phẩm dựa trên tag gợi ý hoặc từ khóa trong cuộc trò chuyện.
        /// </summary>
        private async Task<List<object>> GetRecommendedProductsAsync(string aiReply, string userPrompt)
        {
            var recommendedProducts = new List<object>();

            try
            {
                var uLower = (userPrompt ?? "").Trim().ToLower();

                // 0. BẢO VỆ TUYỆT ĐỐI: Khi khách hàng khiếu nại, phản ánh hàng lỗi hoặc yêu cầu đổi hàng / đổi size
                // TUYỆT ĐỐI KHÔNG hiển thị thẻ sản phẩm để tránh gây khó chịu và phản cảm cho khách hàng.
                bool isComplaintOrExchange = uLower.Contains("đổi") || uLower.Contains("khiếu nại") || uLower.Contains("trả hàng") ||
                    uLower.Contains("chật") || uLower.Contains("rộng") || uLower.Contains("không vừa") || uLower.Contains("bị lỗi") ||
                    uLower.Contains("hàng lỗi") || uLower.Contains("rách") || uLower.Contains("hỏng");

                if (isComplaintOrExchange)
                {
                    return recommendedProducts;
                }

                // Phân loại chi tiết và ưu tiên các tiểu loại trang phục
                bool isAskingKhoac = uLower.Contains("khoác") || uLower.Contains("jacket") || uLower.Contains("blazer") || uLower.Contains("phao") || uLower.Contains("gió");
                bool isAskingJean = (uLower.Contains("jean") || uLower.Contains("bò")) && !isAskingKhoac;
                bool isAskingQuanTay = uLower.Contains("tây") || uLower.Contains("âu") || uLower.Contains("sartorial") || uLower.Contains("công sở");
                bool isAskingKaki = uLower.Contains("kaki") || uLower.Contains("khaki") || uLower.Contains("chino");
                bool isAskingShort = uLower.Contains("short") || uLower.Contains("đùi");
                bool isAskingQuan = isAskingJean || isAskingQuanTay || isAskingKaki || isAskingShort || uLower.Contains("quần") || uLower.Contains("jogger");

                bool isAskingPolo = uLower.Contains("polo");
                bool isAskingSoMi = uLower.Contains("sơ mi");
                bool isAskingThun = uLower.Contains("thun") || uLower.Contains("t-shirt") || uLower.Contains("phông");
                bool isAskingHoodie = uLower.Contains("hoodie") || uLower.Contains("nỉ") || uLower.Contains("sweater");
                bool isAskingAo = isAskingKhoac || isAskingPolo || isAskingSoMi || isAskingThun || isAskingHoodie || uLower.Contains("áo");

                // 1. Kiểm tra tag [RECOMMEND: 1, 2, ...] trong câu trả lời của AI
                var match = System.Text.RegularExpressions.Regex.Match(aiReply, @"\[RECOMMEND:\s*([0-9\s,\-_]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var idsStr = match.Groups[1].Value;
                    var idList = idsStr.Split(',')
                        .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                        .Where(id => id > 0)
                        .ToList();

                    if (idList.Any())
                    {
                        var products = await _context.SanPhams
                            .Where(p => idList.Contains(p.MaSanPham) && p.TrangThai == "Đang bán")
                            .Include(p => p.LoaiSanPham)
                            .Select(p => new
                            {
                                maSanPham = p.MaSanPham,
                                tenSanPham = p.TenSanPham,
                                gia = p.Gia,
                                hinhAnh = string.IsNullOrEmpty(p.HinhAnh) ? "/images/no-image.png" : p.HinhAnh,
                                loaiSanPham = p.LoaiSanPham != null ? p.LoaiSanPham.TenLoaiSanPham : "Thời trang"
                            })
                            .ToListAsync();

                        if (isAskingQuan)
                        {
                            // LỌC CHẶT CHẼ: Loại bỏ triệt để bất kỳ sản phẩm áo/jacket/khoác nào khi khách đang hỏi mua quần
                            var onlyQuan = products.Where(p => 
                                (p.tenSanPham.ToLower().Contains("quần") || p.loaiSanPham.ToLower().Contains("quần")) &&
                                !p.tenSanPham.ToLower().Contains("áo") &&
                                !p.loaiSanPham.ToLower().Contains("áo") &&
                                !p.loaiSanPham.ToLower().Contains("jacket") &&
                                !p.loaiSanPham.ToLower().Contains("khoác")
                            ).ToList();

                            if (onlyQuan.Any())
                            {
                                return onlyQuan.Cast<object>().ToList();
                            }
                            // Nếu tag bị conflict, không trả về sản phẩm sai mà chuyển xuống Fallback chính xác
                        }
                        else if (isAskingAo)
                        {
                            var onlyAo = products.Where(p => 
                                (p.tenSanPham.ToLower().Contains("áo") || p.loaiSanPham.ToLower().Contains("áo") || p.tenSanPham.ToLower().Contains("polo") || p.tenSanPham.ToLower().Contains("sơ mi") || p.tenSanPham.ToLower().Contains("thun")) &&
                                !p.tenSanPham.ToLower().Contains("quần") &&
                                !p.loaiSanPham.ToLower().Contains("quần")
                            ).ToList();

                            if (onlyAo.Any())
                            {
                                return onlyAo.Cast<object>().ToList();
                            }
                        }
                        else if (products.Any())
                        {
                            return products.Cast<object>().ToList();
                        }
                    }
                }

                // 2. Fallback Keyword Search: Tìm kiếm chính xác tuyệt đối theo nhóm sản phẩm khách hỏi
                var query = _context.SanPhams
                    .Where(p => p.TrangThai == "Đang bán")
                    .Include(p => p.LoaiSanPham)
                    .AsQueryable();

                bool matched = false;
                if (isAskingJean)
                {
                    query = query.Where(p => 
                        (p.TenSanPham.ToLower().Contains("jean") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("jean"))) &&
                        !p.TenSanPham.ToLower().Contains("áo")
                    );
                    matched = true;
                }
                else if (isAskingQuanTay)
                {
                    query = query.Where(p => 
                        (p.TenSanPham.ToLower().Contains("tây") || p.TenSanPham.ToLower().Contains("sartorial") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("tây"))) &&
                        !p.TenSanPham.ToLower().Contains("áo")
                    );
                    matched = true;
                }
                else if (isAskingKaki)
                {
                    query = query.Where(p => 
                        (p.TenSanPham.ToLower().Contains("kaki") || p.TenSanPham.ToLower().Contains("khaki") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("khaki"))) &&
                        !p.TenSanPham.ToLower().Contains("áo")
                    );
                    matched = true;
                }
                else if (isAskingShort)
                {
                    query = query.Where(p => 
                        (p.TenSanPham.ToLower().Contains("short") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("short"))) &&
                        !p.TenSanPham.ToLower().Contains("áo")
                    );
                    matched = true;
                }
                else if (isAskingQuan)
                {
                    query = query.Where(p => 
                        (p.TenSanPham.ToLower().Contains("quần") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("quần"))) &&
                        !p.TenSanPham.ToLower().Contains("áo") &&
                        (p.LoaiSanPham == null || (!p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("áo") && !p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("jacket") && !p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("khoác")))
                    );
                    matched = true;
                }
                else if (isAskingKhoac)
                {
                    query = query.Where(p => 
                        p.TenSanPham.ToLower().Contains("khoác") || p.TenSanPham.ToLower().Contains("jacket") || p.TenSanPham.ToLower().Contains("blazer") ||
                        (p.LoaiSanPham != null && (p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("jacket") || p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("blazer") || p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("khoác")))
                    );
                    matched = true;
                }
                else if (isAskingPolo)
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("polo"));
                    matched = true;
                }
                else if (isAskingSoMi)
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("sơ mi"));
                    matched = true;
                }
                else if (isAskingThun)
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("thun") || p.TenSanPham.ToLower().Contains("t-shirt"));
                    matched = true;
                }
                else if (isAskingHoodie)
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("hoodie") || p.TenSanPham.ToLower().Contains("nỉ") || p.TenSanPham.ToLower().Contains("sweater"));
                    matched = true;
                }
                else if (isAskingAo)
                {
                    query = query.Where(p => (p.TenSanPham.ToLower().Contains("áo") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("áo"))) && !p.TenSanPham.ToLower().Contains("quần"));
                    matched = true;
                }
                else if (uLower.Contains("váy") || uLower.Contains("đầm"))
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("váy") || p.TenSanPham.ToLower().Contains("đầm") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("váy")));
                    matched = true;
                }
                else if (uLower.Contains("giày") || uLower.Contains("sneaker") || uLower.Contains("dép"))
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("giày") || p.TenSanPham.ToLower().Contains("sneaker") || p.TenSanPham.ToLower().Contains("dép") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("giày")));
                    matched = true;
                }
                else if (uLower.Contains("mũ") || uLower.Contains("nón") || uLower.Contains("ví") || uLower.Contains("phụ kiện"))
                {
                    query = query.Where(p => p.TenSanPham.ToLower().Contains("mũ") || p.TenSanPham.ToLower().Contains("nón") || p.TenSanPham.ToLower().Contains("ví") || (p.LoaiSanPham != null && p.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("phụ kiện")));
                    matched = true;
                }

                if (matched)
                {
                    var results = await query.Take(4)
                        .Select(p => new
                        {
                            maSanPham = p.MaSanPham,
                            tenSanPham = p.TenSanPham,
                            gia = p.Gia,
                            hinhAnh = string.IsNullOrEmpty(p.HinhAnh) ? "/images/no-image.png" : p.HinhAnh,
                            loaiSanPham = p.LoaiSanPham != null ? p.LoaiSanPham.TenLoaiSanPham : "Thời trang"
                        })
                        .ToListAsync();

                    return results.Cast<object>().ToList();
                }
            }
            catch (Exception)
            {
                // Suppress database exceptions to ensure chat returns text reply even if search fails
            }

            return recommendedProducts;
        }

        /// <summary>
        /// Hàm helper giải mã chuỗi Base64 và trích xuất MimeType.
        /// </summary>
        private void ParseBase64(string base64Input, out string rawBase64, out string mimeType)
        {
            mimeType = "image/jpeg";
            rawBase64 = base64Input;

            if (base64Input.Contains(","))
            {
                var parts = base64Input.Split(',');
                var meta = parts[0];
                rawBase64 = parts[1];

                var mimeIndex = meta.IndexOf("data:");
                var base64Index = meta.IndexOf(";base64");
                if (mimeIndex >= 0 && base64Index > mimeIndex)
                {
                    mimeType = meta.Substring(mimeIndex + 5, base64Index - (mimeIndex + 5));
                }
            }
        }

        /// <summary>
        /// Khởi tạo phiên thử đồ thời gian thực (Live V-TON session).
        /// GET: /api/Ai/VtonSession/{productId}
        /// </summary>
        [HttpGet("VtonSession/{productId}")]
        public async Task<IActionResult> GetVtonSession(int productId)
        {
            var session = await _vtonService.CreateLiveSessionAsync(productId);
            return Ok(session);
        }

        /// <summary>
        /// Xử lý thử đồ bằng hình ảnh (Static V-TON / Chụp từ webcam).
        /// POST: /api/Ai/VtonStatic
        /// </summary>
        [HttpPost("VtonStatic")]
        public async Task<IActionResult> ProcessStaticTryOn([FromBody] VtonTryOnRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var result = await _vtonService.ProcessStaticTryOnAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách sản phẩm gợi ý để đổi trang phục ngay trong phòng thử đồ ảo.
        /// GET: /api/Ai/QuickWardrobe
        /// </summary>
        [HttpGet("QuickWardrobe")]
        public async Task<IActionResult> GetQuickWardrobe()
        {
            try
            {
                var list = await _context.SanPhams
                    .AsNoTracking()
                    .Where(p => !string.IsNullOrEmpty(p.HinhAnh))
                    .OrderByDescending(p => p.MaSanPham)
                    .Take(12)
                    .Select(p => new
                    {
                        id = p.MaSanPham,
                        name = p.TenSanPham,
                        price = p.Gia.ToString("N0") + " đ",
                        imageUrl = p.HinhAnh ?? "/images/no-image.png",
                        category = "upper_body"
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nạp tủ đồ: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy Decart API Key từ cấu hình hệ thống (Admin) để phục vụ luồng WebRTC thời gian thực.
        /// GET: /api/Ai/DecartKey
        /// </summary>
        [HttpGet("DecartKey")]
        public async Task<IActionResult> GetDecartKey()
        {
            var config = await _context.CauHinhHeThongs.FirstOrDefaultAsync(x => x.Key == "Decart_API_Key");
            var key = config?.Value?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                key = "dct_test_EoXNbcuxWjeBFMaspizrmUwHXhbNDrOwOizJUoXLbdesVsagespbmGdvgcpxygqs";
            }
            return Ok(new { key });
        }
    }
}
