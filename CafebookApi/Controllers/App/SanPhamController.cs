// Tập tin: CafebookApi/Controllers/App/SanPhamController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http; // <-- Thêm

namespace CafebookApi.Controllers.App
{
    [Route("api/app/sanpham")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public SanPhamController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;

            if (string.IsNullOrEmpty(_env.WebRootPath))
            {
                _env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (!Directory.Exists(_env.WebRootPath))
                {
                    Directory.CreateDirectory(_env.WebRootPath);
                }
            }

            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        // === HELPER 1: TẠO URL TUYỆT ĐỐI === (Giữ nguyên)
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        // === HELPER 2: TẠO TÊN FILE === (Giữ nguyên)
        private string GenerateFileName(int id, string ten)
        {
            string slug = SlugifyUtil.GenerateSlug(ten);
            return $"{id}_{slug}.jpg"; // Luôn lưu là .jpg
        }

        // === HELPER 3: XÓA FILE CŨ === (Giữ nguyên)
        private void DeleteOldImage(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fileName = relativePath.TrimStart('/');
            var fullPath = Path.Combine(_env.WebRootPath, fileName.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // === HELPER 4 (MỚI): ĐỔI TÊN FILE === (Giữ nguyên)
        private void RenameImage(string oldRelativePath, string newRelativePath)
        {
            if (string.IsNullOrEmpty(oldRelativePath) || string.IsNullOrEmpty(newRelativePath) || oldRelativePath == newRelativePath)
                return;

            var oldFileName = oldRelativePath.TrimStart('/');
            var oldFullPath = Path.Combine(_env.WebRootPath, oldFileName.Replace('/', Path.DirectorySeparatorChar));

            var newFileName = newRelativePath.TrimStart('/');
            var newFullPath = Path.Combine(_env.WebRootPath, newFileName.Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(oldFullPath))
            {
                System.IO.File.Move(oldFullPath, newFullPath);
            }
        }

        // === HELPER 5 (SỬA): LƯU FILE TỪ IFormFile ===
        // (Đã xóa SaveImageFromBase64)
        private async Task<string> SaveImageFromFile(IFormFile file, string fileName, string relativeDir)
        {
            var saveDir = Path.Combine(_env.WebRootPath, relativeDir);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            var fullSavePath = Path.Combine(saveDir, fileName);

            // Dùng stream để copy file
            await using (var stream = new FileStream(fullSavePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{relativeDir.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        }

        #region API Sản Phẩm (Đã sửa)

        // ... (Hàm SearchSanPham, GetSanPhamDetails giữ nguyên) ...
        [HttpGet("search")]
        public async Task<IActionResult> SearchSanPham([FromQuery] string? searchText, [FromQuery] int? danhMucId, [FromQuery] bool? trangThai)
        {
            // ... (Logic không đổi)
            var query = _context.SanPhams.Include(s => s.DanhMuc).AsQueryable();

            if (danhMucId.HasValue && danhMucId > 0)
                query = query.Where(s => s.IdDanhMuc == danhMucId);
            if (trangThai.HasValue)
                query = query.Where(s => s.TrangThaiKinhDoanh == trangThai.Value);
            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(s => s.TenSanPham.ToLower().Contains(searchLower));
            }

            var rawResults = await query.Select(s => new
            {
                s.IdSanPham,
                s.TenSanPham,
                s.GiaBan,
                TenDanhMuc = s.DanhMuc != null ? s.DanhMuc.TenDanhMuc : "N/A",
                s.TrangThaiKinhDoanh,
                s.HinhAnh
            })
            .OrderBy(s => s.TenSanPham)
            .ToListAsync();

            var results = rawResults.Select(s => new SanPhamDto
            {
                IdSanPham = s.IdSanPham,
                TenSanPham = s.TenSanPham,
                GiaBan = s.GiaBan,
                TenDanhMuc = s.TenDanhMuc,
                TrangThaiKinhDoanh = s.TrangThaiKinhDoanh,
                HinhAnhUrl = GetFullImageUrl(s.HinhAnh)
            }).ToList();

            return Ok(results);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetSanPhamDetails(int id)
        {
            // ... (Logic không đổi)
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();

            var dto = new SanPhamDetailDto
            {
                IdSanPham = sp.IdSanPham,
                TenSanPham = sp.TenSanPham,
                IdDanhMuc = sp.IdDanhMuc,
                GiaBan = sp.GiaBan,
                MoTa = sp.MoTa,
                TrangThaiKinhDoanh = sp.TrangThaiKinhDoanh,
                NhomIn = sp.NhomIn,
                HinhAnhUrl = GetFullImageUrl(sp.HinhAnh) // <-- Dùng Helper
            };
            return Ok(dto);
        }


        // SỬA: Dùng [FromForm]
        [HttpPost]
        public async Task<IActionResult> CreateSanPham(
                    [FromForm] SanPhamUpdateRequestDto dto)
        {
            if (await _context.SanPhams.AnyAsync(s => s.TenSanPham.ToLower() == dto.TenSanPham.ToLower() && s.IdDanhMuc == dto.IdDanhMuc))
            {
                return Conflict("Tên sản phẩm này đã tồn tại trong danh mục đã chọn.");
            }

            var sanPham = new SanPham
            {
                TenSanPham = dto.TenSanPham,
                IdDanhMuc = dto.IdDanhMuc ?? 0,
                GiaBan = dto.GiaBan,
                MoTa = dto.MoTa,
                TrangThaiKinhDoanh = dto.TrangThaiKinhDoanh,
                NhomIn = dto.NhomIn,
                HinhAnh = null
            };
            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync(); // Lưu lần 1 (Lấy ID)

            // SỬA: Lấy file từ dto.HinhAnhUpload
            if (dto.HinhAnhUpload != null)
            {
                try
                {
                    string fileName = GenerateFileName(sanPham.IdSanPham, sanPham.TenSanPham);
                    string relativePath = await SaveImageFromFile(dto.HinhAnhUpload, fileName, HinhAnhPaths.UrlFoods.TrimStart('/'));
                    sanPham.HinhAnh = relativePath;
                    await _context.SaveChangesAsync(); // Lưu lần 2 (Cập nhật ảnh)
                }
                catch (Exception ex)
                {
                    _context.SanPhams.Remove(sanPham);
                    await _context.SaveChangesAsync();
                    return StatusCode(500, $"Lỗi khi lưu ảnh: {ex.Message}");
                }
            }

            var detailDto = new SanPhamDetailDto
            {
                IdSanPham = sanPham.IdSanPham,
                TenSanPham = sanPham.TenSanPham,
                HinhAnhUrl = GetFullImageUrl(sanPham.HinhAnh)
            };

            return Ok(detailDto);
        }

        // SỬA: Dùng [FromForm]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSanPham(int id,
                    [FromForm] SanPhamUpdateRequestDto dto)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();

            if (await _context.SanPhams.AnyAsync(s => s.TenSanPham.ToLower() == dto.TenSanPham.ToLower() && s.IdDanhMuc == dto.IdDanhMuc && s.IdSanPham != id))
            {
                return Conflict("Tên sản phẩm này đã tồn tại trong danh mục đã chọn.");
            }

            var oldImagePath = sp.HinhAnh;
            var oldTenSanPham = sp.TenSanPham;

            // SỬA: Lấy file và cờ Xóa từ DTO
            bool isImageUpload = dto.HinhAnhUpload != null;
            bool isNameChange = dto.TenSanPham != oldTenSanPham;

            sp.TenSanPham = dto.TenSanPham;
            sp.IdDanhMuc = dto.IdDanhMuc ?? 0;
            sp.GiaBan = dto.GiaBan;
            sp.MoTa = dto.MoTa;
            sp.TrangThaiKinhDoanh = dto.TrangThaiKinhDoanh;
            sp.NhomIn = dto.NhomIn;

            string newFileName = GenerateFileName(sp.IdSanPham, sp.TenSanPham);
            string newRelativePath = $"/{HinhAnhPaths.UrlFoods.TrimStart('/')}/{newFileName}";

            if (isImageUpload)
            {
                DeleteOldImage(oldImagePath);
                // === SỬA LỖI CS8604 (Dòng 258) ===
                // Thêm '!' vì logic 'isImageUpload' đã đảm bảo file không null
                sp.HinhAnh = await SaveImageFromFile(dto.HinhAnhUpload!, newFileName, HinhAnhPaths.UrlFoods.TrimStart('/'));
            }
            // SỬA: Lấy cờ Xóa từ DTO
            else if (dto.XoaHinhAnh)
            {
                DeleteOldImage(oldImagePath);
                sp.HinhAnh = null;
            }
            else if (isNameChange && !string.IsNullOrEmpty(oldImagePath))
            {
                RenameImage(oldImagePath, newRelativePath);
                sp.HinhAnh = newRelativePath;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ... (DeleteSanPham giữ nguyên, logic DeleteOldImage đã có) ...
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSanPham(int id)
        {
            if (await _context.ChiTietHoaDons.AnyAsync(ct => ct.IdSanPham == id))
            {
                return Conflict("Không thể xóa. Sản phẩm này đã tồn tại trong lịch sử hóa đơn.");
            }
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();

            DeleteOldImage(sp.HinhAnh); // <-- Logic xóa ảnh

            var dinhLuong = await _context.DinhLuongs.Where(d => d.IdSanPham == id).ToListAsync();
            if (dinhLuong.Any())
            {
                _context.DinhLuongs.RemoveRange(dinhLuong);
            }
            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion

        #region API Danh Mục
        [HttpGet("filters")] // <-- PHẢI LÀ [HttpGet]
        public async Task<IActionResult> GetSanPhamFilters()
        {
            var danhMucs = await _context.DanhMucs
                .Select(t => new FilterLookupDto { Id = t.IdDanhMuc, Ten = t.TenDanhMuc })
                .OrderBy(t => t.Ten)
                .ToListAsync();
            var nguyenLieus = (await _context.NguyenLieus
                .Select(nl => new { nl.IdNguyenLieu, nl.TenNguyenLieu, nl.DonViTinh })
                .ToListAsync())
                .Select(nl => new FilterLookupDto
                {
                    Id = nl.IdNguyenLieu,
                    Ten = nl.TenNguyenLieu + $" ({nl.DonViTinh})"
                })
                .OrderBy(nl => nl.Ten)
                .ToList();
            var donViTinhs = await _context.DonViChuyenDois
                .Select(d => new DonViChuyenDoiDto
                {
                    Id = d.IdChuyenDoi,
                    IdNguyenLieu = d.IdNguyenLieu,
                    Ten = d.TenDonVi
                })
                .ToListAsync();
            var dto = new SanPhamFiltersDto
            {
                DanhMucs = danhMucs,
                NguyenLieus = nguyenLieus,
                DonViTinhs = donViTinhs
            };
            return Ok(dto);
        }

        [HttpPost("danhmuc")]
        public async Task<IActionResult> CreateDanhMuc([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.DanhMucs.FirstOrDefaultAsync(t => t.TenDanhMuc.ToLower() == dto.Ten.ToLower());
            if (existing != null) return Conflict("Tên danh mục đã tồn tại.");
            var newEntity = new DanhMuc { TenDanhMuc = dto.Ten };
            _context.DanhMucs.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdDanhMuc, Ten = newEntity.TenDanhMuc });
        }

        [HttpPut("danhmuc/{id}")]
        public async Task<IActionResult> UpdateDanhMuc(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.DanhMucs.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenDanhMuc = dto.Ten;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("danhmuc/{id}")]
        public async Task<IActionResult> DeleteDanhMuc(int id)
        {
            if (await _context.SanPhams.AnyAsync(s => s.IdDanhMuc == id))
            {
                return Conflict("Không thể xóa. Danh mục này đã được gán cho sản phẩm.");
            }
            if (await _context.DanhMucs.AnyAsync(d => d.IdDanhMucCha == id))
            {
                return Conflict("Không thể xóa. Danh mục này đang là cha của danh mục khác.");
            }
            var entity = await _context.DanhMucs.FindAsync(id);
            if (entity == null) return NotFound();
            _context.DanhMucs.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
        #endregion

        #region API Định Lượng
        [HttpGet("{idSanPham}/dinhluong")]
        public async Task<IActionResult> GetDinhLuong(int idSanPham)
        {
            var data = await _context.DinhLuongs
                .Where(d => d.IdSanPham == idSanPham)
                .Include(d => d.NguyenLieu)
                .Include(d => d.DonViSuDung)
                .Select(d => new DinhLuongDto
                {
                    IdNguyenLieu = d.IdNguyenLieu,
                    TenNguyenLieu = d.NguyenLieu.TenNguyenLieu,
                    SoLuong = d.SoLuongSuDung,
                    IdDonViSuDung = d.IdDonViSuDung,
                    TenDonViSuDung = d.DonViSuDung.TenDonVi
                })
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("dinhluong")]
        public async Task<IActionResult> UpdateDinhLuong([FromBody] DinhLuongUpdateRequestDto dto)
        {
            var existing = await _context.DinhLuongs.FindAsync(dto.IdSanPham, dto.IdNguyenLieu);
            if (existing != null)
            {
                existing.SoLuongSuDung = dto.SoLuong;
                existing.IdDonViSuDung = dto.IdDonViSuDung;
            }
            else
            {
                var newDinhLuong = new DinhLuong
                {
                    IdSanPham = dto.IdSanPham,
                    IdNguyenLieu = dto.IdNguyenLieu,
                    SoLuongSuDung = dto.SoLuong,
                    IdDonViSuDung = dto.IdDonViSuDung
                };
                _context.DinhLuongs.Add(newDinhLuong);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("dinhluong/{idSanPham}/{idNguyenLieu}")]
        public async Task<IActionResult> DeleteDinhLuong(int idSanPham, int idNguyenLieu)
        {
            var existing = await _context.DinhLuongs.FindAsync(idSanPham, idNguyenLieu);
            if (existing != null)
            {
                _context.DinhLuongs.Remove(existing);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
        #endregion
    }
}