// File: ThongTinCaNhanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/thongtincanhan")]
    [ApiController]
    [Authorize]
    public class ThongTinCaNhanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _apiBaseUrl;

        public ThongTinCaNhanController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _apiBaseUrl = config["Jwt:Issuer"] ?? "http://127.0.0.1:5166";
        }

        private int GetIdNhanVienFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int id)) return id;
            throw new UnauthorizedAccessException("Không thể xác thực nhân viên.");
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyInfo()
        {
            // (Hàm này giữ nguyên)
            var idNhanVien = GetIdNhanVienFromToken();
            var nhanVien = await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.IdNhanVien == idNhanVien);
            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");
            var homNay = DateTime.Today;
            var lichHomNay = await _context.LichLamViecs
                .Include(l => l.CaLamViec)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdNhanVien == idNhanVien && l.NgayLam.Date == homNay);
            var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
            var cuoiThang = dauThang.AddMonths(1).AddDays(-1);
            var soLanNghi = await _context.DonXinNghis
                .Where(d => d.IdNhanVien == idNhanVien &&
                            (d.NgayBatDau >= dauThang && d.NgayBatDau <= cuoiThang))
                .CountAsync();
            var lichLamViec = await _context.LichLamViecs
                .Include(l => l.CaLamViec)
                .Where(l => l.IdNhanVien == idNhanVien &&
                            l.NgayLam >= dauThang && l.NgayLam <= cuoiThang)
                .OrderBy(l => l.NgayLam)
                .Select(l => new LichLamViecChiTietDto
                {
                    NgayLam = l.NgayLam,
                    TenCa = l.CaLamViec.TenCa,
                    GioBatDau = l.CaLamViec.GioBatDau,
                    GioKetThuc = l.CaLamViec.GioKetThuc
                })
                .ToListAsync();
            var response = new ThongTinCaNhanViewDto
            {
                NhanVien = new NhanVienInfoDto
                {
                    IdNhanVien = nhanVien.IdNhanVien,
                    HoTen = nhanVien.HoTen,
                    SoDienThoai = nhanVien.SoDienThoai,
                    Email = nhanVien.Email,
                    DiaChi = nhanVien.DiaChi,
                    AnhDaiDien = string.IsNullOrEmpty(nhanVien.AnhDaiDien) ? null : $"{_apiBaseUrl}{nhanVien.AnhDaiDien}"
                },
                LichLamViecHomNay = lichHomNay == null ? null : new LichLamViecDto
                {
                    TenCa = lichHomNay.CaLamViec.TenCa,
                    GioBatDau = lichHomNay.CaLamViec.GioBatDau,
                    GioKetThuc = lichHomNay.CaLamViec.GioKetThuc
                },
                SoLanXinNghiThangNay = soLanNghi,
                LichLamViecThangNay = lichLamViec
            };
            return Ok(response);
        }

        [HttpPut("update-info")]
        public async Task<IActionResult> UpdateInfo([FromForm] CapNhatThongTinDto req, IFormFile? avatarFile)
        {
            // (Hàm này giữ nguyên)
            var idNhanVien = GetIdNhanVienFromToken();
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound();
            nhanVien.HoTen = req.HoTen;
            nhanVien.SoDienThoai = req.SoDienThoai;
            nhanVien.Email = req.Email;
            nhanVien.DiaChi = req.DiaChi;
            if (avatarFile != null && avatarFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(nhanVien.AnhDaiDien))
                {
                    var oldPhysicalPath = Path.Combine(_env.WebRootPath, nhanVien.AnhDaiDien.TrimStart('/'));
                    if (System.IO.File.Exists(oldPhysicalPath)) System.IO.File.Delete(oldPhysicalPath);
                }
                var fileExt = Path.GetExtension(avatarFile.FileName);
                var newFileName = $"{nhanVien.IdNhanVien}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";
                var relativeUrl = $"{HinhAnhPaths.UrlAvatarNV}/{newFileName}";
                var physicalPath = Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/'));
                var directory = Path.GetDirectoryName(physicalPath);
                if (directory != null)
                {
                    Directory.CreateDirectory(directory);
                }
                else
                {
                    return BadRequest("Đường dẫn lưu file không hợp lệ.");
                }
                await using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }
                nhanVien.AnhDaiDien = relativeUrl;
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công!" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] DoiMatKhauRequestDto req)
        {
            // (Hàm này giữ nguyên)
            if (string.IsNullOrEmpty(req.MatKhauCu) || string.IsNullOrEmpty(req.MatKhauMoi))
                return BadRequest("Mật khẩu cũ và mới không được để trống.");
            var idNhanVien = GetIdNhanVienFromToken();
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound();
            if (nhanVien.MatKhau != req.MatKhauCu)
                return BadRequest("Mật khẩu cũ không chính xác.");
            nhanVien.MatKhau = req.MatKhauMoi;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // --- HÀM ĐÃ ĐƯỢC CẬP NHẬT ---
        [HttpPost("submit-leave")]
        public async Task<IActionResult> SubmitLeaveRequest([FromBody] DonXinNghiRequestDto req)
        {
            if (req.NgayKetThuc < req.NgayBatDau)
                return BadRequest("Ngày kết thúc không thể trước ngày bắt đầu.");

            var idNhanVien = GetIdNhanVienFromToken();

            // 1. Lấy thông tin Tên nhân viên để tạo nội dung thông báo
            // (Chúng ta chỉ Select HoTen để tối ưu)
            var nhanVien = await _context.NhanViens
                .AsNoTracking()
                .Select(nv => new { nv.IdNhanVien, nv.HoTen })
                .FirstOrDefaultAsync(nv => nv.IdNhanVien == idNhanVien);

            if (nhanVien == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            // 2. Tạo đối tượng Đơn Xin Nghỉ
            var don = new DonXinNghi
            {
                IdNhanVien = idNhanVien,
                LoaiDon = req.LoaiDon,
                LyDo = req.LyDo,
                NgayBatDau = req.NgayBatDau,
                NgayKetThuc = req.NgayKetThuc,
                TrangThai = "Chờ duyệt",
            };

            // 3. Lưu đơn nghỉ (để lấy IdDonXinNghi tự động tăng)
            _context.DonXinNghis.Add(don);
            await _context.SaveChangesAsync();

            // 4. THÊM MỚI: Tạo đối tượng Thông Báo
            var thongBao = new ThongBao
            {
                IdNhanVienTao = idNhanVien,
                NoiDung = $"Đơn nghỉ mới từ {nhanVien.HoTen}",
                ThoiGianTao = DateTime.Now,
                LoaiThongBao = "DonXinNghi", // Giống ví dụ của bạn
                IdLienQuan = don.IdDonXinNghi, // Dùng ID của đơn vừa tạo
                DaXem = false // Mặc định là chưa xem
            };

            // 5. Lưu thông báo vào CSDL
            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi đơn xin nghỉ và tạo thông báo thành công!" });
        }
    }
}