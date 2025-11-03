using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApi;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/taikhoankhach")]
    [ApiController]
    public class KhachHangTaiKhoanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public KhachHangTaiKhoanController(CafebookDbContext context)
        {
            _context = context;
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

            // 1. TÌM KIẾM KHÁCH HÀNG
            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(k =>
                (k.TenDangNhap == userInput || k.SoDienThoai == userInput || k.Email == userInput) &&
                (k.MatKhau == passInput) // LƯU Ý: Phải HASH mật khẩu trong thực tế
            );

            if (khachHang == null)
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Sai thông tin đăng nhập hoặc mật khẩu." });
            }

            // 2. TẠO DTO TRẢ VỀ
            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap
            };

            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto });
        }
        // THÊM API MỚI CHO ĐĂNG KÝ
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DangKyRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.SoDienThoai) || string.IsNullOrEmpty(model.Password))
            {
                return Ok(new WebLoginResponseModel { Success = false, Message = "Vui lòng điền đầy đủ thông tin bắt buộc." });
            }

            // KIỂM TRA TRÙNG LẶP
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

            // TẠO KHÁCH HÀNG MỚI
            var khachHang = new KhachHang // <-- Dùng Entity
            {
                HoTen = model.HoTen,
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                TenDangNhap = string.IsNullOrEmpty(model.TenDangNhap) ? null : model.TenDangNhap,
                MatKhau = model.Password, // LƯU Ý: PHẢI HASH MẬT KHẨU
                NgayTao = DateTime.Now,
                DiemTichLuy = 0
            };

            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();

            // Trả về thông tin khách hàng (để tự động đăng nhập)
            var dto = new KhachHangDto
            {
                IdKhachHang = khachHang.IdKhachHang,
                HoTen = khachHang.HoTen,
                Email = khachHang.Email,
                SoDienThoai = khachHang.SoDienThoai,
                TenDangNhap = khachHang.TenDangNhap
            };

            return Ok(new WebLoginResponseModel { Success = true, KhachHangData = dto });
        }
    }
}