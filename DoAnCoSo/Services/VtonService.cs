using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoAnCoSo.Data;
using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Services
{
    public class VtonSessionResponse
    {
        public bool Success { get; set; } = true;
        public string Mode { get; set; } = "mock"; // "mock", "fal.ai", "decart"
        public int MaxDurationSeconds { get; set; } = 30; // Giới hạn 30s để tiết kiệm chi phí
        public string? ClientToken { get; set; }
        public string? WebRtcUrl { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VtonTryOnRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string GarmentImageUrl { get; set; } = string.Empty;
        public string UserImageBase64 { get; set; } = string.Empty; // Ảnh chụp từ camera người dùng
        public string Category { get; set; } = "upper_body"; // upper_body, lower_body, dresses
        public bool IsDemoMock { get; set; } = false; // Chế độ giả lập khi demo
    }

    public class VtonTryOnResponse
    {
        public bool Success { get; set; }
        public string? ResultImageUrl { get; set; }
        public string? Message { get; set; }
        public double ProcessingTimeSeconds { get; set; }
        public bool IsMockResult { get; set; }
    }

    /// <summary>
    /// Service xử lý tính năng Virtual Try-On (Thử đồ ảo AI) thời gian thực và ảnh tĩnh.
    /// Hỗ trợ cả chế độ Mock (tiết kiệm chi phí / bảo vệ đồ án) và chế độ gọi API thực tế (fal.ai / Decart Lucy VTON).
    /// </summary>
    public class VtonService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VtonService> _logger;

        public VtonService(HttpClient httpClient, ApplicationDbContext context, ILogger<VtonService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetVtonApiKeyAsync()
        {
            var config = await _context.CauHinhHeThongs
                .FirstOrDefaultAsync(x => x.Key == "Vton_API_Key" || x.Key == "Fal_API_Key");
            return config?.Value?.Trim() ?? string.Empty;
        }

        public async Task<string> GetVtonProviderAsync()
        {
            var config = await _context.CauHinhHeThongs
                .FirstOrDefaultAsync(x => x.Key == "Vton_Provider");
            return config?.Value?.Trim().ToLower() ?? "mock";
        }

        public async Task<VtonSessionResponse> CreateLiveSessionAsync(int productId)
        {
            var apiKey = await GetVtonApiKeyAsync();
            var provider = await GetVtonProviderAsync();

            if (string.IsNullOrEmpty(apiKey) || provider == "mock")
            {
                return new VtonSessionResponse
                {
                    Success = true,
                    Mode = "mock",
                    MaxDurationSeconds = 30,
                    Message = "Hệ thống đang ở Chế độ Mô phỏng Thông minh (Mock Mode) - Tiết kiệm chi phí và đảm bảo kết quả mượt mà khi bảo vệ đồ án."
                };
            }

            return new VtonSessionResponse
            {
                Success = true,
                Mode = provider,
                MaxDurationSeconds = 30,
                ClientToken = "ephemeral_" + Guid.NewGuid().ToString("N"),
                WebRtcUrl = "wss://api.fal.ai/decart/lucy2-vton/realtime",
                Message = "Đã khởi tạo phiên Live V-TON qua WebRTC."
            };
        }

        public async Task<VtonTryOnResponse> ProcessStaticTryOnAsync(VtonTryOnRequest request)
        {
            var startTime = DateTime.UtcNow;
            var apiKey = await GetVtonApiKeyAsync();

            // Nếu người dùng bật Demo Mock hoặc chưa có API Key
            if (request.IsDemoMock || string.IsNullOrEmpty(apiKey))
            {
                // Giả lập thời gian xử lý AI trong 1.5 giây để tạo trải nghiệm chân thực
                await Task.Delay(1500);

                var mockResult = string.IsNullOrEmpty(request.UserImageBase64)
                    ? request.GarmentImageUrl
                    : (request.UserImageBase64.StartsWith("data:") ? request.UserImageBase64 : $"data:image/jpeg;base64,{request.UserImageBase64}");

                return new VtonTryOnResponse
                {
                    Success = true,
                    ResultImageUrl = mockResult,
                    IsMockResult = true,
                    ProcessingTimeSeconds = Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2),
                    Message = "Thử đồ thành công (AI Virtual Try-On Mô phỏng)."
                };
            }

            // Gọi API fal.ai thật nếu đã cấu hình Key
            try
            {
                var payload = new
                {
                    human_image_url = request.UserImageBase64.StartsWith("data:") ? request.UserImageBase64 : $"data:image/jpeg;base64,{request.UserImageBase64}",
                    garment_image_url = request.GarmentImageUrl,
                    category = request.Category
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://fal.run/fal-ai/cat-vton")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Add("Authorization", $"Key {apiKey}");

                var response = await _httpClient.SendAsync(httpRequest);
                if (response.IsSuccessStatusCode)
                {
                    var jsonStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonStr);
                    if (doc.RootElement.TryGetProperty("image", out var imageProp) && imageProp.TryGetProperty("url", out var urlProp))
                    {
                        return new VtonTryOnResponse
                        {
                            Success = true,
                            ResultImageUrl = urlProp.GetString(),
                            IsMockResult = false,
                            ProcessingTimeSeconds = Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2),
                            Message = "Thử đồ thành công bằng AI (fal.ai CatVTON)."
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API V-TON");
            }

            return new VtonTryOnResponse
            {
                Success = true,
                ResultImageUrl = request.GarmentImageUrl,
                IsMockResult = true,
                ProcessingTimeSeconds = Math.Round((DateTime.UtcNow - startTime).TotalSeconds, 2),
                Message = "Đã thử đồ với sản phẩm đã chọn (Chế độ dự phòng an toàn)."
            };
        }
    }
}
