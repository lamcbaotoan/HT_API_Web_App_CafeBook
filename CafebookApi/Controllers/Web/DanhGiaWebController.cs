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

        // <<< SỬA LỖI 3: BỔ SUNG PHƯƠNG THỨC GET ĐANG BỊ THIẾU >>>
        // GET: api/web/danhgia/cho-danh-gia/5
        [HttpGet("cho-danh-gia/{idHoaDon:int}")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> GetSanPhamsChoDanhGia(int idHoaDon)
        {
            // 1. Lấy ID khách hàng từ token
            var idKhachHangClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idKhachHangClaim, out int idKhachHang))
            {
                return Unauthorized("Token không hợp lệ.");
            }

            // 2. Kiểm tra hóa đơn
            var hoaDon = await _context.HoaDons
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon
                                        && h.IdKhachHang == idKhachHang
                                        && h.TrangThai == "Đã thanh toán");

            if (hoaDon == null)
            {
                return BadRequest("Không tìm thấy hóa đơn hợp lệ hoặc đơn hàng chưa hoàn thành.");
            }

            // 3. Lấy danh sách ID sản phẩm trong hóa đơn (chỉ lấy SP, không lấy sách)
            var sanPhamsTrongHoaDon = await _context.ChiTietHoaDons
                .Where(ct => ct.IdHoaDon == idHoaDon && ct.IdSanPham != null)
                .Select(ct => (int)ct.IdSanPham) // <<< SỬA LỖI CS1061 TẠI ĐÂY (thay .Value)
                .Distinct()
                .ToListAsync();

            if (!sanPhamsTrongHoaDon.Any())
            {
                return Ok(new List<SanPhamChoDanhGiaDto>());
            }

            // 4. Lấy danh sách ID sản phẩm KHÁCH HÀNG đã đánh giá CỦA HÓA ĐƠN NÀY
            var sanPhamsDaDanhGia = await _context.DanhGias
                .Where(d => d.idHoaDon == idHoaDon && d.idKhachHang == idKhachHang && d.idSanPham != null)
                .Select(d => (int)d.idSanPham) // <<< SỬA LỖI CS1061 TẠI ĐÂY (thay .Value)
                .Distinct()
                .ToListAsync();

            // 5. Lọc ra danh sách SP chưa đánh giá và tạo DTO trả về
            var result = await _context.SanPhams
                .Where(s => sanPhamsTrongHoaDon.Contains(s.IdSanPham))
                .Select(s => new SanPhamChoDanhGiaDto
                {
                    IdSanPham = s.IdSanPham,
                    TenSanPham = s.TenSanPham,
                    HinhAnhUrl = s.HinhAnh,
                    DaDanhGia = sanPhamsDaDanhGia.Contains(s.IdSanPham)
                })
                .ToListAsync();

            return Ok(result);
        }


        // POST: api/web/danhgia
        // (Phần này trong file của bạn đã đúng, giữ nguyên)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostDanhGia([FromForm] TaoDanhGiaDto taoDanhGiaDto, IFormFile? hinhAnhFile)
        {
            // <<< SỬA LỖI TẠI ĐÂY >>>
            // 0. Kiểm tra ModelState (bao gồm cả [Required] cho BinhLuan)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Lấy idKhachHang từ JWT Token
            var idKhachHangClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idKhachHangClaim, out int idKhachHang))
            {
                return Unauthorized("Token không hợp lệ.");
            }

            // 2. Kiểm tra hóa đơn
            var hoaDon = await _context.HoaDons
                .FirstOrDefaultAsync(h => h.IdHoaDon == taoDanhGiaDto.idHoaDon && h.IdKhachHang == idKhachHang && h.TrangThai == "Đã thanh toán");

            if (hoaDon == null)
            {
                return BadRequest("Hóa đơn không hợp lệ hoặc chưa hoàn thành.");
            }

            // 3. Kiểm tra sản phẩm có trong hóa đơn không
            var sanPhamTrongHoaDon = await _context.ChiTietHoaDons
                .AnyAsync(ct => ct.IdHoaDon == taoDanhGiaDto.idHoaDon && ct.IdSanPham == taoDanhGiaDto.idSanPham);

            if (!sanPhamTrongHoaDon)
            {
                return BadRequest("Sản phẩm không có trong hóa đơn này.");
            }

            // 4. Kiểm tra đã đánh giá chưa
            var daDanhGia = await _context.DanhGias
                .AnyAsync(d => d.idHoaDon == taoDanhGiaDto.idHoaDon && d.idSanPham == taoDanhGiaDto.idSanPham && d.idKhachHang == idKhachHang);

            if (daDanhGia)
            {
                return BadRequest("Bạn đã đánh giá sản phẩm này cho đơn hàng này rồi.");
            }

            // 5. Xử lý upload hình ảnh (nếu có)
            string? hinhAnhUrl = null;
            if (hinhAnhFile != null && hinhAnhFile.Length > 0)
            {
                // <<< SỬA ĐƯỜNG DẪN LƯU ẢNH THEO YÊU CẦU >>>
                var uploadsDir = Path.Combine(_env.WebRootPath, "images", "anhdanhgia", "monan");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(hinhAnhFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await hinhAnhFile.CopyToAsync(stream);
                }

                // <<< SỬA URL LƯU VÀO DB >>>
                hinhAnhUrl = $"/images/anhdanhgia/monan/{fileName}";
            }

            // 6. Tạo đối tượng DanhGia
            var danhGia = new DanhGia
            {
                idKhachHang = idKhachHang,
                idSanPham = taoDanhGiaDto.idSanPham,
                idHoaDon = taoDanhGiaDto.idHoaDon,
                SoSao = taoDanhGiaDto.SoSao,
                BinhLuan = taoDanhGiaDto.BinhLuan, // Đã [Required]
                HinhAnhURL = hinhAnhUrl,
                NgayTao = DateTime.Now,
                TrangThai = "Hiển thị"
            };

            // 7. Lưu vào DB
            _context.DanhGias.Add(danhGia);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi đánh giá thành công!" });
        }


        // GET: api/web/danhgia/sanpham/5
        // (Phần này trong file của bạn đã đúng, giữ nguyên)
        [HttpGet("sanpham/{idSanPham:int}")]
        public async Task<IActionResult> GetDanhGiasBySanPham(int idSanPham)
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
                    TenKhachHang = d.KhachHang != null ? d.KhachHang.HoTen : "Người dùng ẩn",
                    AvatarKhachHang = d.KhachHang != null ? d.KhachHang.AnhDaiDien : null,
                    SoSao = d.SoSao,
                    BinhLuan = d.BinhLuan,
                    HinhAnhUrl = d.HinhAnhURL,
                    NgayTao = d.NgayTao,
                    PhanHoi = d.PhanHoiDanhGias
                                .Select(p => new PhanHoiWebDto
                                {
                                    TenNhanVien = p.NhanVien != null ? p.NhanVien.HoTen : "Quản trị viên",
                                    NoiDung = p.NoiDung,
                                    NgayTao = p.NgayTao
                                })
                                .FirstOrDefault()
                })
                .ToListAsync();

            // <<< SỬA LỖI TẠI ĐÂY >>>
            // Nếu không có đánh giá, trả về danh sách rỗng (HTTP 200)
            if (danhGias == null || !danhGias.Any())
            {
                // return NotFound("Không có đánh giá nào."); // <-- DÒNG NÀY GÂY LỖI 404
                return Ok(new List<DanhGiaWebDto>()); // <-- SỬA THÀNH DÒNG NÀY
            }

            return Ok(danhGias);
        }
    }
}