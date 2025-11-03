using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/thongbao")]
    [ApiController]
    public class ThongBaoController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ThongBaoController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API đếm số thông báo chưa đọc
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _context.ThongBaos.CountAsync(t => !t.DaXem);
            return Ok(new ThongBaoCountDto { UnreadCount = count });
        }

        /// <summary>
        /// API lấy tất cả thông báo (mới nhất trước)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotifications()
        {
            var notifications = await _context.ThongBaos
                .Include(t => t.NhanVienTao) // Join để lấy tên NV
                .OrderByDescending(t => t.ThoiGianTao)
                .Take(20) // Chỉ lấy 20 thông báo mới nhất
                .Select(t => new ThongBaoDto
                {
                    IdThongBao = t.IdThongBao,
                    NoiDung = t.NoiDung,
                    ThoiGianTao = t.ThoiGianTao,
                    LoaiThongBao = t.LoaiThongBao,
                    IdLienQuan = t.IdLienQuan,
                    DaXem = t.DaXem,
                    TenNhanVienTao = t.NhanVienTao != null ? t.NhanVienTao.HoTen : "Hệ thống"
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// API đánh dấu 1 thông báo là đã đọc
        /// </summary>
        [HttpPost("mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var thongBao = await _context.ThongBaos.FindAsync(id);
            if (thongBao == null) return NotFound();

            if (!thongBao.DaXem)
            {
                thongBao.DaXem = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}