using CafebookApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("CafeBookConnectionString");
builder.Services.AddDbContext<CafebookDbContext>(options =>
    options.UseSqlServer(connectionString));

// === THÊM KHỐI NÀY ĐỂ SỬA LỖI 401 ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin() // Cho phép bất kỳ nguồn nào
                  .AllowAnyMethod() // Cho phép bất kỳ phương thức nào (GET, POST, PUT, v.v.)
                  .AllowAnyHeader(); // Cho phép bất kỳ header nào
        });
});
// =====================================

// 2️⃣ Cấu hình controller, Swagger
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3️⃣ Cấu hình xác thực JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
        ValidAudience = builder.Configuration["Jwt:Audience"]!,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});
var app = builder.Build();

// 4️⃣ Swagger (chỉ bật khi dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5️⃣ Cho phép truy cập file tĩnh (wwwroot)
app.UseStaticFiles();
app.UseCors("AllowAll"); // <-- Dòng này bây giờ đã hợp lệ

// 6️⃣ Hỗ trợ HTTPS redirect
//app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication(); // Phải trước UseAuthorization
app.UseAuthorization();

// 7️⃣ Map controllers
app.MapControllers();

app.Run();