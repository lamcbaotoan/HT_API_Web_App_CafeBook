using CafebookApi.Data;
using CafebookModel.Model.ModelWeb.QuanLy; // SỬA: Dùng namespace mới
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web.QuanLy
{
    [Route("api/web/quanly/sodoban")]
    [ApiController]
    public class SoDoBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public SoDoBanController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API lấy tất cả Khu Vực và Bàn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSoDoBan()
        {
            // 1. Lấy tất cả hóa đơn chưa thanh toán (để lấy tổng tiền)
            // SỬA LỖI 500 (Duplicate Key): GroupBy và Sum
            var hoaDonChuaThanhToan = await _context.HoaDons
                .Where(h => h.TrangThai == "Chưa thanh toán" && h.IdBan != null)
                .GroupBy(h => h.IdBan) // Nhóm theo IdBan
                .Select(g => new
                {
                    IdBan = g.Key,
                    // SỬA LỖI CS1061: Dùng tên thuộc tính PascalCase (từ Entities.cs)
                    TongTien = g.Sum(h => h.TongTienGoc - h.GiamGia + h.TongPhuThu)
                })
                .ToDictionaryAsync(h => (int)h.IdBan!, h => h.TongTien);

            // 2. Lấy tất cả Khu Vực và Bàn
            var khuVucList = await _context.KhuVucs
                .Include(k => k.Bans) // Join Bàn
                .OrderBy(k => k.IdKhuVuc)
                .Select(k => new KhuVucDto
                {
                    IdKhuVuc = k.IdKhuVuc,
                    TenKhuVuc = k.TenKhuVuc,
                    Bans = k.Bans.OrderBy(b => b.SoBan).Select(b => new BanDto
                    {
                        IdBan = b.IdBan,
                        SoBan = b.SoBan,
                        TrangThai = b.TrangThai,
                        GhiChu = b.GhiChu, // <-- THÊM GHI CHÚ
                        // SỬA LỖI CS0019: 'b.IdKhuVuc' (int?) ?? 0
                        IdKhuVuc = b.IdKhuVuc ?? 0,
                        TongTien = (b.TrangThai == "Có khách")
                                   ? hoaDonChuaThanhToan.GetValueOrDefault(b.IdBan, 0)
                                   : 0
                    }).ToList()
                })
                .ToListAsync();

            return Ok(khuVucList);
        }
    }
}