using CafebookApi.Data; // Thêm
using Microsoft.EntityFrameworkCore; // Thêm

var builder = WebApplication.CreateBuilder(args);

// 1. LẤY CHUỖI KẾT NỐI
var connectionString = builder.Configuration.GetConnectionString("CafeBookConnectionString");

// 2. THÊM DbContext VÀO API
builder.Services.AddDbContext<CafebookDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();