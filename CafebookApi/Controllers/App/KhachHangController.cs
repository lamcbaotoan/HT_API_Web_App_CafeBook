using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/khachhang")]
    [ApiController]
    public class KhachHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public KhachHangController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API Lấy danh sách Khách hàng (có lọc/tìm kiếm)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchKhachHang(
            [FromQuery] string? searchText,
            [FromQuery] bool? biKhoa)
        {
            var query = _context.KhachHangs.AsQueryable();

            if (biKhoa.HasValue)
            {
                query = query.Where(kh => kh.BiKhoa == biKhoa.Value);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(kh =>
                    kh.HoTen.ToLower().Contains(searchLower) ||
                    (kh.SoDienThoai != null && kh.SoDienThoai.Contains(searchLower)) ||
                    (kh.Email != null && kh.Email.ToLower().Contains(searchLower))
                );
            }

            var results = await query
                .Select(kh => new KhachHangDto
                {
                    IdKhachHang = kh.IdKhachHang,
                    HoTen = kh.HoTen,
                    SoDienThoai = kh.SoDienThoai,
                    Email = kh.Email,
                    NgayTao = kh.NgayTao,
                    BiKhoa = kh.BiKhoa
                })
                .OrderBy(kh => kh.HoTen)
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// API Lấy chi tiết và lịch sử
        /// </summary>
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetKhachHangDetails(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            var dto = new KhachHangDetailDto
            {
                IdKhachHang = kh.IdKhachHang,
                HoTen = kh.HoTen,
                SoDienThoai = kh.SoDienThoai,
                Email = kh.Email,
                DiaChi = kh.DiaChi,
                DiemTichLuy = kh.DiemTichLuy,
                TenDangNhap = kh.TenDangNhap,
                BiKhoa = kh.BiKhoa,
                AnhDaiDienBase64 = kh.AnhDaiDien,

                LichSuDonHang = await _context.HoaDons
                    .Where(h => h.IdKhachHang == id)
                    .OrderByDescending(h => h.ThoiGianTao)
                    .Select(h => new LichSuDonHangDto
                    {
                        IdHoaDon = h.IdHoaDon,
                        ThoiGianTao = h.ThoiGianTao,
                        ThanhTien = h.ThanhTien,
                        TrangThai = h.TrangThai
                    }).ToListAsync(),

                LichSuThueSach = await _context.PhieuThueSachs
                    .Where(p => p.IdKhachHang == id)
                    .Include(p => p.ChiTietPhieuThues)
                        .ThenInclude(ct => ct.Sach) // Lấy Tên Sách
                    .OrderByDescending(p => p.NgayThue)
                    .SelectMany(p => p.ChiTietPhieuThues.Select(ct => new LichSuThueSachDto
                    {
                        IdPhieuThue = p.IdPhieuThueSach,
                        TieuDeSach = ct.Sach.TenSach,
                        NgayThue = p.NgayThue,
                        TrangThai = p.TrangThai
                    }))
                    .Take(20) // Lấy 20 lượt thuê gần nhất
                    .ToListAsync()
            };
            return Ok(dto);
        }

        /// <summary>
        /// API Thêm khách hàng mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateKhachHang([FromBody] KhachHangUpdateRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto.HoTen) || string.IsNullOrEmpty(dto.SoDienThoai))
            {
                return BadRequest("Họ tên và SĐT là bắt buộc.");
            }

            // Kiểm tra trùng SĐT
            if (await _context.KhachHangs.AnyAsync(kh => kh.SoDienThoai == dto.SoDienThoai))
            {
                return Conflict("Số điện thoại này đã tồn tại.");
            }
            // Kiểm tra trùng Email (nếu có)
            if (!string.IsNullOrEmpty(dto.Email) && await _context.KhachHangs.AnyAsync(kh => kh.Email == dto.Email))
            {
                return Conflict("Email này đã tồn tại.");
            }

            var khachHang = new KhachHang
            {
                HoTen = dto.HoTen,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                DiaChi = dto.DiaChi,
                TenDangNhap = dto.TenDangNhap,
                DiemTichLuy = 0,
                NgayTao = DateTime.Now,
                BiKhoa = false,
                AnhDaiDien = dto.AnhDaiDienBase64
            };
            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();
            return Ok(khachHang);
        }

        /// <summary>
        /// API Cập nhật khách hàng
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKhachHang(int id, [FromBody] KhachHangUpdateRequestDto dto)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            if (await _context.KhachHangs.AnyAsync(k => k.SoDienThoai == dto.SoDienThoai && k.IdKhachHang != id))
            {
                return Conflict("Số điện thoại này đã tồn tại.");
            }
            if (!string.IsNullOrEmpty(dto.Email) && await _context.KhachHangs.AnyAsync(k => k.Email == dto.Email && k.IdKhachHang != id))
            {
                return Conflict("Email này đã tồn tại.");
            }

            kh.HoTen = dto.HoTen;
            kh.SoDienThoai = dto.SoDienThoai;
            kh.Email = dto.Email;
            kh.DiaChi = dto.DiaChi;
            kh.TenDangNhap = dto.TenDangNhap;
            kh.DiemTichLuy = dto.DiemTichLuy;
            if (dto.AnhDaiDienBase64 != null) // Nếu "" là xóa ảnh, nếu khác là cập nhật
            {
                kh.AnhDaiDien = string.IsNullOrEmpty(dto.AnhDaiDienBase64) ? null : dto.AnhDaiDienBase64;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Khóa/Mở khóa tài khoản
        /// </summary>
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateKhachHangStatus(int id, [FromBody] bool biKhoa)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            kh.BiKhoa = biKhoa;
            // TODO: Ghi nhật ký hệ thống ở đây

            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa khách hàng
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKhachHang(int id)
        {
            // Kiểm tra ràng buộc
            if (await _context.HoaDons.AnyAsync(h => h.IdKhachHang == id))
            {
                return Conflict("Không thể xóa. Khách hàng này đã có lịch sử Hóa đơn.");
            }
            if (await _context.PhieuThueSachs.AnyAsync(p => p.IdKhachHang == id))
            {
                return Conflict("Không thể xóa. Khách hàng này đã có lịch sử Thuê sách.");
            }

            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound();

            _context.KhachHangs.Remove(kh);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}