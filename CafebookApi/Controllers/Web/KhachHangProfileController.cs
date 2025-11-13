using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // <-- Cần
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq; // <-- Cần

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/profile")]
    [ApiController]
    public class KhachHangProfileController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        // Constructor đã được đơn giản hóa (không cần IPasswordHasher)
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

        // --- (GetFullImageUrl giữ nguyên) ---
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        // ==========================================================
        // === SỬA: Cập nhật SaveImageAsync để dùng ID và Slug ===
        // ==========================================================
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string?> SaveImageAsync(IFormFile imageFile, string subFolder, string baseFileNameSlug, int userId)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var uploadPath = Path.Combine(_env.WebRootPath, "images", subFolder);
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var fileExtension = Path.GetExtension(imageFile.FileName);

            // Tên tệp mới: {IdKhachHang}_{SlugHoTen}.{ext}
            // VÍ DỤ: 1_khach-vang-lai.jpg
            var uniqueFileName = $"{userId}_{baseFileNameSlug}{fileExtension}";
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


        // --- (GetOverview và GetProfile giữ nguyên như tệp bạn đã tải lên) ---
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
                TongChiTieu = hoaDons.Sum(hd => hd.TongTienGoc - hd.GiamGia + hd.TongPhuThu)
            };
            return Ok(dto);
        }

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
            // Kiểm tra validation thủ công (vì FromForm không tự trigger)
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            // ==========================================================
            // === THÊM MỚI: KIỂM TRA TRÙNG LẶP ===
            // ==========================================================

            // 1. Kiểm tra Tên đăng nhập
            if (!string.IsNullOrEmpty(model.TenDangNhap) && model.TenDangNhap != kh.TenDangNhap)
            {
                var tenDangNhapExists = await _context.KhachHangs
                    .AnyAsync(k => k.IdKhachHang != id && k.TenDangNhap == model.TenDangNhap);
                if (tenDangNhapExists)
                {
                    // Trả về lỗi mà PageModel có thể đọc được
                    ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập này đã được sử dụng.");
                    return ValidationProblem(ModelState);
                }
            }

            // 2. Kiểm tra Email
            if (!string.IsNullOrEmpty(model.Email) && model.Email != kh.Email)
            {
                var emailExists = await _context.KhachHangs
                    .AnyAsync(k => k.IdKhachHang != id && k.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
                    return ValidationProblem(ModelState);
                }
            }

            // 3. Kiểm tra Số điện thoại
            if (!string.IsNullOrEmpty(model.SoDienThoai) && model.SoDienThoai != kh.SoDienThoai)
            {
                var sdtExists = await _context.KhachHangs
                    .AnyAsync(k => k.IdKhachHang != id && k.SoDienThoai == model.SoDienThoai);
                if (sdtExists)
                {
                    ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại này đã được sử dụng.");
                    return ValidationProblem(ModelState);
                }
            }
            // ==========================================================

            // Xử lý ảnh (giữ nguyên)
            if (avatarFile != null)
            {
                DeleteImage(kh.AnhDaiDien);
                string baseSlug = SlugifyUtil.GenerateSlug(model.HoTen);
                kh.AnhDaiDien = await SaveImageAsync(avatarFile, "avatars/avatarKH", baseSlug, id);
            }
            // ==========================================================

            // Cập nhật thông tin text
            kh.HoTen = model.HoTen;
            kh.SoDienThoai = model.SoDienThoai;
            kh.Email = model.Email;
            kh.DiaChi = model.DiaChi;
            kh.TenDangNhap = model.TenDangNhap; // Thêm dòng này

            await _context.SaveChangesAsync();
            return Ok(new { newAvatarUrl = GetFullImageUrl(kh.AnhDaiDien) });
        }
        /// <summary>
        /// API đổi mật khẩu (KHÔNG HASH)
        /// </summary>
        [HttpPost("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordChangeModel model)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            // Kiểm tra mật khẩu cũ (plain text)
            if (kh.MatKhau != model.MatKhauCu)
            {
                return BadRequest(new { Message = "Mật khẩu cũ không chính xác." });
            }

            kh.MatKhau = model.MatKhauMoi; // Lưu mật khẩu mới (plain text)
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đổi mật khẩu thành công." });
        }

        // ==========================================================
        // === THÊM MỚI: API LỊCH SỬ ĐẶT BÀN ===
        // ==========================================================
        [HttpGet("booking-history/{id}")]
        public async Task<IActionResult> GetBookingHistory(int id)
        {
            var bookings = await _context.PhieuDatBans
                .Include(p => p.Ban) // Join với bảng Ban
                .Where(p => p.IdKhachHang == id)
                .OrderByDescending(p => p.ThoiGianDat)
                .Select(p => new LichSuDatBanDto
                {
                    IdPhieuDatBan = p.IdPhieuDatBan,
                    // ======================================
                    // === SỬA LỖI Ở ĐÂY ===
                    // ======================================
                    TenBan = p.Ban.SoBan, // Sửa từ "TenBan" thành "SoBan"
                    // ======================================
                    ThoiGianDat = p.ThoiGianDat,
                    SoLuongKhach = p.SoLuongKhach,
                    TrangThai = p.TrangThai,
                    GhiChu = p.GhiChu
                })
                .ToListAsync();

            if (bookings == null)
            {
                return Ok(new List<LichSuDatBanDto>());
            }

            return Ok(bookings);
        }

        // ==========================================================
        // === THÊM MỚI: API LỊCH SỬ THUÊ SÁCH ===
        // ==========================================================
        [HttpGet("rental-history/{id}")]
        public async Task<IActionResult> GetRentalHistory(int id)
        {
            var rentals = await _context.PhieuThueSachs
                .Include(p => p.ChiTietPhieuThues) // Join để đếm số lượng sách

                // ==========================================================
                // === SỬA LỖI CS1061 TẠI ĐÂY ===
                // Đổi PhieuTraSach (số ít) thành PhieuTraSachs (số nhiều)
                .Include(p => p.PhieuTraSachs)
                // ==========================================================

                .Where(p => p.IdKhachHang == id)
                .OrderByDescending(p => p.NgayThue)
                .Select(p => new LichSuPhieuThueDto
                {
                    IdPhieuThueSach = p.IdPhieuThueSach,
                    NgayThue = p.NgayThue,
                    TrangThai = p.TrangThai,
                    SoLuongSach = p.ChiTietPhieuThues.Count(), // Đếm số sách trong chi tiết
                    TongTienCoc = p.TongTienCoc,

                    // ==========================================================
                    // === SỬA LỖI CS1061 TẠI ĐÂY ===
                    // Thêm .FirstOrDefault() để lấy 1 phiếu trả duy nhất từ danh sách
                    // ==========================================================
                    NgayTra = p.PhieuTraSachs.FirstOrDefault() != null ? (DateTime?)p.PhieuTraSachs.FirstOrDefault().NgayTra : null,
                    TongPhiThue = p.PhieuTraSachs.FirstOrDefault() != null ? (decimal?)p.PhieuTraSachs.FirstOrDefault().TongPhiThue : null,
                    TongTienPhat = p.PhieuTraSachs.FirstOrDefault() != null ? (decimal?)p.PhieuTraSachs.FirstOrDefault().TongTienPhat : null,
                    TongTienCocHoan = p.PhieuTraSachs.FirstOrDefault() != null ? (decimal?)p.PhieuTraSachs.FirstOrDefault().TongTienCocHoan : null
                })
                .ToListAsync();

            if (rentals == null)
            {
                return Ok(new List<LichSuPhieuThueDto>()); // Trả về danh sách rỗng
            }

            return Ok(rentals);
        }
    }
}