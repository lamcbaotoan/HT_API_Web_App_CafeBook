// Tập tin: CafebookApi/Controllers/Web/ThuVienSachController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Model.ModelApp; // Dùng chung FilterLookupDto
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/thuvien")]
    [ApiController]
    public class ThuVienSachController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public ThuVienSachController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        // Helper tạo URL ảnh
        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        /// <summary>
        /// API lấy danh sách Thể Loại cho bộ lọc
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            // SỬA LỖI CS0104: Chỉ định rõ namespace ModelWeb
            var filters = new CafebookModel.Model.ModelWeb.SachFiltersDto
            {
                TheLoais = await _context.TheLoais
                    .OrderBy(d => d.TenTheLoai)
                    .Select(d => new FilterLookupDto { Id = d.IdTheLoai, Ten = d.TenTheLoai })
                    .ToListAsync()
            };
            return Ok(filters);
        }

        /// <summary>
        /// API tìm kiếm, lọc và phân trang Sách
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] int? theLoaiId,
            [FromQuery] string? trangThai,
            [FromQuery] string sortBy = "ten_asc",
            [FromQuery] int pageNum = 1,
            [FromQuery] int pageSize = 12) // 12 sách/trang
        {
            var query = _context.Sachs
                .Include(s => s.TacGia)
                .AsQueryable();

            // Lọc
            if (theLoaiId.HasValue && theLoaiId > 0)
                query = query.Where(s => s.IdTheLoai == theLoaiId);

            if (trangThai == "con_sach")
                query = query.Where(s => s.SoLuongHienCo > 0);
            else if (trangThai == "het_sach")
                query = query.Where(s => s.SoLuongHienCo <= 0);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.TenSach.Contains(search) || (s.TacGia != null && s.TacGia.TenTacGia.Contains(search)));

            // Sắp xếp
            query = sortBy switch
            {
                "ten_desc" => query.OrderByDescending(s => s.TenSach),
                "gia_asc" => query.OrderBy(s => s.GiaBia),
                "gia_desc" => query.OrderByDescending(s => s.GiaBia),
                _ => query.OrderBy(s => s.TenSach), // Mặc định ten_asc
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Bước 1: Lấy dữ liệu thô
            var items_raw = await query
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    s.IdSach,
                    s.TenSach,
                    TacGia = s.TacGia != null ? s.TacGia.TenTacGia : "N/A",
                    s.GiaBia,
                    s.SoLuongHienCo,
                    s.AnhBia
                })
                .ToListAsync();

            // Bước 2: Chuyển đổi sang DTO (gọi GetFullImageUrl)
            var items_dto = items_raw.Select(s => new SachCardDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = s.TacGia,
                GiaBia = s.GiaBia ?? 0,
                SoLuongCoSan = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia)
            }).ToList();

            var result = new SachPhanTrangDto
            {
                Items = items_dto,
                TotalPages = totalPages,
                CurrentPage = pageNum
            };
            return Ok(result);
        }

        /// <summary>
        /// API lấy chi tiết 1 cuốn sách (gồm gợi ý)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var sach = await _context.Sachs
                .Include(s => s.TacGia)
                .Include(s => s.TheLoai)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSach == id);

            if (sach == null)
                return NotFound(new { Message = "Không tìm thấy sách." });

            // Lấy danh sách gợi ý
            var suggestions_raw = await _context.DeXuatSachs
                .Where(d => d.IdSachGoc == id)
                .Include(d => d.SachDeXuat.TacGia) // Join Sach -> TacGia
                .OrderByDescending(d => d.DoLienQuan)
                .Take(4)
                .Select(d => d.SachDeXuat)
                .ToListAsync();

            var suggestions_dto = suggestions_raw.Select(s => new SachCardDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = s.TacGia != null ? s.TacGia.TenTacGia : null,
                GiaBia = s.GiaBia ?? 0,
                SoLuongCoSan = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia)
            }).ToList();

            var dto = new SachChiTietDto
            {
                IdSach = sach.IdSach,
                TieuDe = sach.TenSach,
                TacGia = sach.TacGia?.TenTacGia,
                TheLoai = sach.TheLoai?.TenTheLoai,
                GiaBia = sach.GiaBia ?? 0,
                AnhBiaUrl = GetFullImageUrl(sach.AnhBia),
                MoTa = sach.MoTa,
                ViTri = sach.ViTri,
                TongSoLuong = sach.SoLuongTong,
                SoLuongCoSan = sach.SoLuongHienCo,
                GoiY = suggestions_dto
            };
            return Ok(dto);
        }
    }
}