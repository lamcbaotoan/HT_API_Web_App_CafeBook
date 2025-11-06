using CafebookApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("CafeBookConnectionString");
builder.Services.AddDbContext<CafebookDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2️⃣ Cấu hình controller, Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3️⃣ Swagger (chỉ bật khi dev)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4️⃣ Cho phép truy cập file tĩnh (wwwroot)
app.UseStaticFiles();  // <--- Quan trọng: nằm trước UseRouting()

// 5️⃣ Hỗ trợ HTTPS redirect
//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// 6️⃣ Map controllers
app.MapControllers();

app.Run();
