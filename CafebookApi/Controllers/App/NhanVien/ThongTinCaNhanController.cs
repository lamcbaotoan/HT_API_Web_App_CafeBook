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
            _apiBaseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://localhost:5166";
        }

        private int GetIdNhanVienFromToken()
        {
            // (Giữ nguyên)
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int id)) return id;
            throw new UnauthorizedAccessException("Không thể xác thực nhân viên.");
        }

        /// <summary>
        /// SỬA: Lấy thông tin, số lần nghỉ, và lịch làm việc tháng
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyInfo()
        {
            var idNhanVien = GetIdNhanVienFromToken();
            var nhanVien = await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.IdNhanVien == idNhanVien);

            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");

            // Lấy lịch làm việc hôm nay (cho thông báo)
            var homNay = DateTime.Today;
            var lichHomNay = await _context.LichLamViecs
                .Include(l => l.CaLamViec)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdNhanVien == idNhanVien && l.NgayLam.Date == homNay);

            // SỬA: Tính toán cho tháng hiện tại
            var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
            var cuoiThang = dauThang.AddMonths(1).AddDays(-1);

            // 1. Lấy số lần xin nghỉ (tính các đơn bắt đầu trong tháng)
            var soLanNghi = await _context.DonXinNghis
                .Where(d => d.IdNhanVien == idNhanVien &&
                            (d.NgayBatDau >= dauThang && d.NgayBatDau <= cuoiThang))
                .CountAsync();

            // 2. Lấy lịch làm việc của tháng
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
                // SỬA: Gán 2 thuộc tính mới
                SoLanXinNghiThangNay = soLanNghi,
                LichLamViecThangNay = lichLamViec
            };

            return Ok(response);
        }

        // --- CÁC HÀM UPDATE, CHANGEPASSWORD, SUBMITLEAVE GIỮ NGUYÊN ---

        [HttpPut("update-info")]
        public async Task<IActionResult> UpdateInfo([FromForm] CapNhatThongTinDto req, IFormFile? avatarFile)
        {
            // (Giữ nguyên logic từ câu trả lời trước)
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
                Directory.CreateDirectory(Path.GetDirectoryName(physicalPath));
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
            // (Giữ nguyên logic từ câu trả lời trước)
            if (string.IsNullOrEmpty(req.MatKhauCu) || string.IsNullOrEmpty(req.MatKhauMoi))
                return BadRequest("Mật khẩu cũ và mới không được để trống.");
            var idNhanVien = GetIdNhanVienFromToken();
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound();
            if (nhanVien.MatKhau != req.MatKhauCu) // (Nên băm mật khẩu)
                return BadRequest("Mật khẩu cũ không chính xác.");
            nhanVien.MatKhau = req.MatKhauMoi;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        [HttpPost("submit-leave")]
        public async Task<IActionResult> SubmitLeaveRequest([FromBody] DonXinNghiRequestDto req)
        {
            // (Giữ nguyên logic từ câu trả lời trước)
            if (req.NgayKetThuc < req.NgayBatDau)
                return BadRequest("Ngày kết thúc không thể trước ngày bắt đầu.");
            var idNhanVien = GetIdNhanVienFromToken();
            var don = new DonXinNghi
            {
                IdNhanVien = idNhanVien,
                LoaiDon = req.LoaiDon,
                LyDo = req.LyDo,
                NgayBatDau = req.NgayBatDau,
                NgayKetThuc = req.NgayKetThuc,
                TrangThai = "Chờ duyệt",
            };
            _context.DonXinNghis.Add(don);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Gửi đơn xin nghỉ thành công!" });
        }
    }
}