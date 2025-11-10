using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb; // Dùng ThueSachRequestDto
using CafebookModel.Model.ModelApp.NhanVien; // Dùng CaiDatThueSachDto
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims; // Cần để lấy ID người dùng
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/thuesach")]
    [ApiController]
    [Authorize(Roles = "KhachHang")] // YÊU CẦU: Phải đăng nhập với vai trò KhachHang
    public class ThueSachWebController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ThueSachWebController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API cho khách hàng đã đăng nhập tự thuê sách
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePhieuThue([FromBody] ThueSachRequestDto dto)
        {
            // Tự động lấy ID Khách hàng từ token/cookie đã đăng nhập
            var khachHangIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(khachHangIdString, out int khachHangId))
            {
                return Unauthorized("Không thể xác định thông tin khách hàng.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var settings = await GetSettingsInternal(); // Lấy cài đặt

                // 1. Kiểm tra Sách
                var sach = await _context.Sachs.FindAsync(dto.IdSach);
                if (sach == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound("Không tìm thấy sách.");
                }
                if (sach.SoLuongHienCo <= 0)
                {
                    await transaction.RollbackAsync();
                    return Conflict("Sách này hiện đã được thuê hết.");
                }

                // 2. Kiểm tra xem khách hàng này đã thuê sách này mà chưa trả?
                bool daThueChuaTra = await _context.ChiTietPhieuThues
                    .AnyAsync(ct => ct.PhieuThueSach.IdKhachHang == khachHangId &&
                                    ct.IdSach == dto.IdSach &&
                                    ct.NgayTraThucTe == null);

                if (daThueChuaTra)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Bạn đã thuê cuốn sách này và chưa trả.");
                }

                // 3. Tạo Phiếu Thuê
                DateTime ngayThue = DateTime.Now;
                DateTime ngayHenTra = ngayThue.AddDays(settings.SoNgayMuonToiDa);
                decimal tienCoc = sach.GiaBia ?? 0;

                var phieuThue = new PhieuThueSach
                {
                    IdKhachHang = khachHangId,
                    // === SỬA LỖI CS0037 ===
                    // Cột IdNhanVien của bạn không cho phép NULL.
                    // Gán ID của một nhân viên "Hệ Thống" (ví dụ ID = 1)
                    IdNhanVien = 1, // TODO: Thay = ID Nhân viên "Hệ Thống Web" của bạn
                    NgayThue = ngayThue,
                    TrangThai = "Đang Thuê",
                    TongTienCoc = tienCoc
                };
                _context.PhieuThueSachs.Add(phieuThue);
                await _context.SaveChangesAsync(); // Cần lưu để lấy IdPhieuThueSach

                // 4. Tạo Chi Tiết Phiếu Thuê
                var chiTiet = new ChiTietPhieuThue
                {
                    IdPhieuThueSach = phieuThue.IdPhieuThueSach,
                    IdSach = sach.IdSach,
                    NgayHenTra = ngayHenTra.Date, // Chỉ lấy ngày
                    TienCoc = tienCoc,
                    TienPhatTraTre = null,
                    NgayTraThucTe = null
                };
                _context.ChiTietPhieuThues.Add(chiTiet);

                // 5. Cập nhật số lượng sách
                sach.SoLuongHienCo--;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new ThueSachResponseDto
                {
                    IdPhieuThueSach = phieuThue.IdPhieuThueSach,
                    TenSach = sach.TenSach,
                    NgayThue = phieuThue.NgayThue,
                    NgayHenTra = ngayHenTra.Date,
                    TongTienCoc = phieuThue.TongTienCoc
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper: Lấy cài đặt
        /// </summary>
        private async Task<CaiDatThueSachDto> GetSettingsInternal()
        {
            var setting = await _context.CaiDats
                .FirstOrDefaultAsync(c => c.TenCaiDat == "Sach_SoNgayMuonToiDa");

            var settingsDto = new CaiDatThueSachDto();
            settingsDto.SoNgayMuonToiDa = int.Parse(setting?.GiaTri ?? "7");

            return settingsDto;
        }
    }
}