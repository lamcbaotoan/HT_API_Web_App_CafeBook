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
// THÊM CÁC USING SAU:
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoankhach")]
    [ApiController]
    public class KhachHangTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;
        private readonly IConfiguration _config; // SỬA: Thêm IConfiguration

        public KhachHangTaiKhoanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config; // SỬA: Thêm
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

        // --- (Hàm Register giữ nguyên) ---
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DangKyRequestModel model)
        {
            // ... (Giữ nguyên logic của bạn) ...
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.SoDienThoai) || string.IsNullOrEmpty(model.Password))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Vui lòng điền đầy đủ Email, SĐT và Mật khẩu." });
            }
            var existingFullAccount = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.TaiKhoanTam == false && (k.Email == model.Email || k.SoDienThoai == model.SoDienThoai));
            if (existingFullAccount != null)
            {
                if (existingFullAccount.Email == model.Email)
                {
                    return Ok(new WebLoginResponseModel { Success = false, Message = "Email này đã được sử dụng bởi một tài khoản khác." });
                }
                if (existingFullAccount.SoDienThoai == model.SoDienThoai)
                {
                    return Ok(new WebLoginResponseModel { Success = false, Message = "SĐT này đã được sử dụng bởi một tài khoản khác." });
                }
            }
            var existingTempAccount = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.TaiKhoanTam == true && (k.Email == model.Email || k.SoDienThoai == model.SoDienThoai));
            KhachHang khachHang;
            if (existingTempAccount != null)
            {
                khachHang = existingTempAccount;
                khachHang.Email = model.Email;
                khachHang.SoDienThoai = model.SoDienThoai;
                khachHang.MatKhau = model.Password;
                khachHang.NgayTao = DateTime.Now;
                khachHang.TaiKhoanTam = false;
                khachHang.BiKhoa = false;
                _context.KhachHangs.Update(khachHang);
            }
            else
            {
                khachHang = new KhachHang
                {
                    HoTen = model.Email,
                    Email = model.Email,
                    SoDienThoai = model.SoDienThoai,
                    TenDangNhap = null,
                    MatKhau = model.Password,
                    NgayTao = DateTime.Now,
                    DiemTichLuy = 0,
                    BiKhoa = false,
                    TaiKhoanTam = false
                };
                _context.KhachHangs.Add(khachHang);
            }
            await _context.SaveChangesAsync();

            // SỬA: Tự động đăng nhập và tạo token sau khi đăng ký
            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(khachHang.AnhDaiDien)
            };

            // Tạo JWT Token
            string token = GenerateJwtToken(khachHang);

            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto, Token = token });
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

            if (khachHang.BiKhoa)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên." });
            }

            if (khachHang.TaiKhoanTam)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này là tài khoản tạm. Vui lòng dùng chức năng Đăng Ký để kích hoạt." });
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

            // SỬA: Tạo và trả về JWT Token
            string token = GenerateJwtToken(khachHang);
            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto, Token = token });
        }

        // --- THÊM MỚI: Hàm helper tạo JWT ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string GenerateJwtToken(KhachHang khachHang)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

            var claims = new List<Claim>
            {
                // Thêm các Claim giống như trong DangNhapView.cshtml.cs
                new Claim(ClaimTypes.NameIdentifier, khachHang.IdKhachHang.ToString()),
                new Claim(ClaimTypes.Name, khachHang.TenDangNhap ?? khachHang.Email ?? ""),
                new Claim(ClaimTypes.GivenName, khachHang.HoTen),
                new Claim(ClaimTypes.Email, khachHang.Email ?? ""),
                new Claim(ClaimTypes.MobilePhone, khachHang.SoDienThoai ?? ""),
                new Claim(ClaimTypes.Role, "KhachHang"), // Quan trọng
                
                // Thêm Claim "IdNhanVien" (dù là khách) để đồng bộ với App,
                // nhưng set giá trị là 0 hoặc ID khách.
                // Tốt nhất là dùng claim riêng. Chúng ta sẽ dùng NameIdentifier.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Token hết hạn sau 7 ngày
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}