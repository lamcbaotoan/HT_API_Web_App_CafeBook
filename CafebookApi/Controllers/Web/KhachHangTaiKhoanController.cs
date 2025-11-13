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

            if (khachHang.BiKhoa)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên." });
            }

            // ==========================================================
            // === NÂNG CẤP: KIỂM TRA TÀI KHOẢN TẠM KHI ĐĂNG NHẬP ===
            // ==========================================================
            if (khachHang.TaiKhoanTam)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Tài khoản này là tài khoản tạm. Vui lòng dùng chức năng Đăng Ký để kích hoạt." });
            }
            // ==========================================================

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
            // ==========================================================
            // === NÂNG CẤP: TOÀN BỘ LOGIC ĐĂNG KÝ MỚI ===
            // ==========================================================
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.SoDienThoai) || string.IsNullOrEmpty(model.Password))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Vui lòng điền đầy đủ Email, SĐT và Mật khẩu." });
            }

            // 1. Kiểm tra Email/SĐT đã tồn tại ở tài khoản CHÍNH THỨC (taiKhoanTam = false) hay chưa
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

            // 2. Tìm tài khoản TẠM (taiKhoanTam = true) khớp Email hoặc SĐT
            var existingTempAccount = await _context.KhachHangs
                .FirstOrDefaultAsync(k => k.TaiKhoanTam == true && (k.Email == model.Email || k.SoDienThoai == model.SoDienThoai));

            KhachHang khachHang;

            if (existingTempAccount != null)
            {
                // TÌM THẤY TÀI KHOẢN TẠM -> NÂNG CẤP (UPDATE)
                khachHang = existingTempAccount;

                khachHang.Email = model.Email; // Cập nhật SĐT và Email mới
                khachHang.SoDienThoai = model.SoDienThoai;
                khachHang.MatKhau = model.Password; // Đặt mật khẩu mới
                khachHang.NgayTao = DateTime.Now; // Làm mới ngày tạo
                khachHang.TaiKhoanTam = false; // Chuyển thành tài khoản chính thức
                khachHang.BiKhoa = false;
                // DiemTichLuy được giữ nguyên

                _context.KhachHangs.Update(khachHang);
            }
            else
            {
                // KHÔNG TÌM THẤY -> TẠO MỚI (CREATE)
                khachHang = new KhachHang
                {
                    // Dùng Email làm Họ Tên mặc định để thỏa mãn NOT NULL
                    HoTen = model.Email,
                    Email = model.Email,
                    SoDienThoai = model.SoDienThoai,
                    TenDangNhap = model.Email, // Không dùng TenDangNhap nữa
                    MatKhau = model.Password,
                    NgayTao = DateTime.Now,
                    DiemTichLuy = 0, // Tài khoản mới bắt đầu từ 0
                    BiKhoa = false,
                    TaiKhoanTam = false // Tạo là tài khoản chính thức ngay
                };
                _context.KhachHangs.Add(khachHang);
            }

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
            // ==========================================================
        }
    }
}