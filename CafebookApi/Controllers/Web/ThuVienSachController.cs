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

        [ApiExplorerSettings(IgnoreApi = true)]
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            // SỬA: Chỉ định rõ namespace DTO
            var filters = new CafebookModel.Model.ModelWeb.SachFiltersDto
            {
                TheLoais = await _context.TheLoais
                    .OrderBy(d => d.TenTheLoai)
                    .Select(d => new CafebookModel.Model.ModelApp.FilterLookupDto { Id = d.IdTheLoai, Ten = d.TenTheLoai })
                    .ToListAsync()
            };
            return Ok(filters);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] int? theLoaiId,
            [FromQuery] string? trangThai,
            [FromQuery] string sortBy = "ten_asc",
            [FromQuery] int pageNum = 1,
            [FromQuery] int pageSize = 12)
        {
            var query = _context.Sachs.AsQueryable();

            // SỬA: Lọc Thể loại (dùng N-N)
            if (theLoaiId.HasValue && theLoaiId > 0)
                query = query.Where(s => s.SachTheLoais.Any(stl => stl.IdTheLoai == theLoaiId));

            if (trangThai == "con_sach")
                query = query.Where(s => s.SoLuongHienCo > 0);
            else if (trangThai == "het_sach")
                query = query.Where(s => s.SoLuongHienCo <= 0);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s =>
                    s.TenSach.ToLower().Contains(searchLower) ||
                    // SỬA: Lọc Tác giả (dùng N-N)
                    s.SachTacGias.Any(stg => stg.TacGia.TenTacGia.ToLower().Contains(searchLower))
                );
            }

            query = sortBy switch
            {
                "ten_desc" => query.OrderByDescending(s => s.TenSach),
                "gia_asc" => query.OrderBy(s => s.GiaBia),
                "gia_desc" => query.OrderByDescending(s => s.GiaBia),
                _ => query.OrderBy(s => s.TenSach),
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items_raw = await query
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    s.IdSach,
                    s.TenSach,
                    // SỬA: Nối chuỗi Tác giả (dùng N-N)
                    TacGia = string.Join(", ", s.SachTacGias.Select(stg => stg.TacGia.TenTacGia)),
                    s.GiaBia,
                    s.SoLuongHienCo,
                    s.AnhBia
                })
                .ToListAsync();

            var items_dto = items_raw.Select(s => new SachCardDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = string.IsNullOrEmpty(s.TacGia) ? "Không rõ" : s.TacGia,
                GiaBia = s.GiaBia ?? 0,
                SoLuongCoSan = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia) // <-- Gọi hàm C# (An toàn)
            }).ToList();

            var result = new SachPhanTrangDto
            {
                Items = items_dto,
                TotalPages = totalPages,
                CurrentPage = pageNum
            };
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            // --- SỬA LỖI: Tách làm 2 bước ---

            // Bước 1: Lấy dữ liệu thô (raw data) từ SQL
            var rawSach = await _context.Sachs
                .AsNoTracking()
                .Where(s => s.IdSach == id)
                .Select(s => new
                {
                    s.IdSach,
                    s.TenSach,
                    TacGia = string.Join(", ", s.SachTacGias.Select(stg => stg.TacGia.TenTacGia)),
                    TheLoai = string.Join(", ", s.SachTheLoais.Select(stl => stl.TheLoai.TenTheLoai)),
                    s.GiaBia,
                    s.AnhBia, // Lấy đường dẫn thô
                    s.MoTa,
                    s.ViTri,
                    s.SoLuongTong,
                    s.SoLuongHienCo
                })
                .FirstOrDefaultAsync(); // <-- Dòng 137 cũ

            if (rawSach == null)
                return NotFound(new { Message = "Không tìm thấy sách." });

            // Bước 2: Tạo DTO cuối cùng (trong bộ nhớ C#)
            var sachDto = new SachChiTietDto
            {
                IdSach = rawSach.IdSach,
                TieuDe = rawSach.TenSach,
                TacGia = string.IsNullOrEmpty(rawSach.TacGia) ? "Không rõ" : rawSach.TacGia,
                TheLoai = string.IsNullOrEmpty(rawSach.TheLoai) ? "Không rõ" : rawSach.TheLoai,
                GiaBia = rawSach.GiaBia ?? 0,
                AnhBiaUrl = GetFullImageUrl(rawSach.AnhBia), // <-- Gọi hàm C# (An toàn)
                MoTa = rawSach.MoTa,
                ViTri = rawSach.ViTri,
                TongSoLuong = rawSach.SoLuongTong,
                SoLuongCoSan = rawSach.SoLuongHienCo
            };

            // --- Phần code lấy Gợi Ý (GoiY) đã đúng, giữ nguyên ---
            var suggestions_raw = await _context.DeXuatSachs
                .Where(d => d.IdSachGoc == id)
                .Include(d => d.SachDeXuat.SachTacGias)
                    .ThenInclude(stg => stg.TacGia)
                .OrderByDescending(d => d.DoLienQuan)
                .Take(4)
                .Select(d => d.SachDeXuat)
                .ToListAsync();

            sachDto.GoiY = suggestions_raw.Select(s => new SachCardDto
            {
                IdSach = s.IdSach,
                TieuDe = s.TenSach,
                TacGia = string.Join(", ", s.SachTacGias.Select(stg => stg.TacGia.TenTacGia)),
                GiaBia = s.GiaBia ?? 0,
                SoLuongCoSan = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia) // <-- Gọi hàm C# (An toàn)
            }).ToList();

            foreach (var item in sachDto.GoiY)
            {
                if (string.IsNullOrEmpty(item.TacGia)) item.TacGia = "Không rõ";
            }

            return Ok(sachDto);
        }
    }
}