// Tập tin: CafebookApi/Controllers/Web/TrangChuController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/trangchu")]
    [ApiController]
    public class TrangChuController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        // (Constructor và GetFullImageUrl giữ nguyên)
        public TrangChuController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpGet("data")] // API endpoint
        public async Task<IActionResult> GetTrangChuData()
        {
            // 1. Lấy thông tin chung (từ bảng CaiDat)
            var settings = await _context.CaiDats
                .Where(c => c.TenCaiDat == "TenQuan" ||
                            c.TenCaiDat == "GioiThieu" ||
                            c.TenCaiDat == "BannerImage" ||
                            c.TenCaiDat == "DiaChi" ||
                            c.TenCaiDat == "SoDienThoai" ||
                            c.TenCaiDat == "LienHe_Email" ||
                            c.TenCaiDat == "LienHe_GioMoCua")
                .ToListAsync();

            var thongTinChung = new ThongTinChungDto
            {
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafebook",
                GioiThieu = settings.FirstOrDefault(c => c.TenCaiDat == "GioiThieu")?.GiaTri,
                BannerImageUrl = GetFullImageUrl(settings.FirstOrDefault(c => c.TenCaiDat == "BannerImage")?.GiaTri),
                DiaChi = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri,
                SoDienThoai = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri,
                EmailLienHe = settings.FirstOrDefault(c => c.TenCaiDat == "LienHe_Email")?.GiaTri,
                GioMoCua = settings.FirstOrDefault(c => c.TenCaiDat == "LienHe_GioMoCua")?.GiaTri,

                // Logic tính toán (SỬA ĐỔI)
                SoBanTrong = await _context.Bans.CountAsync(b => b.TrangThai == "Trống"),
                // Đếm số đầu sách (IdSach) có SoLuongHienCo > 0
                SoSachSanSang = await _context.Sachs.CountAsync(s => s.SoLuongHienCo > 0)
            };

            // 2. Lấy 3 khuyến mãi (Đang hoạt động)
            var promotions = await _context.KhuyenMais
                .Where(km => km.TrangThai == "Hoạt động" && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now)
                .OrderBy(km => km.NgayBatDau)
                .Take(3)
                .Select(km => new KhuyenMaiDto
                {
                    TenKhuyenMai = km.TenChuongTrinh,
                    MoTa = km.MoTa,
                    dieuKienApDung = km.DieuKienApDung
                }).ToListAsync();

            // 3. Lấy 5 món nổi bật (SỬA ĐỔI)
            var monNoiBat_Raw = await _context.SanPhams
                .Where(sp => sp.TrangThaiKinhDoanh == true)
                .OrderByDescending(sp => sp.IdSanPham)
                .Take(5)
                .Select(sp => new
                {
                    sp.IdSanPham, // <-- THÊM ID
                    sp.TenSanPham,
                    sp.GiaBan,
                    sp.HinhAnh
                }).ToListAsync();

            var monNoiBat = monNoiBat_Raw.Select(sp => new SanPhamDto
            {
                IdSanPham = sp.IdSanPham, // <-- THÊM ID
                TenSanPham = sp.TenSanPham,
                DonGia = sp.GiaBan,
                AnhSanPhamUrl = GetFullImageUrl(sp.HinhAnh)
            }).ToList();

            // 4. Lấy 4 sách nổi bật
            var sachNoiBat_Raw = await _context.Sachs
                .Include(s => s.TacGia)
                .OrderByDescending(s => s.SoLuongHienCo)
                .Take(4)
                .Select(s => new
                {
                    s.IdSach,
                    s.TenSach,
                    TacGia = s.TacGia != null ? s.TacGia.TenTacGia : "Không rõ",
                    s.AnhBia
                }).ToListAsync();

            var sachNoiBat = sachNoiBat_Raw.Select(s => new SachDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = s.TacGia,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia)
            }).ToList();

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