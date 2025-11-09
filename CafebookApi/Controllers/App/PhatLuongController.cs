// Tệp: CafebookApi/Controllers/App/PhatLuongController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/phatluong")]
    [ApiController]
    public class PhatLuongController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public PhatLuongController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách các phiếu lương đã chốt hoặc đã phát theo kỳ
        /// </summary>
        [HttpGet("danhsach")]
        public async Task<IActionResult> GetDanhSachPhieuLuong([FromQuery] int thang, [FromQuery] int nam)
        {
            var query = _context.PhieuLuongs
                .Include(p => p.NhanVien)
                .Where(p => p.Thang == thang && p.Nam == nam &&
                             (p.TrangThai == "Đã chốt" || p.TrangThai == "Đã phát"))
                .OrderByDescending(p => p.TrangThai)
                .ThenBy(p => p.NhanVien.HoTen);

            var result = await query.Select(p => new PhieuLuongDto
            {
                IdPhieuLuong = p.IdPhieuLuong,
                IdNhanVien = p.IdNhanVien,
                HoTenNhanVien = p.NhanVien.HoTen,
                Thang = p.Thang,
                Nam = p.Nam,
                TongGioLam = p.TongGioLam,
                ThucLanh = p.ThucLanh,
                NgayChot = p.NgayTao,
                TrangThai = p.TrangThai
            }).ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết 1 phiếu lương để xem hoặc in
        /// </summary>
        [HttpGet("chitiet/{id}")]
        public async Task<IActionResult> GetChiTietPhieuLuong(int id)
        {
            // *** SỬA LỖI: Sử dụng đúng DTO và xóa logic thừa ***
            // Dùng DTO của ModelApp (Quản lý)
            var phieu = await _context.PhieuLuongs
                .Include(p => p.NhanVien)
                .Include(p => p.NguoiPhat)
                .Where(p => p.IdPhieuLuong == id)
                .Select(p => new CafebookModel.Model.ModelApp.PhieuLuongChiTietDto // Chỉ định rõ DTO để hết lỗi CS0104
                {
                    IdPhieuLuong = p.IdPhieuLuong,
                    HoTenNhanVien = p.NhanVien.HoTen,
                    Thang = p.Thang,
                    Nam = p.Nam,
                    LuongCoBan = p.LuongCoBan,
                    TongGioLam = p.TongGioLam,
                    TienThuong = p.TienThuong ?? 0,
                    KhauTru = p.KhauTru ?? 0,
                    ThucLanh = p.ThucLanh,
                    TrangThai = p.TrangThai,
                    NgayPhat = p.NgayPhatLuong,
                    TenNguoiPhat = p.NguoiPhat != null ? p.NguoiPhat.HoTen : null
                })
                .FirstOrDefaultAsync();
            // *** KẾT THÚC SỬA LỖI ***

            if (phieu == null)
            {
                return NotFound("Không tìm thấy phiếu lương.");
            }

            // (Đã xóa logic truy vấn PhieuThuongPhats vì không cần thiết cho màn hình này)

            return Ok(phieu);
        }

        /// <summary>
        /// Xác nhận đã phát lương cho 1 phiếu lương
        /// </summary>
        [HttpPut("xacnhan/{id}")]
        public async Task<IActionResult> XacNhanPhatLuong(int id, [FromBody] PhatLuongXacNhanDto dto)
        {
            var phieu = await _context.PhieuLuongs.FindAsync(id);

            if (phieu == null)
            {
                return NotFound("Không tìm thấy phiếu lương.");
            }

            if (phieu.TrangThai == "Đã phát")
            {
                return BadRequest("Phiếu lương này đã được xác nhận phát trước đó.");
            }

            phieu.TrangThai = "Đã phát";
            phieu.NgayPhatLuong = DateTime.Now;
            phieu.IdNguoiPhat = dto.IdNguoiPhat;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác nhận phát lương thành công." });
        }
    }
}