// Tập tin: CafebookApi/Controllers/App/NhanVienController.cs

using CafebookApi.Data;
using CafebookModel.Model.Entities; // Giữ nguyên using này
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

// THÊM ALIAS NÀY ĐỂ TRÁNH XUNG ĐỘT
using NhanVienEntity = CafebookModel.Model.Entities.NhanVien;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/nhanvien")]
    [ApiController]
    public class NhanVienController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public NhanVienController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(_env.WebRootPath))
                {
                    Directory.CreateDirectory(_env.WebRootPath);
                }
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url")
                       ?? "http://127.0.0.1:5166";
        }

        // (Các hàm Helper xử lý file giữ nguyên)
        #region File Helpers
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private string GenerateFileName(int id, string ten)
        {
            string slug = SlugifyUtil.GenerateSlug(ten);
            return $"{id}_{slug}.jpg";
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private void DeleteOldImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fileName = relativePath.TrimStart('/');
            var fullPath = Path.Combine(_env.WebRootPath, fileName.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private void RenameImage(string oldRelativePath, string newRelativePath)
        {
            if (string.IsNullOrEmpty(oldRelativePath) || string.IsNullOrEmpty(newRelativePath) || oldRelativePath == newRelativePath)
                return;
            var oldFullPath = Path.Combine(_env.WebRootPath, oldRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var newFullPath = Path.Combine(_env.WebRootPath, newRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldFullPath))
            {
                System.IO.File.Move(oldFullPath, newFullPath);
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        private async Task<string> SaveImageFromFile(IFormFile file, string fileName, string relativeDir)
        {
            var saveDir = Path.Combine(_env.WebRootPath, relativeDir);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            var fullSavePath = Path.Combine(saveDir, fileName);
            await using (var stream = new FileStream(fullSavePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/{relativeDir.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        }
        #endregion

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var filters = new NhanSuFiltersDto
            {
                VaiTros = await _context.VaiTros
                    .Select(v => new FilterLookupDto { Id = v.IdVaiTro, Ten = v.TenVaiTro })
                    .OrderBy(v => v.Ten)
                    .ToListAsync()
            };
            return Ok(filters);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchNhanVien(
            [FromQuery] string? searchText,
            [FromQuery] int? vaiTroId)
        {
            // SỬA LỖI: Dùng NhanVienEntity
            var query = _context.NhanViens
                .Include(nv => nv.VaiTro)
                .AsQueryable();

            if (vaiTroId.HasValue && vaiTroId > 0)
            {
                query = query.Where(nv => nv.IdVaiTro == vaiTroId);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(nv =>
                    nv.HoTen.ToLower().Contains(searchLower) ||
                    nv.TenDangNhap.ToLower().Contains(searchLower)
                );
            }

            var results = await query
                .Select(nv => new NhanVienGridDto
                {
                    IdNhanVien = nv.IdNhanVien,
                    HoTen = nv.HoTen,
                    TenVaiTro = nv.VaiTro.TenVaiTro,
                    LuongCoBan = nv.LuongCoBan,
                    TrangThaiLamViec = nv.TrangThaiLamViec
                })
                .OrderBy(nv => nv.HoTen)
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            // SỬA LỖI: Dùng NhanVienEntity
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            var dto = new NhanVienDetailDto
            {
                IdNhanVien = nv.IdNhanVien,
                HoTen = nv.HoTen,
                TenDangNhap = nv.TenDangNhap,
                IdVaiTro = nv.IdVaiTro,
                LuongCoBan = nv.LuongCoBan,
                TrangThaiLamViec = nv.TrangThaiLamViec,
                SoDienThoai = nv.SoDienThoai,
                Email = nv.Email,
                DiaChi = nv.DiaChi,
                NgayVaoLam = nv.NgayVaoLam,
                AnhDaiDienUrl = GetFullImageUrl(nv.AnhDaiDien)
            };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNhanVien([FromForm] NhanVienUpdateRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
            {
                return BadRequest("Tên đăng nhập và Mật khẩu là bắt buộc khi tạo mới.");
            }

            // SỬA LỖI: Dùng NhanVienEntity
            if (await _context.NhanViens.AnyAsync(nv => nv.TenDangNhap.ToLower() == dto.TenDangNhap.ToLower()))
            {
                return Conflict("Tên đăng nhập đã tồn tại.");
            }

            // SỬA LỖI: Dùng NhanVienEntity
            var entity = new NhanVienEntity
            {
                HoTen = dto.HoTen,
                TenDangNhap = dto.TenDangNhap,
                MatKhau = dto.MatKhau,
                IdVaiTro = dto.IdVaiTro,
                LuongCoBan = dto.LuongCoBan,
                TrangThaiLamViec = dto.TrangThaiLamViec,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                DiaChi = dto.DiaChi,
                NgayVaoLam = dto.NgayVaoLam,
                AnhDaiDien = null
            };

            // SỬA LỖI: Dùng NhanVienEntity
            _context.NhanViens.Add(entity);
            await _context.SaveChangesAsync();

            if (dto.AnhDaiDienUpload != null)
            {
                try
                {
                    string fileName = GenerateFileName(entity.IdNhanVien, entity.HoTen);
                    string relativePath = await SaveImageFromFile(dto.AnhDaiDienUpload, fileName, HinhAnhPaths.UrlAvatarNV.TrimStart('/'));
                    entity.AnhDaiDien = relativePath;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi khi lưu ảnh: {ex.Message}");
                }
            }

            return Ok(entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNhanVien(int id, [FromForm] NhanVienUpdateRequestDto dto)
        {
            // SỬA LỖI: Dùng NhanVienEntity
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return NotFound();

            // SỬA LỖI: Dùng NhanVienEntity
            if (await _context.NhanViens.AnyAsync(nv => nv.TenDangNhap.ToLower() == dto.TenDangNhap.ToLower() && nv.IdNhanVien != id))
            {
                return Conflict("Tên đăng nhập đã tồn tại.");
            }

            // (Phần logic cập nhật file và thuộc tính giữ nguyên)
            var oldImagePath = entity.AnhDaiDien;
            var oldTen = entity.HoTen;
            bool isImageUpload = dto.AnhDaiDienUpload != null;
            bool isNameChange = dto.HoTen != oldTen;

            entity.HoTen = dto.HoTen;
            entity.TenDangNhap = dto.TenDangNhap;
            entity.IdVaiTro = dto.IdVaiTro;
            entity.LuongCoBan = dto.LuongCoBan;
            entity.TrangThaiLamViec = dto.TrangThaiLamViec;
            entity.SoDienThoai = dto.SoDienThoai;
            entity.Email = dto.Email;
            entity.DiaChi = dto.DiaChi;
            entity.NgayVaoLam = dto.NgayVaoLam;
            if (!string.IsNullOrWhiteSpace(dto.MatKhau))
            {
                entity.MatKhau = dto.MatKhau;
            }

            string newFileName = GenerateFileName(entity.IdNhanVien, entity.HoTen);
            string newRelativePath = $"/{HinhAnhPaths.UrlAvatarNV.TrimStart('/')}/{newFileName}";

            if (isImageUpload)
            {
                DeleteOldImage(oldImagePath);
                entity.AnhDaiDien = await SaveImageFromFile(dto.AnhDaiDienUpload, newFileName, HinhAnhPaths.UrlAvatarNV.TrimStart('/'));
            }
            else if (dto.XoaAnhDaiDien)
            {
                DeleteOldImage(oldImagePath);
                entity.AnhDaiDien = null;
            }
            else if (isNameChange && !string.IsNullOrEmpty(oldImagePath))
            {
                RenameImage(oldImagePath, newRelativePath);
                entity.AnhDaiDien = newRelativePath;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            // SỬA LỖI: Dùng NhanVienEntity
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TrangThaiLamViec = newStatus;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNhanVien(int id)
        {
            // (Logic kiểm tra ràng buộc giữ nguyên)
            if (await _context.LichLamViecs.AnyAsync(l => l.IdNhanVien == id) ||
                await _context.PhieuLuongs.AnyAsync(p => p.IdNhanVien == id) ||
                await _context.HoaDons.AnyAsync(h => h.IdNhanVien == id))
            {
                return Conflict("Không thể xóa. Nhân viên này đã có dữ liệu Lịch làm việc, Lương hoặc Hóa đơn. Vui lòng chọn 'Nghỉ việc'.");
            }

            // SỬA LỖI: Dùng NhanVienEntity
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return NotFound();

            DeleteOldImage(entity.AnhDaiDien);

            // SỬA LỖI: Dùng NhanVienEntity
            _context.NhanViens.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}