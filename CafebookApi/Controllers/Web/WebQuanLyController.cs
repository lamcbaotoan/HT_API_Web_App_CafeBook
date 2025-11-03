using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/quanly")]
    [ApiController]
    public class WebQuanLyController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public WebQuanLyController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard/{idNhanVien}")]
        public async Task<IActionResult> GetEmployeeDashboard(int idNhanVien)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.IdNhanVien == idNhanVien);

            if (nhanVien == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            // Lấy ca làm việc hôm nay
            var caLamViec = await _context.LichLamViecs
                .Include(llv => llv.CaLamViec)
                .Where(llv => llv.IdNhanVien == idNhanVien && llv.NgayLam == DateTime.Today)
                .Select(llv => llv.CaLamViec.TenCa)
                .FirstOrDefaultAsync();

            // Lấy thống kê
            var banPhucVu = await _context.Bans.CountAsync(b => b.TrangThai == "Có khách");
            var donXuLy = await _context.HoaDons.CountAsync(h => h.TrangThai == "Chưa thanh toán" || h.TrangThai == "Đang giao");

            var dto = new WebQuanLyDashboardDto
            {
                HoTen = nhanVien.HoTen,
                VaiTro = nhanVien.VaiTro.TenVaiTro,
                AnhDaiDien = nhanVien.AnhDaiDien,
                CaHienTai = caLamViec ?? "Hôm nay nghỉ",
                TongBanDangPhucVu = banPhucVu,
                TongDonDangXuLy = donXuLy
            };

            return Ok(dto);
        }
    }
}