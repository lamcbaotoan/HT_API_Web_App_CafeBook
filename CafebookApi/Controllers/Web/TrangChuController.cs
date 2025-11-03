using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/trangchu")]
    [ApiController]
    public class TrangChuController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        public TrangChuController(CafebookDbContext context) { _context = context; }

        [HttpGet("data")] // API endpoint
        public async Task<IActionResult> GetTrangChuData()
        {
            // 1. Lấy thông tin chung (từ bảng CaiDat)
            var settings = await _context.CaiDats.ToListAsync();
            var thongTinChung = new ThongTinChungDto
            {
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafebook",
                GioiThieu = "Nơi bạn có thể thưởng thức cà phê thơm ngon...",
                DiaChi = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri,
                SoDienThoai = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri,
                EmailLienHe = "contact@cafebook.vn",
                GioMoCua = "07:00 - 22:00",
                SoBanTrong = await _context.Bans.CountAsync(b => b.TrangThai == "Trống"),
                SoSachDangDuocThue = await _context.ChiTietPhieuThues.CountAsync(ct => ct.NgayTraThucTe == null)
            };

            // 2. Lấy 3 khuyến mãi mới nhất
            var promotions = await _context.KhuyenMais
                .Where(km => km.NgayKetThuc > DateTime.Now && km.NgayBatDau < DateTime.Now)
                .OrderByDescending(km => km.NgayBatDau)
                .Take(3)
                .Select(km => new KhuyenMaiDto
                {
                    TenKhuyenMai = km.TenChuongTrinh,
                    MoTa = km.MoTa,
                    dieuKienApDung = km.DieuKienApDung
                }).ToListAsync();

            // 3. Lấy 5 món nổi bật
            var monNoiBat = await _context.SanPhams
                .Where(sp => sp.TrangThaiKinhDoanh == true)
                .OrderByDescending(sp => sp.IdSanPham) // Giả lập món mới = nổi bật
                .Take(5)
                .Select(sp => new SanPhamDto
                {
                    TenSanPham = sp.TenSanPham,
                    DonGia = sp.GiaBan,
                    AnhSanPhamBase64 = sp.HinhAnh // Giả sử đây là Base64
                }).ToListAsync();

            // 4. Lấy 4 sách nổi bật
            var sachNoiBat = await _context.Sachs
                .Include(s => s.TacGia) // Join bảng Tác giả
                .OrderByDescending(s => s.SoLuongHienCo)
                .Take(4)
                .Select(s => new SachDto
                {
                    IdSach = s.IdSach,
                    TieuDe = s.TenSach,
                    TacGia = s.TacGia != null ? s.TacGia.TenTacGia : "Không rõ",
                    AnhBia = s.AnhBia // Base64
                }).ToListAsync();

            // 5. Gộp tất cả vào DTO
            var dto = new TrangChuDto
            {
                Info = thongTinChung,
                Promotions = promotions,
                MonNoiBat = monNoiBat,
                SachNoiBat = sachNoiBat
            };

            return Ok(dto);
        }
    }
}