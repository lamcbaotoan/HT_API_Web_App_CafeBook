using CafebookApi.Data;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using CafebookModel.Model.ModelApp.NhanVien;

namespace CafebookApi.Controllers.App.NhanVien
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

        // (Các hàm GetSoDoBan, CreateOrder, BaoCaoSuCo, CreateOrderNoTable giữ nguyên)
        [HttpGet("tables")]
        public async Task<IActionResult> GetSoDoBan()
        {
            var data = await _context.Bans
               .AsNoTracking()
               .Select(b => new BanSoDoDto
               {
                   IdBan = b.IdBan,
                   SoBan = b.SoBan,
                   TrangThai = b.TrangThai,
                   GhiChu = b.GhiChu,
                   IdKhuVuc = b.IdKhuVuc,
                   IdHoaDonHienTai = _context.HoaDons
                                       .Where(h => h.IdBan == b.IdBan && h.TrangThai == "Chưa thanh toán")
                                       .Select(h => (int?)h.IdHoaDon)
                                       .FirstOrDefault(),
                   TongTienHienTai = _context.HoaDons
                                       .Where(h => h.IdBan == b.IdBan && h.TrangThai == "Chưa thanh toán")
                                       .Select(h => h.ThanhTien)
                                       .FirstOrDefault()
               })
               .OrderBy(b => b.SoBan)
               .ToListAsync();
            return Ok(data);
        }

        [HttpPost("createorder/{idBan}/{idNhanVien}")]
        public async Task<IActionResult> CreateOrder(int idBan, int idNhanVien)
        {
            var ban = await _context.Bans.FindAsync(idBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");
            if (ban.TrangThai != "Trống" && ban.TrangThai != "Đã đặt")
                return Conflict("Bàn này đang bận hoặc đang bảo trì.");
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
            ban.TrangThai = "Có khách";
            await _context.SaveChangesAsync();
            return Ok(new { idHoaDon = hoaDon.IdHoaDon });
        }

        [HttpPost("reportproblem/{idBan}/{idNhanVien}")]
        public async Task<IActionResult> BaoCaoSuCo(int idBan, int idNhanVien, [FromBody] BaoCaoSuCoRequestDto request)
        {
            var ban = await _context.Bans.FindAsync(idBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");
            if (ban.TrangThai == "Có khách")
                return Conflict("Không thể báo cáo sự cố bàn đang có khách.");
            ban.TrangThai = "Bảo trì";
            ban.GhiChu = $"[Sự cố NV báo]: {request.GhiChuSuCo}";
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
            await _context.SaveChangesAsync();
            return Ok(new { message = "Báo cáo sự cố thành công. Bàn đã được khóa." });
        }

        [HttpPost("createorder-no-table/{idNhanVien}")]
        public async Task<IActionResult> CreateOrderNoTable(int idNhanVien, [FromBody] string loaiHoaDon)
        {
            // Sửa "Tại quầy" thành "Tại quán" để khớp với CHECK constraint
            if (loaiHoaDon != "Mang về" && loaiHoaDon != "Tại quán")
                return BadRequest("Loại hóa đơn không hợp lệ.");

            // ... (phần còn lại của hàm giữ nguyên) ...
            var nhanVien = await _context.NhanViens.FindAsync(idNhanVien);
            if (nhanVien == null) return NotFound("Nhân viên không hợp lệ.");
            var hoaDon = new HoaDon
            {
                IdBan = null,
                IdNhanVien = idNhanVien,
                TrangThai = "Chưa thanh toán",
                LoaiHoaDon = loaiHoaDon,
                ThoiGianTao = DateTime.Now
            };
            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();
            return Ok(new { idHoaDon = hoaDon.IdHoaDon });
        }


        // === BẮT ĐẦU NÂNG CẤP ===

        [HttpPost("move-table")]
        public async Task<IActionResult> MoveTable([FromBody] BanActionRequestDto dto)
        {
            // (Hàm Chuyển Bàn giữ nguyên)
            var hoaDon = await _context.HoaDons.Include(h => h.Ban).FirstOrDefaultAsync(h => h.IdHoaDon == dto.IdHoaDonNguon);
            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn nguồn.");
            var banDich = await _context.Bans.FindAsync(dto.IdBanDich);
            if (banDich == null) return NotFound("Không tìm thấy bàn đích.");
            if (banDich.TrangThai != "Trống") return Conflict("Bàn đích đang bận, không thể chuyển đến.");
            if (hoaDon.Ban != null) hoaDon.Ban.TrangThai = "Trống";
            banDich.TrangThai = "Có khách";
            hoaDon.IdBan = dto.IdBanDich;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Chuyển bàn thành công." });
        }


        [HttpPost("merge-table")]
        public async Task<IActionResult> MergeTable([FromBody] BanActionRequestDto dto)
        {
            if (dto.IdHoaDonNguon == dto.IdHoaDonDich)
                return BadRequest("Không thể gộp bàn vào chính nó.");

            var chiTietNguon = await _context.ChiTietHoaDons
                .Where(c => c.IdHoaDon == dto.IdHoaDonNguon)
                .ToListAsync();

            if (!chiTietNguon.Any())
                return BadRequest("Bàn nguồn không có sản phẩm để gộp.");

            var hoaDonNguon = await _context.HoaDons
                .Include(h => h.Ban)
                .FirstOrDefaultAsync(h => h.IdHoaDon == dto.IdHoaDonNguon);

            if (hoaDonNguon == null) return NotFound("Không tìm thấy hóa đơn nguồn.");

            // SỬA LỖI CS0266: Kiểm tra HĐ Đích có tồn tại không
            if (!dto.IdHoaDonDich.HasValue ||
                !await _context.HoaDons.AnyAsync(h => h.IdHoaDon == dto.IdHoaDonDich.Value))
            {
                return NotFound("Không tìm thấy hóa đơn đích.");
            }

            // Chuyển các chi tiết HĐ
            foreach (var ct in chiTietNguon)
            {
                // SỬA LỖI CS0266: Gán .Value
                ct.IdHoaDon = dto.IdHoaDonDich.Value;
            }

            // SỬA LỖI CS1061: Xóa dòng này. CSDL (cột computed) sẽ tự tính lại tổng tiền.
            // hoaDonDich.TongTien += tongTienGop; 

            if (hoaDonNguon.Ban != null)
            {
                hoaDonNguon.Ban.TrangThai = "Trống";
            }

            _context.HoaDons.Remove(hoaDonNguon);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gộp bàn thành công." });
        }
    }
}