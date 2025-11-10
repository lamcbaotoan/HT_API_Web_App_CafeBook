using Microsoft.AspNetCore.Authentication.Cookies;
using WebCafebookApi.Services;
using System.Net.Http.Headers; // <-- THÊM USING NÀY

var builder = WebApplication.CreateBuilder(args);

// === 1. SỬA LỖI 401: CẤU HÌNH HTTPCLIENT ===

// Thêm dịch vụ để đọc HttpContext (cần thiết để lấy cookie)
builder.Services.AddHttpContextAccessor();

// Cấu hình "ApiClient" để TỰ ĐỘNG GỬI TOKEN
builder.Services.AddHttpClient("ApiClient", (serviceProvider, client) =>
{
    // 1. Thiết lập địa chỉ API backend
    client.BaseAddress = new Uri("http://localhost:5166"); // Địa chỉ API
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // 2. Lấy HttpContext để đọc cookie
    var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    if (httpContextAccessor != null)
    {
        // 3. Lấy token từ cookie (tên "AuthToken" phải khớp với tên cookie lúc đăng nhập)
        var token = httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];

        // 4. Gắn token vào header của mọi request
        if (!string.IsNullOrEmpty(token))
        {
            // Gắn token vào header (Giả định token là JWT/Bearer)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
});

// Thêm một HttpClient mặc định (cho các cuộc gọi không cần auth)
builder.Services.AddHttpClient();

// ===========================================

// 2. SESSION (Giữ nguyên)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. AUTHENTICATION (Giữ nguyên)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // TODO: Đảm bảo tên cookie khớp với tên lúc đăng nhập
        options.Cookie.Name = "AuthToken";
        // Bỏ comment các dòng này để tự động chuyển hướng
        options.LoginPath = "/account/DangNhapView";
        options.AccessDeniedPath = "/AccessDenied";
        options.LogoutPath = "/Account/DangXuat";
    });

// 4. RAZOR PAGES (Giữ nguyên)
builder.Services.AddRazorPages();

// 5. ĐĂNG KÝ EMAIL SERVICE (Giữ nguyên)
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<EmailService>();

// 6. ĐĂNG KÝ BỘ NHỚ CACHE (Giữ nguyên)
builder.Services.AddMemoryCache();

var app = builder.Build();

// ... (Pipeline giữ nguyên) ...
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Đảm bảo đúng thứ tự
app.UseSession();
app.UseAuthentication(); // Bật xác thực
app.UseAuthorization(); // Bật phân quyền

app.MapRazorPages();
app.Run();