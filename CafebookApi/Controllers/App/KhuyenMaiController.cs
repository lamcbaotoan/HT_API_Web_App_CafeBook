using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks; // <--- Đảm bảo bạn CÓ 'using' này

namespace CafebookApi.Controllers.App
{
    [Route("api/app/khuyenmai")]
    [ApiController]
    public class KhuyenMaiController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public KhuyenMaiController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// SỬA LỖI: Thêm 'async Task<...>'
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchKhuyenMai( // <-- SỬA LỖI (Thêm async Task)
            [FromQuery] string? maKhuyenMai,
            [FromQuery] string? trangThai)
        {
            var now = DateTime.Now;
            var today = now.Date;

            // Dòng 36: 'await' bây giờ đã hợp lệ
            var hetHan = await _context.KhuyenMais
                .Where(km => km.TrangThai != "Hết hạn" && km.NgayKetThuc < today)
                .ToListAsync();

            foreach (var km in hetHan)
            {
                km.TrangThai = "Hết hạn";
            }

            // Dòng 44: 'await' bây giờ đã hợp lệ
            await _context.SaveChangesAsync();

            var query = _context.KhuyenMais.AsQueryable();

            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(km => km.TrangThai == trangThai);
            }

            if (!string.IsNullOrEmpty(maKhuyenMai))
            {
                query = query.Where(km => km.MaKhuyenMai.Contains(maKhuyenMai));
            }

            // Dòng 61: 'await' bây giờ đã hợp lệ
            var results = await query
                .OrderByDescending(km => km.NgayBatDau)
                .Select(km => new KhuyenMaiDto
                {
                    IdKhuyenMai = km.IdKhuyenMai,
                    MaKhuyenMai = km.MaKhuyenMai,
                    TenKhuyenMai = km.TenChuongTrinh,
                    GiaTriGiam = km.LoaiGiamGia == "PhanTram"
                                 ? km.GiaTriGiam.ToString("F0") + "%"
                                 : km.GiaTriGiam.ToString("N0"),
                    GiamToiDa = km.GiamToiDa,
                    DieuKienApDung = km.DieuKienApDung,
                    NgayBatDau = km.NgayBatDau,
                    NgayKetThuc = km.NgayKetThuc,
                    TrangThai = km.TrangThai
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// SỬA LỖI: Thêm 'async Task<...>'
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters() // <-- SỬA LỖI (Thêm async Task)
        {
            var filters = new KhuyenMaiFiltersDto
            {
                SanPhams = await _context.SanPhams // 'await' hợp lệ
                    .Where(s => s.TrangThaiKinhDoanh)
                    .Select(s => new FilterLookupDto { Id = s.IdSanPham, Ten = s.TenSanPham })
                    .OrderBy(s => s.Ten)
                    .ToListAsync()
            };
            return Ok(filters);
        }

        /// <summary>
        /// SỬA LỖI: Thêm 'async Task<...>'
        /// </summary>
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetKhuyenMaiDetails(int id) // <-- SỬA LỖI (Thêm async Task)
        {
            var km = await _context.KhuyenMais.FindAsync(id); // 'await' hợp lệ
            if (km == null) return NotFound();

            var dto = new KhuyenMaiUpdateRequestDto
            {
                IdKhuyenMai = km.IdKhuyenMai,
                MaKhuyenMai = km.MaKhuyenMai,
                TenChuongTrinh = km.TenChuongTrinh,
                MoTa = km.MoTa,
                LoaiGiamGia = km.LoaiGiamGia,
                GiaTriGiam = km.GiaTriGiam,
                GiamToiDa = km.GiamToiDa,
                HoaDonToiThieu = km.HoaDonToiThieu,
                IdSanPhamApDung = km.IdSanPhamApDung,
                NgayBatDau = km.NgayBatDau,
                NgayKetThuc = km.NgayKetThuc,
                NgayTrongTuan = km.NgayTrongTuan,
                GioBatDau = km.GioBatDau?.ToString(@"hh\:mm"),
                GioKetThuc = km.GioKetThuc?.ToString(@"hh\:mm"),
                DieuKienApDung = km.DieuKienApDung,
                TrangThai = km.TrangThai
            };
            return Ok(dto);
        }

        /// <summary>
        /// SỬA LỖI: Thêm 'async Task<...>'
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateKhuyenMai([FromBody] KhuyenMaiUpdateRequestDto dto) // <-- SỬA LỖI (Thêm async Task)
        {
            if (string.IsNullOrWhiteSpace(dto.MaKhuyenMai))
            {
                return BadRequest("Mã khuyến mãi là bắt buộc.");
            }
            if (await _context.KhuyenMais.AnyAsync(km => km.MaKhuyenMai.ToLower() == dto.MaKhuyenMai.ToLower())) // 'await' hợp lệ
            {
                return Conflict("Mã khuyến mãi này đã tồn tại.");
            }

            var km = new KhuyenMai
            {
                MaKhuyenMai = dto.MaKhuyenMai.ToUpper(),
                TenChuongTrinh = dto.TenChuongTrinh,
                MoTa = dto.MoTa,
                LoaiGiamGia = dto.LoaiGiamGia,
                GiaTriGiam = dto.GiaTriGiam,
                GiamToiDa = dto.GiamToiDa,
                HoaDonToiThieu = dto.HoaDonToiThieu,
                IdSanPhamApDung = dto.IdSanPhamApDung,
                NgayBatDau = dto.NgayBatDau.Date,
                NgayKetThuc = dto.NgayKetThuc.Date,
                NgayTrongTuan = dto.NgayTrongTuan,
                GioBatDau = TimeSpan.TryParse(dto.GioBatDau, out var tsStart) ? tsStart : null,
                GioKetThuc = TimeSpan.TryParse(dto.GioKetThuc, out var tsEnd) ? tsEnd : null,
                DieuKienApDung = dto.DieuKienApDung,
                TrangThai = "Hoạt động"
            };

            _context.KhuyenMais.Add(km);
            await _context.SaveChangesAsync(); // 'await' hợp lệ
            return Ok(km);
        }

        /// <summary>
        /// SỬA LỖI: Thêm 'async Task<...>'
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKhuyenMai(int id, [FromBody] KhuyenMaiUpdateRequestDto dto) // <-- SỬA LỖI (Thêm async Task)
        {
            var km = await _context.KhuyenMais.FindAsync(id); // 'await' hợp lệ
            if (km == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.MaKhuyenMai))
            {
                return BadRequest("Mã khuyến mãi là bắt buộc.");
            }
            if (await _context.KhuyenMais.AnyAsync(k => k.MaKhuyenMai.ToLower() == dto.MaKhuyenMai.ToLower() && k.IdKhuyenMai != id)) // 'await' hợp lệ
            {
                return Conflict("Mã khuyến mãi này đã tồn tại.");
            }

            km.MaKhuyenMai = dto.MaKhuyenMai.ToUpper();
            km.TenChuongTrinh = dto.TenChuongTrinh;
            km.MoTa = dto.MoTa;
            km.LoaiGiamGia = dto.LoaiGiamGia;
            km.GiaTriGiam = dto.GiaTriGiam;
            km.GiamToiDa = dto.GiamToiDa;
            km.HoaDonToiThieu = dto.HoaDonToiThieu;
            km.IdSanPhamApDung = dto.IdSanPhamApDung;
            km.NgayBatDau = dto.NgayBatDau.Date;
            km.NgayKetThuc = dto.NgayKetThuc.Date;
            km.NgayTrongTuan = dto.NgayTrongTuan;
            km.GioBatDau = TimeSpan.TryParse(dto.GioBatDau, out var tsStart) ? tsStart : null;
            km.GioKetThuc = TimeSpan.TryParse(dto.GioKetThuc, out var tsEnd) ? tsEnd : null;
            km.DieuKienApDung = dto.DieuKienApDung;

            if (km.TrangThai != "Hết hạn")
            {
                km.TrangThai = dto.TrangThai;
            }

            await _context.SaveChangesAsync(); // 'await' hợp lệ
            return Ok();
        }

        /// <summary>
        /// SỬA LỖI: Thêm 'async Task<...>'
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKhuyenMai(int id) // <-- SỬA LỖI (Thêm async Task)
        {
            if (await _context.HoaDonKhuyenMais.AnyAsync(hkm => hkm.IdKhuyenMai == id)) // 'await' hợp lệ
            {
                return Conflict("Không thể xóa. Khuyến mãi này đã được áp dụng cho hóa đơn.");
            }

            var km = await _context.KhuyenMais.FindAsync(id); // 'await' hợp lệ
            if (km == null) return NotFound();

            _context.KhuyenMais.Remove(km);
            await _context.SaveChangesAsync(); // 'await' hợp lệ
            return Ok();
        }
    }
}