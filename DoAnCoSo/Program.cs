using DoAnCoSo.Data;
using Microsoft.EntityFrameworkCore;

// ==========================================
// KHỞI TẠO BỘ DỰNG ỨNG DỤNG (WEBAPPLICATIONBUILDER)
// ==========================================
// Nơi đăng ký các dịch vụ (Services) vào DI (Dependency Injection) Container
var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ MVC (Model-View-Controller)
// Dịch vụ này giúp ứng dụng nhận diện và xử lý các Request thông qua Controller và hiển thị giao diện qua View (.cshtml)
builder.Services.AddControllersWithViews();

// 2. Cấu hình và kết nối cơ sở dữ liệu SQL Server thông qua Entity Framework Core (EF Core)
// Sử dụng Connection String "DefaultConnection" được cấu hình trong file appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Cấu hình Session (Phiên làm việc)
// Sử dụng để lưu trữ trạng thái người dùng tạm thời giữa các request (ví dụ: thông tin giỏ hàng, thông tin đăng nhập)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giữ phiên đăng nhập/làm việc trong 30 phút kể từ request cuối
    options.Cookie.HttpOnly = true;                 // Cookie bảo mật, chặn JavaScript phía Client truy cập để chống XSS
    options.Cookie.IsEssential = true;              // Bắt buộc cookie hoạt động để ứng dụng chạy đúng
});

// 4. Đăng ký HttpContextAccessor
// Cho phép truy cập vào HttpContext (Session, Request, Response, User...) từ các lớp/dịch vụ bên ngoài Controller nếu cần
builder.Services.AddHttpContextAccessor();

// 5. Đăng ký HttpClient và GeminiService với Timeout 5 phút
builder.Services.AddHttpClient<DoAnCoSo.Services.GeminiService>(client => 
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

// 6. Đăng ký các Dịch vụ Nâng cao Marketing, Logistics & Bảo mật
builder.Services.AddHttpClient<DoAnCoSo.Services.VtonService>(client => 
{
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddScoped<DoAnCoSo.Services.AbandonedCartService>();
builder.Services.AddScoped<DoAnCoSo.Services.LoyaltyService>();
builder.Services.AddScoped<DoAnCoSo.Services.AuditLogService>();

// ==========================================
// XÂY DỰNG ỨNG DỤNG (BUILD WEB APPLICATION)
// ==========================================
var app = builder.Build();

// ==========================================
// CẤU HÌNH MIDDLEWARE PIPELINE (LUỒNG XỬ LÝ REQUEST)
// ==========================================
// Các middleware này xử lý Request theo thứ tự tuần tự từ trên xuống dưới

// Cấu hình môi trường chạy ứng dụng
if (!app.Environment.IsDevelopment())
{
    // Nếu xảy ra lỗi ở môi trường Production, chuyển hướng người dùng đến trang báo lỗi thân thiện
    app.UseExceptionHandler("/Home/Error");
    // Kích hoạt cơ chế HSTS (HTTP Strict Transport Security) để bắt buộc trình duyệt chỉ kết nối qua HTTPS bảo mật
    app.UseHsts();
}

// Tự động chuyển hướng mọi request HTTP sang HTTPS bảo mật
app.UseHttpsRedirection();

// Cho phép phục vụ các file tĩnh (CSS, JS, hình ảnh, font...) từ thư mục wwwroot
app.UseStaticFiles();

// Định tuyến URL của Request để xác định Controller và Action phù hợp sẽ xử lý
app.UseRouting();

// 5. KÍCH HOẠT SESSION MIDDLEWARE
// Phải chạy trước Authorization để các thông tin xác thực lưu trong Session (như VaiTro, AdminUser) có sẵn cho bước phân quyền
app.UseSession();

// Middleware xác thực quyền truy cập của người dùng
app.UseAuthorization();

// 6. CẤU HÌNH ROUTING MẶC ĐỊNH (DEFAULT ROUTE)
// Map các mẫu URL dạng: domain/Controller/Action/Id
// Ví dụ: không truyền gì -> Home/Index; truyền /SanPham -> SanPham/Index; /SanPham/Details/5 -> SanPham/Details với id = 5
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 7. KHỞI TẠO DỮ LIỆU MẶC ĐỊNH (DATABASE INITIALIZATION & SEEDING)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi seed dữ liệu danh mục sản phẩm.");
    }
}

// Khởi chạy Web Server và bắt đầu lắng nghe các Request gửi đến từ Client
app.Run();