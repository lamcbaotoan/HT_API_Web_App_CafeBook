using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/chamcong")]
    [ApiController]
    [Authorize] // Yêu cầu nhân viên phải đăng nhập
    public class ChamCongController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ChamCongController(CafebookDbContext context)
        {
            _context = context;
        }

        // Helper: Lấy ID Nhân viên từ token
        private int GetIdNhanVienFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int id)) return id;
            throw new UnauthorizedAccessException("Không thể xác thực nhân viên.");
        }

        /// <summary>
        /// Xử lý Check-in hoặc Check-out cho nhân viên
        /// </summary>
        [HttpPost("check-in-out")]
        public async Task<IActionResult> CheckInCheckOut()
        {
            try
            {
                var idNhanVien = GetIdNhanVienFromToken();
                var thoiGianHienTai = DateTime.Now;
                var ngayHomNay = thoiGianHienTai.Date;

                // 1. Tìm lịch làm việc của nhân viên trong ngày hôm nay
                var lichLamViec = await _context.LichLamViecs
                    .AsNoTracking() // Không cần theo dõi vì chỉ đọc
                    .FirstOrDefaultAsync(l => l.IdNhanVien == idNhanVien && l.NgayLam == ngayHomNay);

                if (lichLamViec == null)
                {
                    return BadRequest("Bạn không có lịch làm việc hôm nay.");
                }

                // 2. Tìm bản ghi chấm công tương ứng với lịch làm việc
                var chamCongHomNay = await _context.BangChamCongs
                    .FirstOrDefaultAsync(c => c.IdLichLamViec == lichLamViec.IdLichLamViec);

                // 3. Xử lý logic Check-in / Check-out
                if (chamCongHomNay == null)
                {
                    // ----- CHECK-IN -----
                    // Chưa có bản ghi, tạo mới (đây là lần check-in)
                    var newChamCong = new BangChamCong
                    {
                        IdLichLamViec = lichLamViec.IdLichLamViec,
                        GioVao = thoiGianHienTai,
                        GioRa = null // Chưa check-out
                    };

                    _context.BangChamCongs.Add(newChamCong);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = $"Check-in thành công lúc {thoiGianHienTai:HH:mm}" });
                }
                else if (chamCongHomNay.GioRa == null)
                {
                    // ----- CHECK-OUT -----
                    // Đã check-in, giờ là check-out
                    chamCongHomNay.GioRa = thoiGianHienTai;
                    await _context.SaveChangesAsync();

                    return Ok(new { message = $"Check-out thành công lúc {thoiGianHienTai:HH:mm}" });
                }
                else
                {
                    // ----- ĐÃ HOÀN THÀNH -----
                    // Đã check-in và check-out
                    return BadRequest("Bạn đã hoàn thành chấm công hôm nay.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }
    }
}