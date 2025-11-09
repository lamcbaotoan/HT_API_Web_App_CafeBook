// Tệp: CafebookApi/Controllers/App/NhanVien/PhieuLuongController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/phieuluong")]
    [ApiController]
    [Authorize]
    public class PhieuLuongController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public PhieuLuongController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách các phiếu lương đã chốt/đã phát của nhân viên
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetDanhSachPhieuLuong()
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                if (idNhanVien == 0) return Unauthorized();

                var list = await _context.PhieuLuongs
                    .Where(pl => pl.IdNhanVien == idNhanVien &&
                                 (pl.TrangThai == "Đã phát" || pl.TrangThai == "Đã chốt"))
                    .OrderByDescending(pl => pl.Nam)
                    .ThenByDescending(pl => pl.Thang)
                    .Select(pl => new PhieuLuongItemDto
                    {
                        IdPhieuLuong = pl.IdPhieuLuong,
                        Thang = pl.Thang,
                        Nam = pl.Nam,
                        ThucLanh = pl.ThucLanh,
                        TrangThai = pl.TrangThai
                    })
                    .ToListAsync();

                return Ok(new PhieuLuongViewDto { DanhSachPhieuLuong = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy chi tiết một phiếu lương
        /// </summary>
        [HttpGet("detail/{idPhieuLuong}")]
        public async Task<IActionResult> GetChiTietPhieuLuong(int idPhieuLuong)
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                if (idNhanVien == 0) return Unauthorized();

                var phieuLuong = await _context.PhieuLuongs
                    .Include(pl => pl.NguoiPhat)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pl => pl.IdPhieuLuong == idPhieuLuong && pl.IdNhanVien == idNhanVien);

                if (phieuLuong == null)
                {
                    return NotFound("Không tìm thấy phiếu lương hoặc bạn không có quyền xem.");
                }

                // *** SỬA LỖI: Thêm .Select() tường minh ***
                // Thay vì tải toàn bộ đối tượng PhieuThuongPhat (gây lỗi),
                // chúng ta chỉ chọn các trường cần thiết vào DTO.
                var chiTietThuongPhat = await _context.PhieuThuongPhats
                    .Where(ptp => ptp.IdPhieuLuong == idPhieuLuong)
                    .AsNoTracking()
                    .Select(ptp => new PhieuThuongPhatItemDto // Chọn thẳng ra DTO
                    {
                        NgayTao = ptp.NgayTao,
                        SoTien = ptp.SoTien,
                        LyDo = ptp.LyDo,
                        TenNguoiTao = ptp.NguoiTao.HoTen // EF sẽ tự động join
                    })
                    .ToListAsync();
                // *** KẾT THÚC SỬA LỖI ***

                // Xây dựng DTO trả về
                var chiTietDto = new PhieuLuongChiTietDto
                {
                    IdPhieuLuong = phieuLuong.IdPhieuLuong,
                    Thang = phieuLuong.Thang,
                    Nam = phieuLuong.Nam,
                    LuongCoBan = phieuLuong.LuongCoBan,
                    TongGioLam = phieuLuong.TongGioLam,
                    TienLuongTheoGio = phieuLuong.LuongCoBan * phieuLuong.TongGioLam,
                    TongTienThuong = phieuLuong.TienThuong ?? 0,
                    TongKhauTru = phieuLuong.KhauTru ?? 0,
                    ThucLanh = phieuLuong.ThucLanh,
                    TrangThai = phieuLuong.TrangThai,
                    NgayPhatLuong = phieuLuong.NgayPhatLuong,
                    TenNguoiPhat = phieuLuong.NguoiPhat?.HoTen,

                    // SỬA: Dùng danh sách DTO đã được tạo ở trên
                    DanhSachThuong = chiTietThuongPhat
                        .Where(ptp => ptp.SoTien > 0)
                        .ToList(),

                    DanhSachPhat = chiTietThuongPhat
                        .Where(ptp => ptp.SoTien < 0)
                        .ToList()
                };

                return Ok(chiTietDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        // === HÀM HELPER ===
        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int idNhanVien))
            {
                return idNhanVien;
            }
            return 0;
        }
    }
}