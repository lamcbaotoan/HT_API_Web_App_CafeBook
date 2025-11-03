using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Model.Entities; // Thêm
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/sodoban")]
    [ApiController]
    public class SoDoBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public SoDoBanController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API chính: Lấy trạng thái của tất cả các bàn
        /// </summary>
        [HttpGet("tables")]
        public async Task<IActionResult> GetSoDoBan()
        {
            // Dùng LINQ để truy vấn trạng thái và hóa đơn chưa thanh toán
            var data = await _context.Bans
                .AsNoTracking()
                .Select(b => new BanSoDoDto
                {
                    IdBan = b.IdBan,
                    SoBan = b.SoBan,
                    TrangThai = b.TrangThai,
                    GhiChu = b.GhiChu,

                    // Tìm hóa đơn 'Chưa thanh toán' tương ứng
                    IdHoaDonHienTai = _context.HoaDons
                                      .Where(h => h.IdBan == b.IdBan && h.TrangThai == "Chưa thanh toán")
                                      .Select(h => (int?)h.IdHoaDon)
                                      .FirstOrDefault(),

                    TongTienHienTai = _context.HoaDons
                                      .Where(h => h.IdBan == b.IdBan && h.TrangThai == "Chưa thanh toán")
                                      .Select(h => h.ThanhTien) // 'thanhTien' là cột computed
                                      .FirstOrDefault()
                })
                .OrderBy(b => b.SoBan) // Sắp xếp theo tên bàn
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// API tạo một Hóa đơn mới cho bàn Trống
        /// </summary>
        [HttpPost("createorder/{idBan}/{idNhanVien}")]
        public async Task<IActionResult> CreateOrder(int idBan, int idNhanVien)
        {
            var ban = await _context.Bans.FindAsync(idBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");
            if (ban.TrangThai != "Trống" && ban.TrangThai != "Đã đặt")
                return Conflict("Bàn này đang bận hoặc đang bảo trì.");

            // Kiểm tra xem nhân viên có tồn tại không
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound("Nhân viên không hợp lệ.");

            var hoaDon = new HoaDon
            {
                IdBan = idBan,
                IdNhanVien = idNhanVien,
                TrangThai = "Chưa thanh toán",
                LoaiHoaDon = "Tại quán",
                ThoiGianTao = DateTime.Now
            };

            _context.HoaDons.Add(hoaDon);

            // Cập nhật trạng thái bàn
            ban.TrangThai = "Có khách";

            await _context.SaveChangesAsync();

            // Trả về Hóa đơn vừa tạo
            return Ok(new { idHoaDon = hoaDon.IdHoaDon });
        }

        /// <summary>
        /// API báo cáo sự cố (Khóa bàn)
        /// </summary>
        [HttpPost("reportproblem/{idBan}/{idNhanVien}")] // Thêm idNhanVien
        public async Task<IActionResult> BaoCaoSuCo(int idBan, int idNhanVien, [FromBody] BaoCaoSuCoRequestDto request)
        {
            var ban = await _context.Bans.FindAsync(idBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");

            if (ban.TrangThai == "Có khách")
                return Conflict("Không thể báo cáo sự cố bàn đang có khách.");

            ban.TrangThai = "Bảo trì";
            ban.GhiChu = $"[Sự cố NV báo]: {request.GhiChuSuCo}";

            // --- NÂNG CẤP: TẠO THÔNG BÁO MỚI ---
            var thongBao = new ThongBao
            {
                IdNhanVienTao = idNhanVien,
                NoiDung = $"Bàn {ban.SoBan} vừa được báo cáo sự cố: {request.GhiChuSuCo}",
                LoaiThongBao = "SuCoBan",
                IdLienQuan = idBan,
                ThoiGianTao = DateTime.Now,
                DaXem = false
            };
            _context.ThongBaos.Add(thongBao);
            // --- KẾT THÚC NÂNG CẤP ---

            await _context.SaveChangesAsync();
            return Ok(new { message = "Báo cáo sự cố thành công. Bàn đã được khóa." });
        }
    }
}