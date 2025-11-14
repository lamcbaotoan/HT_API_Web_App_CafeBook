using CafebookApi.Data;
using CafebookModel.Model;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/danhgia")]
    [ApiController]
    public class DanhGiaWebController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DanhGiaWebController(CafebookDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: api/web/danhgia
        [HttpPost]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> PostDanhGia([FromForm] TaoDanhGiaDto taoDanhGiaDto, IFormFile? hinhAnhFile)
        {
            // 1. Lấy idKhachHang từ JWT Token
            var idKhachHangClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idKhachHangClaim, out int idKhachHang))
            {
                return Unauthorized("Token không hợp lệ.");
            }

            // 2. Kiểm tra xem khách hàng có sở hữu hóa đơn này không và hóa đơn đã thanh toán chưa
            var hoaDon = await _context.HoaDons
                .Include(hd => hd.ChiTietHoaDons)
                .FirstOrDefaultAsync(hd => hd.IdHoaDon == taoDanhGiaDto.idHoaDon &&
                                             hd.IdKhachHang == idKhachHang &&
                                             hd.TrangThai == "Đã thanh toán");

            if (hoaDon == null)
            {
                return BadRequest("Hóa đơn không hợp lệ, chưa thanh toán, hoặc bạn không sở hữu hóa đơn này.");
            }

            // 3. Kiểm tra xem sản phẩm/sách có nằm trong hóa đơn đó không
            bool daMuaHang = hoaDon.ChiTietHoaDons.Any(ct => ct.IdSanPham == taoDanhGiaDto.idSanPham);

            if (!daMuaHang)
            {
                return BadRequest("Bạn phải mua sản phẩm này trong đơn hàng này trước khi đánh giá.");
            }

            // 4. Kiểm tra xem đã đánh giá SP/Sách này từ hóa đơn này CHƯA
            bool daDanhGia = await _context.DanhGias.AnyAsync(d =>
                d.idHoaDon == taoDanhGiaDto.idHoaDon &&
                d.idKhachHang == idKhachHang &&
                d.idSanPham == taoDanhGiaDto.idSanPham // Chỉ kiểm tra sản phẩm
            );

            if (daDanhGia)
            {
                return BadRequest("Bạn đã đánh giá sản phẩm này từ hóa đơn này rồi.");
            }

            // 5. Xử lý Upload ảnh (Nếu có)
            string? hinhAnhURL = null;
            if (hinhAnhFile != null && hinhAnhFile.Length > 0)
            {
                string thuMucLuu = Path.Combine(_env.WebRootPath, "images", "anhdanhgia", "monan");
                string folder = "monan";

                if (!Directory.Exists(thuMucLuu))
                {
                    Directory.CreateDirectory(thuMucLuu);
                }

                string tenFileMoi = $"{Guid.NewGuid()}_{hinhAnhFile.FileName}";
                string duongDanDayDu = Path.Combine(thuMucLuu, tenFileMoi);

                await using (var stream = new FileStream(duongDanDayDu, FileMode.Create))
                {
                    await hinhAnhFile.CopyToAsync(stream);
                }

                hinhAnhURL = $"/images/anhdanhgia/{folder}/{tenFileMoi}";
            }

            // 6. Tạo đối tượng Đánh giá và Lưu DB
            var danhGiaMoi = new DanhGia
            {
                idKhachHang = idKhachHang,
                idHoaDon = taoDanhGiaDto.idHoaDon,
                idSanPham = taoDanhGiaDto.idSanPham,
                SoSao = taoDanhGiaDto.SoSao,
                BinhLuan = taoDanhGiaDto.BinhLuan,
                HinhAnhURL = hinhAnhURL,
                NgayTao = DateTime.Now,
                TrangThai = "Hiển thị"
            };

            _context.DanhGias.Add(danhGiaMoi);
            await _context.SaveChangesAsync();
            
            // Logic CapNhatDiemTrungBinhSanPham đã bị xóa vì SanPham không có DiemDanhGia

            return Ok(new { message = "Đánh giá của bạn đã được ghi nhận." });
        }

        // GET: api/web/danhgia/cho-danh-gia/{idHoaDon}
        [HttpGet("cho-danh-gia/{idHoaDon}")]
        [Authorize]
        public async Task<IActionResult> GetSanPhamChoDanhGia(int idHoaDon)
        {
            var idKhachHangClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idKhachHangClaim, out int idKhachHang))
            {
                return Unauthorized("Token không hợp lệ.");
            }

            var hoaDon = await _context.HoaDons
                .Include(hd => hd.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(hd => hd.IdHoaDon == idHoaDon &&
                                            hd.IdKhachHang == idKhachHang &&
                                            hd.TrangThai == "Đã thanh toán");

            if (hoaDon == null)
            {
                return BadRequest("Hóa đơn không hợp lệ hoặc không phải của bạn.");
            }

            var reviewsDaGui = await _context.DanhGias
                .Where(d => d.idHoaDon == idHoaDon && d.idKhachHang == idKhachHang && d.idSanPham != null)
                .Select(d => d.idSanPham.Value)
                .ToListAsync();

            var result = hoaDon.ChiTietHoaDons
                .Where(ct => ct.SanPham != null)
                .Select(ct => new SanPhamChoDanhGiaDto
                {
                    IdSanPham = (int)ct.IdSanPham,
                    TenSanPham = ct.SanPham.TenSanPham,
                    HinhAnhUrl = ct.SanPham.HinhAnh,
                    DaDanhGia = reviewsDaGui.Contains((int)ct.IdSanPham)
                })
                .DistinctBy(p => p.IdSanPham)
                .ToList();

            return Ok(result);
        }


        // GET: api/web/danhgia/sanpham/{id}
        [HttpGet("sanpham/{idSanPham}")]
        public async Task<ActionResult<IEnumerable<DanhGiaWebDto>>> GetDanhGiaChoSanPham(int idSanPham)
        {
            var danhGias = await _context.DanhGias
                .Where(d => d.idSanPham == idSanPham && d.TrangThai == "Hiển thị")
                .Include(d => d.KhachHang)
                .Include(d => d.PhanHoiDanhGias)
                    .ThenInclude(p => p.NhanVien)
                .OrderByDescending(d => d.NgayTao)
                .Select(d => new DanhGiaWebDto
                {
                    IdDanhGia = d.idDanhGia,
                    // SỬA: TenKH -> HoTen
                    TenKhachHang = d.KhachHang != null ? d.KhachHang.HoTen : "Người dùng ẩn",
                    // SỬA: Avatar -> AnhDaiDien
                    AvatarKhachHang = d.KhachHang != null ? d.KhachHang.AnhDaiDien : null,
                    SoSao = d.SoSao,
                    BinhLuan = d.BinhLuan,
                    HinhAnhUrl = d.HinhAnhURL,
                    NgayTao = d.NgayTao,
                    PhanHoi = d.PhanHoiDanhGias
                                // SỬA: Xóa .Where() vì PhanHoiDanhGia không có TrangThai
                                .Select(p => new PhanHoiWebDto
                                {
                                    TenNhanVien = p.NhanVien != null ? p.NhanVien.HoTen : "Quản trị viên",
                                    NoiDung = p.NoiDung,
                                    NgayTao = p.NgayTao
                                })
                                .FirstOrDefault() // Lấy phản hồi đầu tiên (nếu có)
                })
                .ToListAsync();

            return Ok(danhGias);
        }

        // Logic CapNhatDiemTrungBinhSanPham đã bị xóa
    }
}