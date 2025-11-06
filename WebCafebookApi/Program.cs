using Microsoft.AspNetCore.Authentication.Cookies;
using WebCafebookApi.Services; // <-- THÊM USING NÀY

var builder = WebApplication.CreateBuilder(args);

// 1. HTTPCLIENT
builder.Services.AddHttpClient();

// 2. SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. AUTHENTICATION
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        //options.LoginPath = "/Account/DangNhapView";
        //options.AccessDeniedPath = "/AccessDenied";
       //options.LogoutPath = "/Account/DangXuat";
    });

// 4. RAZOR PAGES
builder.Services.AddRazorPages();

// 5. SỬA: ĐĂNG KÝ EMAIL SERVICE
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<EmailService>();

// 6. SỬA: ĐĂNG KÝ BỘ NHỚ CACHE
builder.Services.AddMemoryCache();

var app = builder.Build();

// ... (Pipeline giữ nguyên) ...
//app.UseHttpsRedirection(); // Thêm để đảm bảo chạy HTTPS
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();