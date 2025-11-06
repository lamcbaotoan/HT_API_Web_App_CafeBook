using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // <-- THÊM

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
        /// THÊM MỚI: API để tải danh sách sản phẩm cho ComboBox
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetKhuyenMaiFilters()
        {
            var sanPhams = await _context.SanPhams
                .Where(sp => sp.TrangThaiKinhDoanh == true)
                .OrderBy(sp => sp.TenSanPham)
                .Select(sp => new FilterLookupDto { Id = sp.IdSanPham, Ten = sp.TenSanPham })
                .ToListAsync();

            return Ok(sanPhams);
        }

        /// <summary>
        /// SỬA: Cập nhật logic tìm kiếm
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchKhuyenMai(
            [FromQuery] string? maKhuyenMai,
            [FromQuery] string? trangThai)
        {
            var now = DateTime.Now;
            var today = now.Date;

            // 1. Tự động cập nhật các mã hết hạn
            var hetHan = await _context.KhuyenMais
                .Where(km => km.TrangThai != "Hết hạn" && km.NgayKetThuc < today)
                .ToListAsync();

            foreach (var km in hetHan)
            {
                km.TrangThai = "Hết hạn";
            }
            await _context.SaveChangesAsync();

            // 2. Bắt đầu truy vấn
            var query = _context.KhuyenMais.AsQueryable();

            if (!string.IsNullOrEmpty(maKhuyenMai))
            {
                query = query.Where(km => km.MaKhuyenMai.Contains(maKhuyenMai));
            }
            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(km => km.TrangThai == trangThai);
            }

            // 3. Select DTO
            var result = await query
                .OrderByDescending(km => km.IdKhuyenMai)
                .Select(km => new KhuyenMaiDto
                {
                    IdKhuyenMai = km.IdKhuyenMai,
                    MaKhuyenMai = km.MaKhuyenMai,
                    TenKhuyenMai = km.TenChuongTrinh,
                    GiaTriGiam = km.LoaiGiamGia == "PhanTram" ? $"{km.GiaTriGiam}%" : $"{km.GiaTriGiam:N0}đ",
                    GiamToiDa = km.GiamToiDa,
                    DieuKienApDung = km.DieuKienApDung, // <-- SỬA: Thêm
                    NgayBatDau = km.NgayBatDau,
                    NgayKetThuc = km.NgayKetThuc,
                    TrangThai = km.TrangThai,
                    SoLuongConLai = km.SoLuongConLai // <-- SỬA: Thêm
                })
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// SỬA: Trả về đầy đủ các trường
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetKhuyenMaiById(int id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
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
                GioBatDau = km.GioBatDau.HasValue ? km.GioBatDau.Value.ToString(@"hh\:mm") : null,
                GioKetThuc = km.GioKetThuc.HasValue ? km.GioKetThuc.Value.ToString(@"hh\:mm") : null,
                SoLuongConLai = km.SoLuongConLai,
                TrangThai = km.TrangThai,
                DieuKienApDung = km.DieuKienApDung
            };
            return Ok(dto);
        }

        /// <summary>
        /// SỬA: Xử lý đầy đủ các trường khi Tạo mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateKhuyenMai([FromBody] KhuyenMaiUpdateRequestDto dto)
        {
            if (await _context.KhuyenMais.AnyAsync(km => km.MaKhuyenMai == dto.MaKhuyenMai))
            {
                return BadRequest($"Mã khuyến mãi '{dto.MaKhuyenMai}' đã tồn tại.");
            }

            var km = new KhuyenMai
            {
                MaKhuyenMai = dto.MaKhuyenMai,
                TenChuongTrinh = dto.TenChuongTrinh,
                MoTa = dto.MoTa,
                LoaiGiamGia = dto.LoaiGiamGia,
                GiaTriGiam = dto.GiaTriGiam,
                GiamToiDa = (dto.LoaiGiamGia == "PhanTram" && dto.GiamToiDa > 0) ? dto.GiamToiDa : null,
                HoaDonToiThieu = (dto.HoaDonToiThieu > 0) ? dto.HoaDonToiThieu : null,
                IdSanPhamApDung = (dto.IdSanPhamApDung > 0) ? dto.IdSanPhamApDung : null,
                NgayBatDau = dto.NgayBatDau.Date,
                NgayKetThuc = dto.NgayKetThuc.Date,
                NgayTrongTuan = string.IsNullOrWhiteSpace(dto.NgayTrongTuan) ? null : dto.NgayTrongTuan,
                GioBatDau = TimeSpan.TryParse(dto.GioBatDau, out var tsStart) ? tsStart : null,
                GioKetThuc = TimeSpan.TryParse(dto.GioKetThuc, out var tsEnd) ? tsEnd : null,
                SoLuongConLai = (dto.SoLuongConLai > 0) ? dto.SoLuongConLai : null,
                TrangThai = dto.TrangThai, // Thường là "Hoạt động"
                DieuKienApDung = dto.DieuKienApDung
            };

            _context.KhuyenMais.Add(km);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetKhuyenMaiById), new { id = km.IdKhuyenMai }, km);
        }

        /// <summary>
        /// SỬA: Xử lý đầy đủ các trường khi Cập nhật
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKhuyenMai(int id, [FromBody] KhuyenMaiUpdateRequestDto dto)
        {
            if (id != dto.IdKhuyenMai) return BadRequest("ID không khớp.");

            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound();

            // Kiểm tra mã trùng lặp (nếu đổi mã)
            if (km.MaKhuyenMai != dto.MaKhuyenMai && await _context.KhuyenMais.AnyAsync(k => k.MaKhuyenMai == dto.MaKhuyenMai))
            {
                return BadRequest($"Mã khuyến mãi '{dto.MaKhuyenMai}' đã tồn tại.");
            }

            // Map tất cả các trường
            km.MaKhuyenMai = dto.MaKhuyenMai;
            km.TenChuongTrinh = dto.TenChuongTrinh;
            km.MoTa = dto.MoTa;
            km.LoaiGiamGia = dto.LoaiGiamGia;
            km.GiaTriGiam = dto.GiaTriGiam;
            km.GiamToiDa = (dto.LoaiGiamGia == "PhanTram" && dto.GiamToiDa > 0) ? dto.GiamToiDa : null;
            km.HoaDonToiThieu = (dto.HoaDonToiThieu > 0) ? dto.HoaDonToiThieu : null;
            km.IdSanPhamApDung = (dto.IdSanPhamApDung > 0) ? dto.IdSanPhamApDung : null;
            km.NgayBatDau = dto.NgayBatDau.Date;
            km.NgayKetThuc = dto.NgayKetThuc.Date;
            km.NgayTrongTuan = string.IsNullOrWhiteSpace(dto.NgayTrongTuan) ? null : dto.NgayTrongTuan;
            km.GioBatDau = TimeSpan.TryParse(dto.GioBatDau, out var tsStart) ? tsStart : null;
            km.GioKetThuc = TimeSpan.TryParse(dto.GioKetThuc, out var tsEnd) ? tsEnd : null;
            km.SoLuongConLai = (dto.SoLuongConLai > 0) ? dto.SoLuongConLai : null;
            km.DieuKienApDung = dto.DieuKienApDung;

            // Chỉ cập nhật trạng thái nếu nó không phải là "Hết hạn"
            if (km.TrangThai != "Hết hạn")
            {
                km.TrangThai = dto.TrangThai;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKhuyenMai(int id)
        {
            if (await _context.HoaDonKhuyenMais.AnyAsync(hkm => hkm.IdKhuyenMai == id))
            {
                return BadRequest("Không thể xóa khuyến mãi đã được áp dụng cho hóa đơn.");
            }

            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound();

            _context.KhuyenMais.Remove(km);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("togglestatus/{id}")]
        public async Task<IActionResult> ToggleKhuyenMaiStatus(int id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound();

            if (km.TrangThai == "Hết hạn")
            {
                return BadRequest("Không thể kích hoạt khuyến mãi đã hết hạn.");
            }

            km.TrangThai = (km.TrangThai == "Hoạt động") ? "Tạm dừng" : "Hoạt động";
            await _context.SaveChangesAsync();
            return Ok(km.TrangThai);
        }
    }
}