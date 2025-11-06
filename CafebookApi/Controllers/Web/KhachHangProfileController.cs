using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/profile")]
    [ApiController]
    public class KhachHangProfileController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public KhachHangProfileController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        // --- (Các hàm helper xử lý ảnh: GetFullImageUrl, SaveImageAsync, DeleteImage giữ nguyên) ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string?> SaveImageAsync(IFormFile imageFile, string subFolder, string baseFileName)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var uploadPath = Path.Combine(_env.WebRootPath, "images", subFolder);
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var fileExtension = Path.GetExtension(imageFile.FileName);
            var uniqueFileName = $"{baseFileName}_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/{subFolder}/{uniqueFileName}".Replace(Path.DirectorySeparatorChar, '/');
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }



        /// <summary>
        /// API MỚI: Lấy thông tin tổng quan (Điểm tích lũy)
        /// </summary>
        [HttpGet("overview/{id}")]
        public async Task<IActionResult> GetOverview(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            var hoaDons = await _context.HoaDons
                .Where(hd => hd.IdKhachHang == id && hd.TrangThai == "Đã thanh toán")
                .ToListAsync();

            var dto = new KhachHangTongQuanDto
            {
                DiemTichLuy = kh.DiemTichLuy,
                NgayTao = kh.NgayTao,
                TongHoaDon = hoaDons.Count,
                // SỬA LỖI CS1061: Tính toán dựa trên các cột có thật
                TongChiTieu = hoaDons.Sum(hd => hd.TongTienGoc - hd.GiamGia + hd.TongPhuThu)
            };
            return Ok(dto);
        }

        /// <summary>
        /// API lấy thông tin chi tiết của khách hàng
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            var dto = new KhachHangProfileDto
            {
                IdKhachHang = kh.IdKhachHang,
                HoTen = kh.HoTen,
                SoDienThoai = kh.SoDienThoai,
                Email = kh.Email,
                DiaChi = kh.DiaChi,
                TenDangNhap = kh.TenDangNhap,
                AnhDaiDienUrl = GetFullImageUrl(kh.AnhDaiDien)
            };
            return Ok(dto);
        }

        /// <summary>
        /// API cập nhật thông tin cá nhân (bao gồm cả ảnh)
        /// </summary>
        [HttpPut("update-info/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] ProfileUpdateModel model, IFormFile? avatarFile)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            // Cập nhật ảnh (nếu có)
            if (avatarFile != null)
            {
                DeleteImage(kh.AnhDaiDien); // Xóa ảnh cũ
                string baseFileName = kh.TenDangNhap ?? kh.Email ?? kh.IdKhachHang.ToString();
                kh.AnhDaiDien = await SaveImageAsync(avatarFile, "avatars/avatarKH", SlugifyUtil.GenerateSlug(baseFileName));
            }

            // Cập nhật thông tin text
            kh.HoTen = model.HoTen;
            kh.SoDienThoai = model.SoDienThoai;
            kh.Email = model.Email;
            kh.DiaChi = model.DiaChi;

            await _context.SaveChangesAsync();
            return Ok(new { newAvatarUrl = GetFullImageUrl(kh.AnhDaiDien) });
        }

        /// <summary>
        /// API đổi mật khẩu
        /// </summary>
        [HttpPost("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeModel model)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            // SỬA: Phải kiểm tra mật khẩu cũ (trong thực tế phải HASH)
            if (kh.MatKhau != model.MatKhauCu)
            {
                return BadRequest(new { Message = "Mật khẩu cũ không chính xác." });
            }

            kh.MatKhau = model.MatKhauMoi; // (Trong thực tế phải HASH mật khẩu mới)
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đổi mật khẩu thành công." });
        }
    }
}