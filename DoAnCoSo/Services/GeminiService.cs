#pragma warning disable IDE1006 // Naming rule for Gemini external JSON contracts
#pragma warning disable RCS1037 // Trailing whitespace

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Services
{
    public class GeminiMessage
    {
        public string role { get; set; } = string.Empty; // "user" or "model"
        public List<GeminiPart> parts { get; set; } = new List<GeminiPart>();
    }

    public class GeminiPart
    {
        public string? text { get; set; }
        public GeminiInlineData? inlineData { get; set; }
    }

    public class GeminiInlineData
    {
        public string mimeType { get; set; } = string.Empty;
        public string data { get; set; } = string.Empty; // Chuỗi Base64
    }

    public class GeminiGenerationConfig
    {
        public int? maxOutputTokens { get; set; }
        public double? temperature { get; set; }
    }

    public class GeminiPayload
    {
        public List<GeminiMessage> contents { get; set; } = new List<GeminiMessage>();
        public GeminiSystemInstruction? systemInstruction { get; set; }
        public GeminiGenerationConfig? generationConfig { get; set; }
    }

    public class GeminiSystemInstruction
    {
        public List<GeminiPart> parts { get; set; } = new List<GeminiPart>();
    }

    public class BodyRecommendationResponse
    {
        public string advice { get; set; } = string.Empty;
        public string recommended_size { get; set; } = string.Empty;
        public List<ProductRecommendationDto> recommendations { get; set; } = new List<ProductRecommendationDto>();
    }

    public class ProductRecommendationDto
    {
        public int productId { get; set; }
        public string reason { get; set; } = string.Empty;
    }

    public class ProductAiInfoResponse
    {
        public string? KieuCoAo { get; set; }
        public string? ChatLieu { get; set; }
        public string? FormDang { get; set; }
        public string? GioiTinh { get; set; }
        public string? MoTa { get; set; }
        public bool IsFallback { get; set; } = false;
    }

    public class InventoryForecastResponse
    {
        public List<ForecastItem> HotSelling { get; set; } = new List<ForecastItem>();
        public List<ForecastItem> SlowMoving { get; set; } = new List<ForecastItem>();
        public string GeneralAdvice { get; set; } = string.Empty;
    }

    public class ForecastItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; 
    }

    public class ReviewSentimentResponse
    {
        public string Sentiment { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }

    public class BatchReviewInput
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class BatchReviewResult
    {
        public int Id { get; set; }
        public string Sentiment { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
    }

    public class FraudRiskResponse
    {
        public string RiskLevel { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dịch vụ kết nối và gọi Google Gemini API (gemini-1.5-flash).
    /// </summary>
    public class GeminiService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions JsonIgnoreNullOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions JsonCaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiService(ApplicationDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Lấy Gemini API Key từ cơ sở dữ liệu (tự động làm sạch dấu ngoặc kép hoặc khoảng trắng).
        /// </summary>
        public async Task<string> GetGeminiApiKeyAsync()
        {
            var config = await _context.CauHinhHeThongs
                .FirstOrDefaultAsync(x => x.Key == "Gemini_API_Key");
            var key = config?.Value?.Trim() ?? string.Empty;
            if (key.StartsWith('"') && key.EndsWith('"') && key.Length > 2)
            {
                key = key[1..^1].Trim();
            }

            if (string.IsNullOrEmpty(key))
            {
                key = Environment.GetEnvironmentVariable("GEMINI_API_KEY")?.Trim() ?? string.Empty;
                if (key.StartsWith('"') && key.EndsWith('"') && key.Length > 2)
                {
                    key = key[1..^1].Trim();
                }
            }

            return key;
        }

        private static readonly string[] CandidateModels =
        [
            "gemini-1.5-flash",
            "gemini-2.0-flash",
            "gemini-1.5-flash-8b",
            "gemini-1.5-pro"
        ];

        /// <summary>
        /// Gọi Gemini API với cơ chế tự động xoay vòng qua các Model dự phòng khi gặp lỗi 503 (ServiceUnavailable) hoặc 429.
        /// </summary>
        private async Task<string> CallGeminiApiAsync(object payload)
        {
            var apiKey = await GetGeminiApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Gemini API Key chưa được cấu hình. Vui lòng thiết lập khóa trong trang Cấu hình hệ thống (Admin).");
            }

            var jsonPayload = JsonSerializer.Serialize(payload, JsonIgnoreNullOptions);

            string lastError = "Không thể kết nối đến máy chủ Gemini API.";

            // Lần lượt thử qua các model dự phòng: gemini-1.5-flash -> gemini-2.0-flash -> gemini-1.5-flash-8b -> gemini-1.5-pro
            foreach (var modelName in CandidateModels)
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync(url, content, cts.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResponse = await response.Content.ReadAsStringAsync(cts.Token);
                            using var doc = JsonDocument.Parse(jsonResponse);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("candidates", out var candidates) && 
                                candidates.GetArrayLength() > 0 &&
                                candidates[0].TryGetProperty("content", out var candContent) &&
                                candContent.TryGetProperty("parts", out var parts) &&
                                parts.GetArrayLength() > 0 &&
                                parts[0].TryGetProperty("text", out var textElem))
                            {
                                return textElem.GetString() ?? string.Empty;
                            }
                        }

                        var statusCode = (int)response.StatusCode;
                        var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                        lastError = $"Model {modelName} phản hồi mã lỗi {statusCode}: {errorBody}";

                        // Nếu máy chủ Google báo quá tải 503 (ServiceUnavailable) hoặc 429 (TooManyRequests):
                        if (statusCode == 503 || statusCode == 429)
                        {
                            await Task.Delay(500 * (attempt + 1), cts.Token);
                            if (attempt == 1) break; // Chuyển sang model dự phòng tiếp theo
                            continue;
                        }

                        // Nếu mã lỗi khác (404/400), chuyển ngay sang model dự phòng khác
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastError = $"Lỗi kết nối tới {modelName}: {ex.Message}";
                        break;
                    }
                }
            }

            throw new Exception($"Gemini API tạm thời không khả dụng (503): {lastError}");
        }

        /// <summary>
        /// Chat văn bản thông thường (hỗ trợ lịch sử chat và cơ chế Smart Fallback nếu máy chủ AI quá tải).
        /// </summary>
        public async Task<string> ChatAsync(List<GeminiMessage> history)
        {
            var lastUserPrompt = history?.LastOrDefault(h => h.role == "user")?.parts?.FirstOrDefault()?.text ?? "";
            var pLower = lastUserPrompt.Trim().ToLower();

            // Lấy danh sách sản phẩm thông minh: ưu tiên các danh mục người dùng đang hỏi + sản phẩm nổi bật
            var query = _context.SanPhams
                .Where(s => s.TrangThai == "Đang bán")
                .Include(s => s.LoaiSanPham)
                .AsQueryable();

            if (pLower.Contains("quần") || pLower.Contains("jean") || pLower.Contains("kaki") || pLower.Contains("khaki") || pLower.Contains("tây") || pLower.Contains("short"))
            {
                query = query.OrderByDescending(s => s.TenSanPham.Contains("Quần") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.Contains("Quần")));
            }
            else if (pLower.Contains("áo") || pLower.Contains("polo") || pLower.Contains("sơ mi") || pLower.Contains("thun") || pLower.Contains("khoác"))
            {
                query = query.OrderByDescending(s => s.TenSanPham.Contains("Áo") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.Contains("Áo")));
            }
            else
            {
                query = query.OrderByDescending(s => s.IsBanChay).ThenByDescending(s => s.IsNoiBat);
            }

            var activeProducts = await query
                .Select(s => new {
                    s.MaSanPham,
                    s.TenSanPham,
                    LoaiSanPham = s.LoaiSanPham != null ? s.LoaiSanPham.TenLoaiSanPham : "Thời trang",
                    s.GioiTinh,
                    s.Gia,
                    s.ChatLieu,
                    s.FormDang
                })
                .Take(50)
                .ToListAsync();

            try
            {
                var sbProducts = new StringBuilder();
                foreach (var p in activeProducts)
                {
                    var form = string.IsNullOrWhiteSpace(p.FormDang) ? "Tiêu chuẩn" : p.FormDang;
                    var material = string.IsNullOrWhiteSpace(p.ChatLieu) ? "Chất lượng cao" : p.ChatLieu;
                    sbProducts.AppendLine($"- ID:{p.MaSanPham} | {p.TenSanPham} | Giá: {p.Gia:N0}₫ | Loại: {p.LoaiSanPham} | Form: {form} | Chất liệu: {material} | Dành cho: {p.GioiTinh}");
                }

                var fullSystemPrompt = FashionStoreKnowledgeBase.BuildFullSystemPrompt(sbProducts.ToString());

                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = fullSystemPrompt }
                    }
                };

                // Đảm bảo lịch sử chat gửi tới Gemini bắt đầu bằng lượt thoại của user (tránh lỗi 400 nếu bắt đầu bằng model)
                var validContents = (history ?? new List<GeminiMessage>())
                    .SkipWhile(m => m.role != "user")
                    .ToList();

                if (!validContents.Any())
                {
                    validContents.Add(new GeminiMessage
                    {
                        role = "user",
                        parts = new List<GeminiPart> { new GeminiPart { text = lastUserPrompt } }
                    });
                }

                var payload = new GeminiPayload
                {
                    contents = validContents,
                    systemInstruction = systemInstruction,
                    generationConfig = new GeminiGenerationConfig
                    {
                        maxOutputTokens = 1536,
                        temperature = 0.7
                    }
                };

                return await CallGeminiApiAsync(payload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chat fallback: {ex.Message}");
                return await BuildFallbackChatResponseAsync(lastUserPrompt, isImage: false);
            }
        }

        /// <summary>
        /// Phân tích ảnh và tư vấn thời trang (Multimodal Chat).
        /// </summary>
        public async Task<string> ChatWithImageAsync(string prompt, string base64Image, string mimeType)
        {
            var activeProducts = await _context.SanPhams
                .Where(s => s.TrangThai == "Đang bán")
                .Include(s => s.LoaiSanPham)
                .Select(s => new {
                    s.MaSanPham,
                    s.TenSanPham,
                    LoaiSanPham = s.LoaiSanPham != null ? s.LoaiSanPham.TenLoaiSanPham : "Thời trang",
                    s.GioiTinh,
                    s.Gia,
                    s.ChatLieu,
                    s.FormDang
                })
                .Take(50)
                .ToListAsync();

            try
            {
                var sbProducts = new StringBuilder();
                foreach (var p in activeProducts)
                {
                    var form = string.IsNullOrWhiteSpace(p.FormDang) ? "Tiêu chuẩn" : p.FormDang;
                    var material = string.IsNullOrWhiteSpace(p.ChatLieu) ? "Chất lượng cao" : p.ChatLieu;
                    sbProducts.AppendLine($"- ID:{p.MaSanPham} | {p.TenSanPham} | Giá: {p.Gia:N0}₫ | Loại: {p.LoaiSanPham} | Form: {form} | Chất liệu: {material} | Dành cho: {p.GioiTinh}");
                }

                var fullSystemPrompt = FashionStoreKnowledgeBase.BuildFullSystemPrompt(sbProducts.ToString());

                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = fullSystemPrompt }
                    }
                };

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = prompt },
                        new GeminiPart
                        {
                            inlineData = new GeminiInlineData
                            {
                                mimeType = mimeType,
                                data = base64Image
                            }
                        }
                    }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                payload.generationConfig = new GeminiGenerationConfig
                {
                    maxOutputTokens = 1536,
                    temperature = 0.7
                };

                return await CallGeminiApiAsync(payload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatWithImage fallback: {ex.Message}");
                return await BuildFallbackChatResponseAsync(prompt, isImage: true);
            }
        }

        /// <summary>
        /// Phân tích hình ảnh sản phẩm để trích xuất các từ khóa đặc trưng (Visual Search).
        /// </summary>
        public async Task<List<string>> ExtractKeywordsFromImageAsync(string base64Image, string mimeType)
        {
            try
            {
                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Hãy phân tích hình ảnh sản phẩm thời trang này và trích xuất ra các từ khóa mô tả đặc điểm nổi bật nhất (ví dụ: loại sản phẩm, màu sắc, kiểu dáng, hoa văn). Bắt buộc chỉ trả về một mảng JSON các chuỗi (string), không giải thích hay thêm bớt gì. Ví dụ: [\"áo thun\", \"màu đen\", \"tay ngắn\"]." },
                        new GeminiPart
                        {
                            inlineData = new GeminiInlineData
                            {
                                mimeType = mimeType,
                                data = base64Image
                            }
                        }
                    }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage }
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                // Làm sạch chuỗi JSON trả về từ AI
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var keywords = JsonSerializer.Deserialize<List<string>>(cleanedJson, JsonCaseInsensitiveOptions);
                if (keywords != null && keywords.Count > 0)
                {
                    return keywords;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Extract keywords error: {ex.Message}");
            }

            return new List<string> { "áo", "quần", "thời trang", "polo", "sơ mi" };
        }

        /// <summary>
        /// Phân tích số đo chiều cao, cân nặng, giới tính và gợi ý trang phục phù hợp kèm sản phẩm từ shop.
        /// </summary>
        public async Task<BodyRecommendationResponse> GetBodyRecommendationAsync(double height, double weight, string gender, List<SanPham> products)
        {
            try
            {
                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Bạn là chuyên gia tư vấn thời trang AI chuyên nghiệp của Fashion Store. Hãy phân tích chiều cao, cân nặng, giới tính của người dùng để khuyên chọn phong cách thời trang tôn dáng, che khuyết điểm và gợi ý kích cỡ phù hợp. Bạn cũng cần lựa chọn ra tối đa 4 sản phẩm phù hợp nhất trong danh sách sản phẩm của shop." }
                    }
                };

                // Tạo danh sách mô tả ngắn gọn các sản phẩm trong shop để gửi cho AI
                var sbProducts = new StringBuilder();
                sbProducts.AppendLine("Danh sách sản phẩm đang bán tại shop:");
                foreach (var p in products.Take(30)) // Giới hạn 30 sản phẩm để tối ưu hóa tokens
                {
                    sbProducts.AppendLine($"- ID: {p.MaSanPham}, Tên: {p.TenSanPham}, Loại: {p.LoaiSanPham?.TenLoaiSanPham ?? "Thời trang"}, Giới tính phù hợp: {p.GioiTinh}, Mô tả: {p.MoTa ?? "Không có mô tả"}");
                }

                var prompt = $@"Khách hàng của shop có các thông số cơ thể sau:
- Giới tính: {gender}
- Chiều cao: {height} cm
- Cân nặng: {weight} kg

{sbProducts.ToString()}

Yêu cầu thực hiện:
1. Đưa ra lời khuyên thời trang ngắn gọn, súc tích (khoảng 3-4 câu) chỉ cách phối đồ, màu sắc, kiểu dáng phù hợp với vóc dáng của họ (trả về trong thuộc tính 'advice').
2. Đề xuất kích cỡ (size) trang phục phù hợp nhất (ví dụ: S, M, L, XL, XXL) dựa trên chiều cao và cân nặng (trả về trong thuộc tính 'recommended_size').
3. Dựa trên giới tính và đặc điểm cơ thể của họ, chọn ra tối đa 4 sản phẩm phù hợp nhất từ danh sách sản phẩm ở trên. Với mỗi sản phẩm được chọn, hãy viết một câu ngắn gọn giải thích lý do tại sao sản phẩm đó lại phù hợp với vóc dáng của họ (trả về danh sách trong thuộc tính 'recommendations' chứa 'productId' và 'reason').

BẮT BUỘC TRẢ VỀ DƯỚI DẠNG JSON CHUẨN, KHÔNG CÓ BẤT KỲ VĂN BẢN NÀO KHÁC HOẶC KHỐI CODE MARKDOWN:
{{
  ""advice"": ""Lời khuyên thời trang chi tiết của bạn tại đây..."",
  ""recommended_size"": ""M"",
  ""recommendations"": [
    {{
      ""productId"": 1,
      ""reason"": ""Lý do sản phẩm này phù hợp...""
    }}
  ]
}}";

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart> { new GeminiPart { text = prompt } }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                // Làm sạch chuỗi JSON
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var response = JsonSerializer.Deserialize<BodyRecommendationResponse>(cleanedJson, JsonCaseInsensitiveOptions);
                if (response != null && !string.IsNullOrEmpty(response.advice))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Body recommendation error: {ex.Message}");
            }

            return BuildFallbackBodyRecommendation(height, weight, gender, products);
        }

        /// <summary>
        /// Phân tích hình ảnh sản phẩm để tự động tạo mô tả và các tag phân loại (Auto-Tagging).
        /// </summary>
        public async Task<ProductAiInfoResponse> GenerateProductInfoAsync(string productName, string base64Image, string mimeType)
        {
            try
            {
                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Bạn là chuyên gia content SEO về thời trang. Nhiệm vụ của bạn là phân tích hình ảnh và tên sản phẩm để trích xuất các thuộc tính phân loại và viết một đoạn mô tả bán hàng thật hấp dẫn." }
                    }
                };

                var prompt = $@"Dựa trên hình ảnh sản phẩm và tên sản phẩm ""{productName}"", hãy thực hiện:
1. Xác định Kiểu Cổ Áo (KieuCoAo): ví dụ Cổ bẻ, Cổ tròn, Cổ tim, Cổ trụ v.v. Nếu không có cổ hoặc là quần thì ghi rỗng """".
2. Xác định Chất Liệu vải (ChatLieu): ví dụ Cotton, Denim, Kaki, Linen, Polyester, v.v.
3. Xác định Form Dáng (FormDang): ví dụ Slim fit, Regular fit, Oversize, Suông.
4. Xác định Giới Tính phù hợp (GioiTinh): Nam, Nữ, hoặc Unisex.
5. Viết một đoạn Mô Tả chi tiết (MoTa): Viết thật hấp dẫn để thuyết phục khách hàng mua. Độ dài khoảng 4-6 câu, văn phong chuyên nghiệp. Trả về dưới dạng VĂN BẢN THUẦN (Plain Text), TUYỆT ĐỐI KHÔNG DÙNG THẺ HTML (như <p>, <ul>, <li>, <strong>). Dùng dấu gạch đầu dòng (-) và xuống dòng để liệt kê các ý chính. Chú ý tối ưu SEO.

BẮT BUỘC TRẢ VỀ DƯỚI DẠNG JSON CHUẨN (KHÔNG CÓ MARKDOWN BỌC NGOÀI, CHỈ JSON, KHÔNG THÊM BACKTICKS, CHỈ NGOẶC NHỌN):
{{
  ""KieuCoAo"": ""Cổ tròn"",
  ""ChatLieu"": ""Cotton"",
  ""FormDang"": ""Oversize"",
  ""GioiTinh"": ""Unisex"",
  ""MoTa"": ""Sản phẩm mang đến phong cách trẻ trung và hiện đại...\\n\\n- Chất liệu: Cotton cao cấp thoáng mát.\\n- Phom dáng: Oversize thời thượng, tôn dáng.\\n- Phong cách: Dễ dàng phối đồ đi làm hoặc đi chơi.\\n\\nCam kết hình ảnh thật 100%, đổi size linh hoạt trong 7 ngày.""
}}";

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = prompt },
                        new GeminiPart
                        {
                            inlineData = new GeminiInlineData
                            {
                                mimeType = mimeType,
                                data = base64Image
                            }
                        }
                    }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var response = JsonSerializer.Deserialize<ProductAiInfoResponse>(cleanedJson, JsonCaseInsensitiveOptions);
                if (response != null && !string.IsNullOrEmpty(response.MoTa))
                {
                    response.MoTa = CleanHtmlToPlainText(response.MoTa);
                    response.IsFallback = false;
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Generate product info error: {ex.Message}");
            }

            return BuildFallbackProductInfo(productName);
        }

        /// <summary>
        /// Phân tích báo cáo tồn kho và tốc độ bán hàng để đưa ra dự báo kinh doanh.
        /// </summary>
        public async Task<InventoryForecastResponse> AnalyzeInventoryAndDemandAsync(string reportData)
        {
            try
            {
                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Bạn là Giám đốc kinh doanh (CSO) và Chuyên gia chuỗi cung ứng thời trang bằng AI. Nhiệm vụ của bạn là phân tích báo cáo doanh số và tồn kho được cung cấp để phát hiện các sản phẩm có rủi ro 'cháy hàng' và các mặt hàng 'tồn kho lâu', từ đó đề xuất hành động cụ thể." }
                    }
                };

                var prompt = $@"Dưới đây là số liệu bán hàng và tồn kho hiện tại (đã tổng hợp):
{reportData}

Yêu cầu thực hiện:
1. Xác định tối đa 3 sản phẩm đang bán rất chạy nhưng tồn kho thấp, có nguy cơ hết hàng (HotSelling).
2. Xác định tối đa 3 sản phẩm bán rất chậm, tồn kho cao (SlowMoving).
3. Đưa ra lời khuyên tổng quan chung về tình hình kinh doanh (GeneralAdvice).

BẮT BUỘC TRẢ VỀ JSON CHUẨN (KHÔNG BỌC TRONG MARKDOWN, CHỈ JSON):
{{
  ""HotSelling"": [
    {{ ""ProductId"": 1, ""ProductName"": ""Tên SP"", ""Reason"": ""Bán 100 cái tháng qua nhưng chỉ còn 10 cái."", ""Action"": ""Nhập thêm gấp 200 cái"" }}
  ],
  ""SlowMoving"": [
    {{ ""ProductId"": 2, ""ProductName"": ""Tên SP"", ""Reason"": ""Tồn kho 500 cái nhưng không bán được cái nào."", ""Action"": ""Giảm giá 30% xả hàng"" }}
  ],
  ""GeneralAdvice"": ""Lời khuyên chung về chiến lược nhập hàng...""
}}";

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart> { new GeminiPart { text = prompt } }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var response = JsonSerializer.Deserialize<InventoryForecastResponse>(cleanedJson, JsonCaseInsensitiveOptions);
                if (response != null)
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Inventory forecast error: {ex.Message}");
            }

            return new InventoryForecastResponse
            {
                GeneralAdvice = "Hệ thống khuyến nghị tiếp tục theo dõi biến động tồn kho thực tế và chuẩn bị kế hoạch bổ sung các nhóm hàng bán chạy.",
                HotSelling = new List<ForecastItem>(),
                SlowMoving = new List<ForecastItem>()
            };
        }

        /// <summary>
        /// Phân tích cảm xúc và trích xuất từ khóa từ một đánh giá sản phẩm.
        /// </summary>
        public async Task<ReviewSentimentResponse> AnalyzeReviewSentimentAsync(string reviewText)
        {
            try
            {
                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Bạn là Chuyên gia phân tích trải nghiệm khách hàng (Customer Insights) bằng AI. Nhiệm vụ của bạn là đọc các đánh giá của khách hàng về sản phẩm thời trang, sau đó phân loại cảm xúc và trích xuất các từ khóa chính." }
                    }
                };

                var prompt = $@"Hãy phân tích đánh giá sau đây của khách hàng:
""{reviewText}""

Yêu cầu thực hiện:
1. Phân loại cảm xúc (Sentiment): Chỉ được chọn 1 trong 3 giá trị: ""Tích cực"", ""Tiêu cực"", hoặc ""Trung lập"". Nếu đánh giá có số sao/nội dung tốt thì là Tích cực.
2. Trích xuất từ khóa (Tags): Rút ra 2-4 từ/cụm từ ngắn gọn nhất phản ánh nội dung chính của đánh giá (ví dụ: ""vải mát, form đẹp"", ""giao chậm"", ""chật""). Phân cách bằng dấu phẩy.

BẮT BUỘC TRẢ VỀ JSON CHUẨN (KHÔNG BỌC TRONG MARKDOWN, CHỈ JSON):
{{
  ""Sentiment"": ""Tích cực"",
  ""Tags"": ""vải đẹp, form chuẩn""
}}";

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart> { new GeminiPart { text = prompt } }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var response = JsonSerializer.Deserialize<ReviewSentimentResponse>(cleanedJson, JsonCaseInsensitiveOptions);
                if (response != null && !string.IsNullOrEmpty(response.Sentiment))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Review sentiment error: {ex.Message}");
            }

            return BuildFallbackReviewSentiment(reviewText);
        }

        /// <summary>
        /// Phân tích cảm xúc và trích xuất từ khóa cho một danh sách đánh giá (Batch Processing) để tối ưu thời gian.
        /// </summary>
        public async Task<List<BatchReviewResult>> AnalyzeReviewBatchAsync(List<BatchReviewInput> reviews)
        {
            if (reviews == null || reviews.Count == 0) return new List<BatchReviewResult>();

            try
            {
                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Bạn là Chuyên gia phân tích trải nghiệm khách hàng (Customer Insights) bằng AI. Nhiệm vụ của bạn là đọc một danh sách các đánh giá của khách hàng, sau đó phân loại cảm xúc và trích xuất từ khóa cho TỪNG đánh giá." }
                    }
                };

                var reviewsText = string.Join("\n", reviews.Select(r => $"ID: {r.Id} | Nội dung: {r.Text}"));

                var prompt = $@"Hãy phân tích danh sách đánh giá sau:
{reviewsText}

Yêu cầu thực hiện:
1. Phân loại cảm xúc (Sentiment): ""Tích cực"", ""Tiêu cực"", hoặc ""Trung lập"".
2. Trích xuất từ khóa (Tags): 2-4 từ/cụm từ ngắn gọn (ví dụ ""vải tốt"", ""giao chậm"").

BẮT BUỘC TRẢ VỀ CHÍNH XÁC ĐỊNH DẠNG JSON LÀ MỘT MẢNG OBJECT (KHÔNG BỌC TRONG MARKDOWN, CHỈ MẢNG JSON):
[
  {{ ""Id"": 1, ""Sentiment"": ""Tích cực"", ""Tags"": ""vải đẹp"" }},
  {{ ""Id"": 2, ""Sentiment"": ""Tiêu cực"", ""Tags"": ""giao chậm"" }}
]";

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart> { new GeminiPart { text = prompt } }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var response = JsonSerializer.Deserialize<List<BatchReviewResult>>(cleanedJson, JsonCaseInsensitiveOptions);
                if (response != null && response.Count > 0)
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Review batch error: {ex.Message}");
            }

            return reviews.Select(r =>
            {
                var s = BuildFallbackReviewSentiment(r.Text);
                return new BatchReviewResult { Id = r.Id, Sentiment = s.Sentiment, Tags = s.Tags };
            }).ToList();
        }

        /// <summary>
        /// Phân tích lịch sử mua hàng để cảnh báo rủi ro "Bom hàng".
        /// </summary>
        public async Task<FraudRiskResponse> AnalyzeFraudRiskAsync(int totalOrders, int canceledOrders, int completedOrders, decimal totalSpent)
        {
            try
            {
                var systemInstruction = new GeminiSystemInstruction
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart { text = "Bạn là AI chống gian lận thương mại điện tử (Fraud Detection). Dựa vào lịch sử mua hàng, hãy đánh giá tỷ lệ rủi ro bom hàng của tài khoản." }
                    }
                };

                var prompt = $@"Lịch sử mua hàng của khách hàng này:
- Tổng số đơn đã đặt: {totalOrders}
- Số đơn đã hủy: {canceledOrders}
- Số đơn giao thành công: {completedOrders}
- Tổng số tiền đã thanh toán: {totalSpent} VNĐ

Yêu cầu:
1. Đánh giá Mức độ rủi ro (RiskLevel): Chỉ chọn 1 trong 3 giá trị ""Cao"", ""Trung bình"", hoặc ""Thấp"".
2. Lý do (Reason): Viết 1 câu thật ngắn gọn giải thích.

BẮT BUỘC TRẢ VỀ JSON:
{{
  ""RiskLevel"": ""Cao"",
  ""Reason"": ""Tỷ lệ hủy đơn trên 80%, rủi ro bom hàng rất cao.""
}}";

                var userMessage = new GeminiMessage
                {
                    role = "user",
                    parts = new List<GeminiPart> { new GeminiPart { text = prompt } }
                };

                var payload = new GeminiPayload
                {
                    contents = new List<GeminiMessage> { userMessage },
                    systemInstruction = systemInstruction
                };

                var rawResponse = await CallGeminiApiAsync(payload);
                
                var cleanedJson = rawResponse.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var response = JsonSerializer.Deserialize<FraudRiskResponse>(cleanedJson, JsonCaseInsensitiveOptions);
                if (response != null && !string.IsNullOrEmpty(response.RiskLevel))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fraud risk error: {ex.Message}");
            }

            return BuildFallbackFraudRisk(totalOrders, canceledOrders, completedOrders, totalSpent);
        }

        // ==========================================
        // SMART FALLBACK LOGIC (TỰ ĐỘNG XỬ LÝ NỘI BỘ KHI GOOGLE GEMINI BỊ 503 HOẶC QUÁ TẢI)
        // ==========================================

        private async Task<string> BuildFallbackChatResponseAsync(string userPrompt, bool isImage = false)
        {
            var pLower = (userPrompt ?? "").Trim().ToLower();

            // 1. Phân tích hình ảnh
            if (isImage)
            {
                var sampleProducts = await _context.SanPhams
                    .Where(s => s.TrangThai == "Đang bán")
                    .OrderByDescending(s => s.IsBanChay)
                    .Take(3)
                    .ToListAsync();
                var imgTags = sampleProducts.Count > 0 ? $"\n[RECOMMEND: {string.Join(", ", sampleProducts.Select(m => m.MaSanPham))}]" : "";
                return "Dạ Em chào Anh/Chị ạ! Em đã nhận được hình ảnh trang phục của mình. Phong cách này rất hiện đại và thời thượng! Dựa trên kiểu dáng và tone màu trong ảnh, Fashion Store xin gợi ý một số sản phẩm có thiết kế tương đồng nhất tại shop để Anh/Chị tham khảo nhé:" + imgTags;
            }

            // 2. Kịch bản khiếu nại / Đổi size / Đổi hàng
            if (pLower.Contains("đổi size") || pLower.Contains("đổi hàng") || pLower.Contains("đổi trả") || pLower.Contains("khiếu nại") || 
                (pLower.Contains("đổi") && pLower.Contains("size")) || pLower.Contains("chật quá") || pLower.Contains("rộng quá") || pLower.Contains("không vừa"))
            {
                return FashionStoreKnowledgeBase.GetExchangePolicyFallback();
            }

            // 3. Kịch bản xử lý hết hàng (AK01 hết size L đen hoặc tương tự)
            if (pLower.Contains("ak01") || (pLower.Contains("hết") && (pLower.Contains("size") || pLower.Contains("hàng") || pLower.Contains("màu"))))
            {
                return FashionStoreKnowledgeBase.GetOutOfStockFallback("\n[RECOMMEND: 5]");
            }

            // 4. Kịch bản ngân sách dưới 500k
            if (pLower.Contains("500k") || pLower.Contains("500.000") || pLower.Contains("500 000") || 
                (pLower.Contains("ngân sách") && pLower.Contains("500")) || (pLower.Contains("dưới") && pLower.Contains("500")))
            {
                return FashionStoreKnowledgeBase.GetBudgetUnder500kFallback("\n[RECOMMEND: 8, 7, 3]");
            }

            // 5. Kịch bản tư vấn size theo thể hình (1m70, 85kg hoặc tương tự)
            if (pLower.Contains("size") || pLower.Contains("nặng") || pLower.Contains("cao") || pLower.Contains("kg") || 
                pLower.Contains("cm") || pLower.Contains("m7") || pLower.Contains("m6") || pLower.Contains("m8") || 
                pLower.Contains("bụng") || pLower.Contains("vai") || pLower.Contains("bắp tay"))
            {
                return FashionStoreKnowledgeBase.GetSizeConsultationFallback(pLower, "\n[RECOMMEND: 3, 2]");
            }

            // 6. Phân loại chi tiết và chính xác theo nhóm trang phục khách hỏi
            bool isAskingKhoac = pLower.Contains("khoác") || pLower.Contains("jacket") || pLower.Contains("blazer") || pLower.Contains("phao") || pLower.Contains("gió");
            bool isAskingJean = (pLower.Contains("jean") || pLower.Contains("bò")) && !isAskingKhoac;
            bool isAskingQuanTay = pLower.Contains("tây") || pLower.Contains("âu") || pLower.Contains("sartorial") || pLower.Contains("công sở");
            bool isAskingKaki = pLower.Contains("kaki") || pLower.Contains("khaki") || pLower.Contains("chino");
            bool isAskingShort = pLower.Contains("short") || pLower.Contains("đùi");
            bool isQuan = isAskingJean || isAskingQuanTay || isAskingKaki || isAskingShort || pLower.Contains("quần") || pLower.Contains("jogger");

            bool isAskingPolo = pLower.Contains("polo");
            bool isAskingSoMi = pLower.Contains("sơ mi");
            bool isAskingThun = pLower.Contains("thun") || pLower.Contains("t-shirt") || pLower.Contains("phông");
            bool isAskingHoodie = pLower.Contains("hoodie") || pLower.Contains("nỉ") || pLower.Contains("sweater");
            bool isAo = isAskingKhoac || isAskingPolo || isAskingSoMi || isAskingThun || isAskingHoodie || pLower.Contains("áo");

            bool isVay = pLower.Contains("váy") || pLower.Contains("đầm") || pLower.Contains("chân váy");
            bool isGiay = pLower.Contains("giày") || pLower.Contains("dép") || pLower.Contains("sneaker");
            bool isPhuKien = pLower.Contains("phụ kiện") || pLower.Contains("ví") || pLower.Contains("dây lưng") || pLower.Contains("thắt lưng") || pLower.Contains("mũ") || pLower.Contains("nón") || pLower.Contains("cà vạt");

            var query = _context.SanPhams
                .Where(s => s.TrangThai == "Đang bán")
                .Include(s => s.LoaiSanPham)
                .AsQueryable();

            if (pLower.Contains("nam"))
            {
                query = query.Where(s => s.GioiTinh == "Nam" || s.GioiTinh == "Unisex");
            }
            else if (pLower.Contains("nữ"))
            {
                query = query.Where(s => s.GioiTinh == "Nữ" || s.GioiTinh == "Unisex");
            }

            string introText;

            if (isAskingJean)
            {
                query = query.Where(s => 
                    (s.TenSanPham.ToLower().Contains("jean") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("jean"))) &&
                    !s.TenSanPham.ToLower().Contains("áo")
                );
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm kiếm quần jean của mình, Em xin gợi ý những mẫu quần jean nam ống suông, slim-fit wash màu thời thượng và bền đẹp nhất tại Fashion Store:";
            }
            else if (isAskingQuanTay)
            {
                query = query.Where(s => 
                    (s.TenSanPham.ToLower().Contains("tây") || s.TenSanPham.ToLower().Contains("sartorial") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("tây"))) &&
                    !s.TenSanPham.ToLower().Contains("áo")
                );
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm quần tây của mình, Em xin gợi ý những mẫu quần tây nam cạp cao, xếp ly giữa đứng phom chuẩn mực và sang trọng nhất tại Fashion Store:";
            }
            else if (isAskingKaki)
            {
                query = query.Where(s => 
                    (s.TenSanPham.ToLower().Contains("kaki") || s.TenSanPham.ToLower().Contains("khaki") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("khaki"))) &&
                    !s.TenSanPham.ToLower().Contains("áo")
                );
                introText = "Dạ Em chào Anh/Chị ạ! Fashion Store xin gửi Anh/Chị các mẫu quần kaki co giãn nhẹ, dễ phối đồ và cực kỳ thoải mái:";
            }
            else if (isAskingShort)
            {
                query = query.Where(s => 
                    (s.TenSanPham.ToLower().Contains("short") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("short"))) &&
                    !s.TenSanPham.ToLower().Contains("áo")
                );
                introText = "Dạ Em chào Anh/Chị ạ! Fashion Store xin gợi ý những mẫu quần short năng động, thoáng mát rất thích hợp dạo phố và thể thao:";
            }
            else if (isQuan)
            {
                query = query.Where(s => 
                    (s.TenSanPham.ToLower().Contains("quần") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("quần"))) &&
                    !s.TenSanPham.ToLower().Contains("áo") &&
                    (s.LoaiSanPham == null || (!s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("áo") && !s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("jacket") && !s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("khoác")))
                );
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm kiếm quần thời trang của mình, Em xin gợi ý những mẫu quần tây công sở, quần jean và quần kaki cao cấp, tôn dáng và bán chạy nhất tại Fashion Store:";
            }
            else if (isAskingKhoac)
            {
                query = query.Where(s => 
                    s.TenSanPham.ToLower().Contains("khoác") || s.TenSanPham.ToLower().Contains("jacket") || s.TenSanPham.ToLower().Contains("blazer") ||
                    (s.LoaiSanPham != null && (s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("jacket") || s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("khoác") || s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("blazer")))
                );
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm áo khoác của mình, Em xin gợi ý những mẫu áo khoác gió cản nước, áo khoác jean và jacket thời thượng đang sẵn hàng tại shop:";
            }
            else if (isAskingPolo)
            {
                query = query.Where(s => s.TenSanPham.ToLower().Contains("polo"));
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm áo polo của mình, Em xin gợi ý những mẫu áo polo dệt pique cao cấp, phom dáng chuẩn đẹp và thoáng mát nhất tại shop:";
            }
            else if (isAskingSoMi)
            {
                query = query.Where(s => s.TenSanPham.ToLower().Contains("sơ mi"));
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm áo sơ mi của mình, Em xin gợi ý những mẫu sơ mi công sở cao cấp chống nhăn, phom dáng thanh lịch nhất tại shop:";
            }
            else if (isAskingThun)
            {
                query = query.Where(s => s.TenSanPham.ToLower().Contains("thun") || s.TenSanPham.ToLower().Contains("t-shirt"));
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm áo thun của mình, Em xin gợi ý những mẫu áo thun cotton 100% co giãn thoáng mát, thấm hút mồ hôi tối ưu:";
            }
            else if (isAskingHoodie)
            {
                query = query.Where(s => s.TenSanPham.ToLower().Contains("hoodie") || s.TenSanPham.ToLower().Contains("nỉ") || s.TenSanPham.ToLower().Contains("sweater"));
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu của mình, Em xin gợi ý các mẫu áo nỉ, hoodie ấm áp và năng động nhất tại Fashion Store:";
            }
            else if (isAo)
            {
                query = query.Where(s => (s.TenSanPham.ToLower().Contains("áo") || (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("áo"))) && !s.TenSanPham.ToLower().Contains("quần"));
                introText = "Dạ Em chào Anh/Chị ạ! Dựa trên yêu cầu tìm áo của mình, Em xin gợi ý những mẫu áo polo, áo sơ mi và áo thun cao cấp, phom dáng chuẩn mực và thoáng mát đang sẵn hàng tại shop:";
            }
            else if (isVay)
            {
                query = query.Where(s => 
                    s.TenSanPham.ToLower().Contains("đầm") || 
                    s.TenSanPham.ToLower().Contains("váy") || 
                    (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("váy")));

                introText = "Dạ Em chào Anh/Chị ạ! Fashion Store xin gợi ý những mẫu váy đầm thanh lịch, tôn dáng và thời thượng nhất tại cửa hàng:";
            }
            else if (isGiay)
            {
                query = query.Where(s => 
                    s.TenSanPham.ToLower().Contains("giày") || 
                    s.TenSanPham.ToLower().Contains("dép") || 
                    s.TenSanPham.ToLower().Contains("sneaker") || 
                    (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("giày")));

                introText = "Dạ Em chào Anh/Chị ạ! Fashion Store xin gợi ý các mẫu giày dép năng động và êm ái dành cho Anh/Chị:";
            }
            else if (isPhuKien)
            {
                query = query.Where(s => 
                    s.TenSanPham.ToLower().Contains("ví") || 
                    s.TenSanPham.ToLower().Contains("lưng") || 
                    s.TenSanPham.ToLower().Contains("mũ") || 
                    (s.LoaiSanPham != null && s.LoaiSanPham.TenLoaiSanPham.ToLower().Contains("phụ kiện")));

                introText = "Dạ Em chào Anh/Chị ạ! Fashion Store xin gửi Anh/Chị những món phụ kiện tinh tế giúp hoàn thiện phong cách:";
            }
            else
            {
                query = query.OrderByDescending(s => s.IsBanChay).ThenByDescending(s => s.IsNoiBat);
                introText = "Dạ Em chào Anh/Chị ạ! Em là Chuyên viên tư vấn phong cách của Fashion Store. Em có thể hỗ trợ Anh/Chị chọn size chuẩn, phối set đồ thời trang theo phong cách và ngân sách, hoặc giải đáp mọi chính sách mua sắm ạ! Dưới đây là những mẫu sản phẩm hot và được yêu thích nhất hôm nay tại shop để Anh/Chị tham khảo nhé:";
            }

            var matched = await query.Take(4).ToListAsync();

            // Nếu danh mục cụ thể bị trống, fallback chỉ trong đúng nhóm đó, TUYỆT ĐỐI không lấy chéo danh mục
            if (!matched.Any())
            {
                if (isQuan)
                {
                    matched = await _context.SanPhams
                        .Where(s => s.TrangThai == "Đang bán" && s.TenSanPham.Contains("Quần") && !s.TenSanPham.Contains("Áo"))
                        .Take(4)
                        .ToListAsync();
                }
                else if (isAo)
                {
                    matched = await _context.SanPhams
                        .Where(s => s.TrangThai == "Đang bán" && s.TenSanPham.Contains("Áo") && !s.TenSanPham.Contains("Quần"))
                        .Take(4)
                        .ToListAsync();
                }
                else
                {
                    matched = await _context.SanPhams
                        .Where(s => s.TrangThai == "Đang bán")
                        .OrderByDescending(s => s.IsBanChay)
                        .Take(4)
                        .ToListAsync();
                }
            }

                        var idsList = string.Join(", ", matched.Select(m => m.MaSanPham));
            var recommendTag = matched.Count > 0 ? $"\n[RECOMMEND: {idsList}]" : "";

            if (pLower.Contains("chào") || pLower.Contains("hi") || pLower.Contains("hello") || string.IsNullOrEmpty(pLower))
            {
                return "Dạ Em chào Anh/Chị ạ! Em là Chuyên viên tư vấn phong cách của Fashion Store. Em có thể hỗ trợ Anh/Chị chọn size chuẩn, phối đồ thời trang theo phong cách và ngân sách, hoặc giải đáp mọi chính sách mua sắm ạ! Dưới đây là những sản phẩm đang được yêu thích nhất hôm nay tại shop, Anh/Chị tham khảo nhé:" + recommendTag;
            }

            return introText + recommendTag;
        }

        private static BodyRecommendationResponse BuildFallbackBodyRecommendation(double height, double weight, string gender, List<SanPham> products)
        {
            double heightM = height / 100.0;
            double bmi = heightM > 0 ? (weight / (heightM * heightM)) : 22.0;

            string size = "M";
            if (weight < 54 || height < 162) size = "S";
            else if (weight <= 64 && height <= 170) size = "M";
            else if (weight <= 74 && height <= 176) size = "L";
            else if (weight <= 84 && height <= 182) size = "XL";
            else size = "XXL";

            string advice;
            if (bmi < 18.5)
            {
                advice = $"Với chiều cao {height}cm và cân nặng {weight}kg (BMI: {bmi:F1}), vóc dáng của bạn thuộc tuýp mảnh mai. Lời khuyên thời trang: Bạn nên ưu tiên chọn trang phục phom dáng Regular fit hoặc hơi Oversize với các gam màu sáng (trắng, be, pastel), họa tiết kẻ ngang để tạo cảm giác đầy đặn và cân đối hơn. Tránh mặc đồ bó sát hoặc các mẫu áo cổ tim quá sâu.";
            }
            else if (bmi <= 24.9)
            {
                advice = $"Với chiều cao {height}cm và cân nặng {weight}kg (BMI: {bmi:F1}), bạn đang sở hữu chỉ số cơ thể cân đối lý tưởng! Lời khuyên thời trang: Bạn rất dễ phối đồ, cả phom dáng Slim fit tôn nét khỏe khoắn lẫn Regular fit thanh lịch đều phù hợp. Bạn có thể tự tin diện áo polo phối quần âu hoặc sơ mi kết hợp quần kaki năng động cho mọi sự kiện.";
            }
            else
            {
                advice = $"Với chiều cao {height}cm và cân nặng {weight}kg (BMI: {bmi:F1}), vóc dáng của bạn đầy đặn và khỏe khoắn. Lời khuyên thời trang: Bạn nên ưu tiên trang phục phom Regular fit hoặc suông nhẹ vừa vặn cơ thể, chất liệu vải có độ co giãn tốt và đứng phom. Hãy lựa chọn các gam màu trầm, trung tính (đen, xanh navy, than chì) và họa tiết kẻ sọc dọc để giúp cơ thể trông gọn gàng, thanh thoát hơn.";
            }

            var matchingProducts = products
                .Where(p => string.IsNullOrEmpty(gender) || gender == "Unisex" || p.GioiTinh == gender || p.GioiTinh == "Unisex")
                .Take(4)
                .ToList();

            if (matchingProducts.Count == 0)
            {
                matchingProducts = products.Take(4).ToList();
            }

            var recommendations = new List<ProductRecommendationDto>();
            foreach (var p in matchingProducts)
            {
                string reason = p.TenSanPham.Contains("polo", StringComparison.OrdinalIgnoreCase)
                    ? "Áo polo phom chuẩn tôn vóc dáng, chất liệu co giãn thoáng mát rất phù hợp với số đo cơ thể bạn."
                    : (p.TenSanPham.Contains("sơ mi", StringComparison.OrdinalIgnoreCase)
                        ? "Sơ mi thiết kế thanh lịch, dễ dàng điều chỉnh mở cúc nhẹ để tạo cảm giác cơ thể cân đối hài hòa."
                        : "Phom dáng hiện đại, dễ phối đồ và cực kỳ tôn dáng cho vóc dáng của bạn.");

                recommendations.Add(new ProductRecommendationDto
                {
                    productId = p.MaSanPham,
                    reason = reason
                });
            }

            return new BodyRecommendationResponse
            {
                advice = advice,
                recommended_size = size,
                recommendations = recommendations
            };
        }

        private static string CleanHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var text = html
                .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n")
                .Replace("</p>", "\n\n")
                .Replace("</li>", "\n")
                .Replace("<li>", "- ");
            text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text);
            return text.Trim();
        }

        private static ProductAiInfoResponse BuildFallbackProductInfo(string productName)
        {
            var pLower = productName.ToLower();

            string kieuCo = "Cổ tròn";
            if (pLower.Contains("polo")) kieuCo = "Cổ bẻ (Polo)";
            else if (pLower.Contains("sơ mi") || pLower.Contains("so mi")) kieuCo = "Cổ bẻ sơ mi";
            else if (pLower.Contains("vest") || pLower.Contains("blazer")) kieuCo = "Cổ bẻ ve (Notch lapel)";
            else if (pLower.Contains("tim") || pLower.Contains("chữ v")) kieuCo = "Cổ tim";
            else if (pLower.Contains("trụ") || pLower.Contains("tàu")) kieuCo = "Cổ trụ";
            else if (pLower.Contains("quần") || pLower.Contains("short") || pLower.Contains("jean")) kieuCo = "";

            string chatLieu = "Cotton cao cấp (100% Cotton compact)";
            if (pLower.Contains("jean") || pLower.Contains("bò")) chatLieu = "Denim co giãn cao cấp";
            else if (pLower.Contains("tây") || pLower.Contains("âu") || pLower.Contains("công sở")) chatLieu = "Vải Tuyết mưa / Kaki cao cấp chống nhăn";
            else if (pLower.Contains("kaki") || pLower.Contains("khaki")) chatLieu = "Kaki dệt chéo chống nhăn";
            else if (pLower.Contains("linen") || pLower.Contains("đũi")) chatLieu = "Linen tự nhiên thoáng mát";
            else if (pLower.Contains("lụa") || pLower.Contains("silk")) chatLieu = "Lụa mềm mại";
            else if (pLower.Contains("dù") || pLower.Contains("gió")) chatLieu = "Polyester trượt nước";
            else if (pLower.Contains("len")) chatLieu = "Len dệt mịn giữ ấm";

            string formDang = "Regular fit";
            if (pLower.Contains("oversize") || pLower.Contains("rộng")) formDang = "Oversize";
            else if (pLower.Contains("slim") || pLower.Contains("ôm")) formDang = "Slim fit";
            else if (pLower.Contains("suông") || pLower.Contains("suong")) formDang = "Form suông thoải mái";

            string gioiTinh = "Unisex";
            if (pLower.Contains("nữ") || pLower.Contains("nu") || pLower.Contains("váy") || pLower.Contains("đầm")) gioiTinh = "Nữ";
            else if (pLower.Contains("nam")) gioiTinh = "Nam";

            var sbDesc = new StringBuilder();
            sbDesc.AppendLine($"Sản phẩm {productName} là thiết kế độc quyền tại Fashion Store, mang đến sự kết hợp hoàn hảo giữa nét trẻ trung, hiện đại và tính ứng dụng cao trong đời sống hàng ngày.");
            sbDesc.AppendLine();
            sbDesc.AppendLine($"- Chất liệu: {chatLieu}, tạo cảm giác mềm mại, thoáng mát và co giãn thoải mái suốt cả ngày dài.");
            sbDesc.AppendLine($"- Phom dáng: {formDang}, tôn dáng người mặc, đường may tinh xảo và sắc nét từng chi tiết.");
            sbDesc.AppendLine("- Phong cách: Dễ dàng phối cùng nhiều trang phục khác nhau, thích hợp đi làm, đi chơi hoặc gặp gỡ đối tác.");
            sbDesc.AppendLine();
            sbDesc.AppendLine("Cam kết hình ảnh chụp thật 100%, hỗ trợ đổi size linh hoạt trong 7 ngày.");

            return new ProductAiInfoResponse
            {
                KieuCoAo = kieuCo,
                ChatLieu = chatLieu,
                FormDang = formDang,
                GioiTinh = gioiTinh,
                MoTa = sbDesc.ToString().Trim(),
                IsFallback = true
            };
        }

        private static ReviewSentimentResponse BuildFallbackReviewSentiment(string reviewText)
        {
            if (string.IsNullOrWhiteSpace(reviewText))
            {
                return new ReviewSentimentResponse { Sentiment = "Trung lập", Tags = "đánh giá ngắn" };
            }

            var textLower = reviewText.ToLower();
            string[] posWords = ["đẹp", "tốt", "thích", "ưng", "chất lượng", "nhanh", "hài lòng", "tuyệt vời", "đúng mẫu", "xịn", "ok", "chuẩn", "mát", "êm"];
            string[] negWords = ["xấu", "tệ", "chán", "chậm", "rách", "lỗi", "không giống", "thất vọng", "kém", "mỏng dính", "chật", "rộng quá", "bẩn", "thô"];

            int posScore = posWords.Count(w => textLower.Contains(w, StringComparison.OrdinalIgnoreCase));
            int negScore = negWords.Count(w => textLower.Contains(w, StringComparison.OrdinalIgnoreCase));

            if (posScore > negScore)
            {
                return new ReviewSentimentResponse { Sentiment = "Tích cực", Tags = "chất lượng tốt, hài lòng" };
            }
            else if (negScore > posScore)
            {
                return new ReviewSentimentResponse { Sentiment = "Tiêu cực", Tags = "chưa hài lòng, cần cải thiện" };
            }
            else
            {
                return new ReviewSentimentResponse { Sentiment = "Trung lập", Tags = "đánh giá trải nghiệm" };
            }
        }

        private static FraudRiskResponse BuildFallbackFraudRisk(int totalOrders, int canceledOrders, int completedOrders, decimal totalSpent)
        {
            if (totalOrders == 0)
            {
                return new FraudRiskResponse { RiskLevel = "Thấp", Reason = "Khách hàng mới, chưa có lịch sử mua hàng." };
            }

            double cancelRate = (double)canceledOrders / totalOrders * 100.0;
            if (cancelRate >= 60.0 && totalOrders >= 2)
            {
                return new FraudRiskResponse 
                { 
                    RiskLevel = "Cao", 
                    Reason = $"Tỷ lệ hủy đơn cao ({canceledOrders}/{totalOrders} đơn, {cancelRate:N0}%), rủi ro bom hàng cao. Khuyến nghị xác nhận cọc trước khi giao." 
                };
            }
            else if (cancelRate >= 30.0)
            {
                return new FraudRiskResponse 
                { 
                    RiskLevel = "Trung bình", 
                    Reason = $"Khách hàng từng hủy {canceledOrders}/{totalOrders} đơn hàng. Cần chú ý theo dõi hành trình giao nhận." 
                };
            }
            else
            {
                return new FraudRiskResponse 
                { 
                    RiskLevel = "Thấp", 
                    Reason = $"Lịch sử đơn hàng uy tín ({completedOrders} đơn giao thành công, tổng thanh toán {totalSpent:N0} đ)." 
                };
            }
        }
    }
}
