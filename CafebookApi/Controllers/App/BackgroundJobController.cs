// Tệp: CafebookApi/Controllers/App/BackgroundJobController.cs
// (*** TẠO TỆP MỚI NÀY ***)

using CafebookApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/background")]
    [ApiController]
    public class BackgroundJobController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public BackgroundJobController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API CHUYÊN DỤNG: Tự động huỷ các phiếu đặt trễ 15 phút
        /// (Được gọi bởi SoDoBanController và DatBanController)
        /// </summary>
        [HttpPost("auto-cancel-late")]
        public async Task<IActionResult> AutoCancelLateReservationsAsync()
        {
            var now = DateTime.Now;
            // Mốc thời gian (bất kỳ phiếu nào trước 15 phút)
            var timeLimit = now.AddMinutes(-15);

            try
            {
                var lateReservations = await _context.PhieuDatBans
                    .Include(p => p.Ban)
                    .Where(p => (p.TrangThai == "Đã đặt" || p.TrangThai == "Chờ xác nhận") &&
                                p.ThoiGianDat < timeLimit)
                    .ToListAsync();

                if (lateReservations.Any())
                {
                    foreach (var phieu in lateReservations)
                    {
                        phieu.TrangThai = "Đã hủy";
                        phieu.GhiChu = (phieu.GhiChu ?? "") + " (Tự động hủy do trễ 15 phút)";

                        // Chỉ reset bàn nếu bàn đang ở trạng thái "Đã đặt"
                        if (phieu.Ban != null && phieu.Ban.TrangThai == "Đã đặt")
                        {
                            phieu.Ban.TrangThai = "Trống";
                        }
                    }
                    await _context.SaveChangesAsync();
                    return Ok(new { message = $"Đã tự động hủy {lateReservations.Count} phiếu bị trễ." });
                }
                return Ok(new { message = "Không có phiếu nào bị trễ." });
            }
            catch (Exception ex)
            {
                // Ghi lại lỗi nếu có sự cố
                Console.WriteLine($"[AutoCancelLateReservationsAsync Error]: {ex.Message}");
                return StatusCode(500, "Lỗi máy chủ khi xử lý hủy phiếu tự động.");
            }
        }
    }
}