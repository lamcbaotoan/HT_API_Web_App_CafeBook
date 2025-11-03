using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. THÊM DỊCH VỤ HTTPCLIENT (ĐỂ GỌI API)
builder.Services.AddHttpClient();

// 2. THÊM DỊCH VỤ SESSION (ĐỂ LƯU AVATAR)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. THÊM DỊCH VỤ AUTHENTICATION (ĐỂ TẠO COOKIE)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.LogoutPath = "/Account/DangXuat";
    });

// 4. THÊM RAZOR PAGES (của bạn đã có)
builder.Services.AddRazorPages(options =>
{
    //options.Conventions.AddPageRoute("/Account/DangNhapView", "/Account/Login");
    //options.Conventions.AddPageRoute("/Account/DangKyView", "/Account/Register");
    //options.Conventions.AddPageRoute("/Employee/DangNhapEmployee", "/Employee/Login");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

// 5. KÍCH HOẠT CÁC DỊCH VỤ (Thứ tự quan trọng)
app.UseSession(); // Kích hoạt Session
app.UseAuthentication(); // Kích hoạt Authentication
app.UseAuthorization(); // Kích hoạt Authorization

app.MapRazorPages();

app.Run();