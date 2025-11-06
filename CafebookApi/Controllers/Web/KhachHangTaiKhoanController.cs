// Tập tin: CafebookApi/Controllers/Web/KhachHangTaiKhoanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApi;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoankhach")]
    [ApiController]
    public class KhachHangTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public KhachHangTaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.TenDangNhap) || string.IsNullOrEmpty(model.MatKhau))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Thông tin không hợp lệ." });
            }

            var userInput = model.TenDangNhap.Trim();
            var passInput = model.MatKhau.Trim();

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k =>
                (k.TenDangNhap == userInput || k.SoDienThoai == userInput || k.Email == userInput) &&
                (k.MatKhau == passInput)
            );

            if (khachHang == null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Sai thông tin đăng nhập hoặc mật khẩu." });
            }

            // SỬA ĐỔI: KIỂM TRA TÀI KHOẢN KHÓA
            if (khachHang.BiKhoa)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên." });
            }

            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(khachHang.AnhDaiDien)
            };
            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DangKyRequestModel model)
        {
            // ... (Logic kiểm tra trùng lặp và tạo KhachHang entity giữ nguyên) ...
            if (model == null || string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.SoDienThoai) || string.IsNullOrEmpty(model.Password))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Vui lòng điền đầy đủ thông tin bắt buộc." });
            }
            if (await _context.KhachHangs.AnyAsync(k => k.Email == model.Email))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Email này đã được sử dụng." });
            }
            if (await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == model.SoDienThoai))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "SĐT này đã được sử dụng." });
            }
            if (!string.IsNullOrEmpty(model.TenDangNhap) && await _context.KhachHangs.AnyAsync(k => k.TenDangNhap == model.TenDangNhap))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tên đăng nhập này đã được sử dụng." });
            }
            var khachHang = new KhachHang
            {
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                TenDangNhap = string.IsNullOrEmpty(model.TenDangNhap) ? null : model.TenDangNhap,
                MatKhau = model.Password,
                NgayTao = DateTime.Now,
                DiemTichLuy = 0,
                BiKhoa = false // Mặc định khi đăng ký
            };
            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();

            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(khachHang.AnhDaiDien)
            };
            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto });
        }
    }
}