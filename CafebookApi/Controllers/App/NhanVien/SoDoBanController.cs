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

        // === HÀM ĐÃ ĐƯỢC NÂNG CẤP ===
        [HttpGet("tables")]
        public async Task<IActionResult> GetSoDoBan()
        {
            var now = DateTime.Now;
            var nowPlus5Minutes = now.AddMinutes(5); // Mốc thời gian 5 phút

            var data = await _context.Bans
               .AsNoTracking()
               .Select(b => new
               {
                   // 1. Lấy thông tin bàn
                   Ban = b,

                   // 2. Lấy Hóa đơn hiện tại (nếu có)
                   HoaDonHienTai = _context.HoaDons
                       .Where(h => h.IdBan == b.IdBan && h.TrangThai == "Chưa thanh toán")
                       .Select(h => new { h.IdHoaDon, h.ThanhTien }) // Chỉ lấy 2 trường cần
                       .FirstOrDefault(),

                   // 3. Lấy Phiếu đặt sắp tới GẦN NHẤT
                   PhieuDatSapToi = _context.PhieuDatBans
                       .Where(p => p.IdBan == b.IdBan &&
                                   p.ThoiGianDat > now && // Phải là trong tương lai
                                   (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận"))
                       .OrderBy(p => p.ThoiGianDat) // Quan trọng: lấy cái sớm nhất
                       .FirstOrDefault()
               })
               // 4. Giờ mới tạo DTO
               .Select(data => new BanSoDoDto
               {
                   IdBan = data.Ban.IdBan,
                   SoBan = data.Ban.SoBan,

                   // === LOGIC TRẠNG THÁI MỚI ===
                   TrangThai = (data.Ban.TrangThai == "Trống" &&
                                data.PhieuDatSapToi != null &&
                                data.PhieuDatSapToi.ThoiGianDat <= nowPlus5Minutes)
                               ? "Đã đặt" // Tự động đổi "Trống" -> "Đã đặt" (Gần giờ)
                               : data.Ban.TrangThai, // Giữ nguyên (Trống, Có khách, Bảo trì)

                   GhiChu = data.Ban.GhiChu,
                   IdKhuVuc = data.Ban.IdKhuVuc,

                   // Gán từ HoaDonHienTai
                   IdHoaDonHienTai = data.HoaDonHienTai != null ? (int?)data.HoaDonHienTai.IdHoaDon : null,
                   TongTienHienTai = data.HoaDonHienTai != null ? data.HoaDonHienTai.ThanhTien : 0,

                   // === LOGIC THÔNG TIN ĐẶT BÀN MỚI ===
                   ThongTinDatBan = (data.Ban.TrangThai == "Trống" && data.PhieuDatSapToi != null)
                                    ? $"Đặt lúc: {data.PhieuDatSapToi.ThoiGianDat:HH:mm}" // Luôn hiển thị nếu có phiếu
                                    : null
               })
               .OrderBy(b => b.SoBan)
               .ToListAsync();

            return Ok(data);
        }

        // (Các hàm còn lại giữ nguyên)

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
            if (loaiHoaDon != "Mang về" && loaiHoaDon != "Tại quán")
                return BadRequest("Loại hóa đơn không hợp lệ.");

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

        [HttpPost("move-table")]
        public async Task<IActionResult> MoveTable([FromBody] BanActionRequestDto dto)
        {
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

            if (!dto.IdHoaDonDich.HasValue ||
                !await _context.HoaDons.AnyAsync(h => h.IdHoaDon == dto.IdHoaDonDich.Value))
            {
                return NotFound("Không tìm thấy hóa đơn đích.");
            }

            foreach (var ct in chiTietNguon)
            {
                ct.IdHoaDon = dto.IdHoaDonDich.Value;
            }

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