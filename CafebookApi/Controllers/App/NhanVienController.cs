using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/nhanvien")]
    [ApiController]
    public class NhanVienController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public NhanVienController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy bộ lọc (Vai Trò)
        /// </summary>
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

        /// <summary>
        /// API Lấy danh sách Nhân Viên (có lọc/tìm kiếm)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchNhanVien(
            [FromQuery] string? searchText,
            [FromQuery] int? vaiTroId)
        {
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

        /// <summary>
        /// API Lấy chi tiết 1 Nhân viên
        /// </summary>
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            var dto = new NhanVienUpdateRequestDto
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
                AnhDaiDienBase64 = nv.AnhDaiDien
                // Mật khẩu không được gửi về
            };
            return Ok(dto);
        }

        /// <summary>
        /// API Thêm mới Nhân viên
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNhanVien([FromBody] NhanVienUpdateRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
            {
                return BadRequest("Tên đăng nhập và Mật khẩu là bắt buộc khi tạo mới.");
            }

            if (await _context.NhanViens.AnyAsync(nv => nv.TenDangNhap.ToLower() == dto.TenDangNhap.ToLower()))
            {
                return Conflict("Tên đăng nhập đã tồn tại.");
            }

            var entity = new NhanVien
            {
                HoTen = dto.HoTen,
                TenDangNhap = dto.TenDangNhap,
                MatKhau = dto.MatKhau, // (Lưu ý: Đang lưu plain text theo cấu trúc cũ)
                IdVaiTro = dto.IdVaiTro,
                LuongCoBan = dto.LuongCoBan,
                TrangThaiLamViec = dto.TrangThaiLamViec,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                DiaChi = dto.DiaChi,
                NgayVaoLam = dto.NgayVaoLam,
                AnhDaiDien = dto.AnhDaiDienBase64
            };

            _context.NhanViens.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        /// <summary>
        /// API Cập nhật Nhân viên
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNhanVien(int id, [FromBody] NhanVienUpdateRequestDto dto)
        {
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return NotFound();

            if (await _context.NhanViens.AnyAsync(nv => nv.TenDangNhap.ToLower() == dto.TenDangNhap.ToLower() && nv.IdNhanVien != id))
            {
                return Conflict("Tên đăng nhập đã tồn tại.");
            }

            entity.HoTen = dto.HoTen;
            entity.TenDangNhap = dto.TenDangNhap;
            entity.IdVaiTro = dto.IdVaiTro;
            entity.LuongCoBan = dto.LuongCoBan;
            entity.TrangThaiLamViec = dto.TrangThaiLamViec;
            entity.SoDienThoai = dto.SoDienThoai;
            entity.Email = dto.Email;
            entity.DiaChi = dto.DiaChi;
            entity.NgayVaoLam = dto.NgayVaoLam;

            if (dto.AnhDaiDienBase64 != null) // Nếu "" là xóa, nếu có text là cập nhật
            {
                entity.AnhDaiDien = string.IsNullOrEmpty(dto.AnhDaiDienBase64) ? null : dto.AnhDaiDienBase64;
            }
            // Chỉ cập nhật mật khẩu nếu có gửi mật khẩu mới
            if (!string.IsNullOrWhiteSpace(dto.MatKhau))
            {
                entity.MatKhau = dto.MatKhau;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Cập nhật trạng thái (Ngưng hoạt động)
        /// </summary>
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TrangThaiLamViec = newStatus;
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa Nhân viên (Logic kiểm tra)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNhanVien(int id)
        {
            // Kiểm tra ràng buộc
            if (await _context.LichLamViecs.AnyAsync(l => l.IdNhanVien == id) ||
                await _context.PhieuLuongs.AnyAsync(p => p.IdNhanVien == id) ||
                await _context.HoaDons.AnyAsync(h => h.IdNhanVien == id))
            {
                return Conflict("Không thể xóa. Nhân viên này đã có dữ liệu Lịch làm việc, Lương hoặc Hóa đơn. Vui lòng chọn 'Nghỉ việc'.");
            }

            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return NotFound();

            _context.NhanViens.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}