using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/donhang")]
    [ApiController]
    public class DonHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public DonHangController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API lấy dữ liệu cho các ComboBox lọc
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetDonHangFilters()
        {
            var dto = new DonHangFiltersDto
            {
                NhanViens = await _context.NhanViens
                    .Select(nv => new FilterLookupDto { Id = nv.IdNhanVien, Ten = nv.HoTen })
                    .OrderBy(nv => nv.Ten)
                    .ToListAsync(),
                KhachHangs = await _context.KhachHangs
                    .Select(kh => new FilterLookupDto { Id = kh.IdKhachHang, Ten = kh.HoTen })
                    .OrderBy(kh => kh.Ten)
                    .ToListAsync()
            };
            return Ok(dto);
        }

        /// <summary>
        /// API Lấy danh sách Đơn hàng (có lọc)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchDonHang(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? trangThai,
            [FromQuery] int? nhanVienId,
            [FromQuery] string? searchText)
        {
            var query = _context.HoaDons
                .Include(h => h.NhanVien)
                .Include(h => h.KhachHang)
                .Include(h => h.Ban)
                .Where(h => h.ThoiGianTao >= startDate && h.ThoiGianTao < endDate.AddDays(1))
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(h => h.TrangThai == trangThai);
            }

            if (nhanVienId.HasValue && nhanVienId > 0)
            {
                query = query.Where(h => h.IdNhanVien == nhanVienId);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(h =>
                    h.IdHoaDon.ToString().Contains(searchLower) ||
                    (h.KhachHang != null && h.KhachHang.HoTen.ToLower().Contains(searchLower)) ||
                    (h.KhachHang != null && h.KhachHang.SoDienThoai != null && h.KhachHang.SoDienThoai.Contains(searchLower))
                );
            }

            var results = await query
                .OrderByDescending(h => h.ThoiGianTao)
                .Select(h => new DonHangDto
                {
                    IdHoaDon = h.IdHoaDon,
                    ThoiGianTao = h.ThoiGianTao,
                    TenNhanVien = h.NhanVien.HoTen,
                    TenKhachHang = h.KhachHang != null ? h.KhachHang.HoTen : "Khách vãng lai",
                    SoBan = h.Ban != null ? h.Ban.SoBan : "Mang về/Giao",
                    ThanhTien = h.ThanhTien,
                    TrangThai = h.TrangThai,
                    LoaiHoaDon = h.LoaiHoaDon
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// API Lấy chi tiết một Đơn hàng
        /// </summary>
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDonHangDetails(int id)
        {
            var details = await _context.ChiTietHoaDons
                .Where(ct => ct.IdHoaDon == id)
                .Include(ct => ct.SanPham)
                .Select(ct => new DonHangChiTietDto
                {
                    TenSanPham = ct.SanPham.TenSanPham,
                    SoLuong = ct.SoLuong,
                    ThanhTien = ct.ThanhTien
                })
                .ToListAsync();

            return Ok(details);
        }

        /// <summary>
        /// API Cập nhật trạng thái (Hủy, Giao hàng)
        /// </summary>
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string newStatus)
        {
            var hoaDon = await _context.HoaDons.FindAsync(id);
            if (hoaDon == null) return NotFound();

            if (hoaDon.TrangThai == "Đã thanh toán")
            {
                return Conflict("Không thể cập nhật trạng thái cho hóa đơn đã thanh toán.");
            }

            // Cập nhật trạng thái chính
            if (newStatus == "Hủy")
            {
                hoaDon.TrangThai = "Đã hủy";
                // TODO: Logic hoàn kho (nếu cần)
            }
            // Cập nhật trạng thái giao hàng
            else if (newStatus == "Đang giao")
            {
                if (hoaDon.LoaiHoaDon != "Giao hàng")
                {
                    return BadRequest("Chỉ có thể giao hàng cho 'Đơn Giao Hàng'.");
                }
                if (string.IsNullOrEmpty(hoaDon.DiaChiGiaoHang) || string.IsNullOrEmpty(hoaDon.SoDienThoaiGiaoHang))
                {
                    return BadRequest("Không thể giao hàng. Thiếu địa chỉ hoặc SĐT.");
                }
                hoaDon.TrangThaiGiaoHang = "Đang giao";
            }
            else
            {
                return BadRequest("Trạng thái mới không hợp lệ.");
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}