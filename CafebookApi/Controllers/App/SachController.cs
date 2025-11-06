// Tập tin: CafebookApi/Controllers/App/SachController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using CafebookModel.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http; // <-- THÊM

namespace CafebookApi.Controllers.App
{
    [Route("api/app/sach")]
    [ApiController]
    public class SachController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public SachController(CafebookDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;

            // Đảm bảo WebRootPath tồn tại
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

        // === 5 HÀM HELPER XỬ LÝ FILE ===

        // HELPER 1: TẠO URL TUYỆT ĐỐI (Giữ nguyên)
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        // HELPER 2: TẠO TÊN FILE (Giữ nguyên)
        private string GenerateFileName(int id, string ten)
        {
            string slug = SlugifyUtil.GenerateSlug(ten);
            return $"{id}_{slug}.jpg";
        }

        // HELPER 3: XÓA FILE CŨ (Giữ nguyên)
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

        // HELPER 4: ĐỔI TÊN FILE (Giữ nguyên)
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

        // HELPER 5 (SỬA): LƯU FILE TỪ IFormFile
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
        // =============================

        [HttpGet("filters")] // <-- PHẢI LÀ [HttpGet]
        public async Task<IActionResult> GetSachFilters()
        {
            var theLoais = await _context.TheLoais
                .Select(t => new FilterLookupDto { Id = t.IdTheLoai, Ten = t.TenTheLoai })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var tacGias = await _context.TacGias
                .Select(t => new FilterLookupDto { Id = t.IdTacGia, Ten = t.TenTacGia })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var nhaXuatBans = await _context.NhaXuatBans
                .Select(t => new FilterLookupDto { Id = t.IdNhaXuatBan, Ten = t.TenNhaXuatBan })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var dto = new SachFiltersDto
            {
                TheLoais = theLoais,
                TacGias = tacGias,
                NhaXuatBans = nhaXuatBans
            };
            return Ok(dto);
        }

        #region API Sách (Đã sửa)

        // (Hàm SearchSach và GetSachDetails giữ nguyên logic, chỉ đảm bảo trả về URL)
        [HttpGet("search")]
        public async Task<IActionResult> SearchSach(
            [FromQuery] string? searchText,
            [FromQuery] int? theLoaiId)
        {
            var query = _context.Sachs
                .Include(s => s.TacGia)
                .Include(s => s.TheLoai)
                .AsQueryable();

            if (theLoaiId.HasValue && theLoaiId > 0)
            {
                query = query.Where(s => s.IdTheLoai == theLoaiId);
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(s =>
                    s.TenSach.ToLower().Contains(searchLower) ||
                    (s.TacGia != null && s.TacGia.TenTacGia.ToLower().Contains(searchLower))
                );
            }

            var rawResults = await query
                .Select(s => new
                {
                    s.IdSach,
                    s.TenSach,
                    TenTacGia = s.TacGia != null ? s.TacGia.TenTacGia : "N/A",
                    TenTheLoai = s.TheLoai != null ? s.TheLoai.TenTheLoai : "N/A",
                    s.ViTri,
                    s.SoLuongTong,
                    s.SoLuongHienCo,
                    s.AnhBia // Chỉ lấy path
                })
                .OrderBy(s => s.TenSach)
                .ToListAsync();

            var results = rawResults.Select(s => new SachDto
            {
                IdSach = s.IdSach,
                TenSach = s.TenSach,
                TenTacGia = s.TenTacGia,
                TenTheLoai = s.TenTheLoai,
                ViTri = s.ViTri,
                SoLuongTong = s.SoLuongTong,
                SoLuongHienCo = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia) // <-- Dùng Helper
            }).ToList();

            return Ok(results);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetSachDetails(int id)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            var dto = new SachDetailDto
            {
                IdSach = sach.IdSach,
                TenSach = sach.TenSach,
                IdTheLoai = sach.IdTheLoai,
                IdTacGia = sach.IdTacGia,
                IdNhaXuatBan = sach.IdNhaXuatBan,
                NamXuatBan = sach.NamXuatBan,
                MoTa = sach.MoTa,
                SoLuongTong = sach.SoLuongTong,
                AnhBiaUrl = GetFullImageUrl(sach.AnhBia), // <-- Dùng Helper
                GiaBia = sach.GiaBia,
                ViTri = sach.ViTri
            };
            return Ok(dto);
        }

        // SỬA: Dùng [FromForm]
        [HttpPost]
        public async Task<IActionResult> CreateSach(
                    [FromForm] SachUpdateRequestDto dto)
        {
            var sach = new Sach
            {
                TenSach = dto.TenSach,
                IdTheLoai = dto.IdTheLoai,
                IdTacGia = dto.IdTacGia,
                IdNhaXuatBan = dto.IdNhaXuatBan,
                NamXuatBan = dto.NamXuatBan,
                MoTa = dto.MoTa,
                SoLuongTong = dto.SoLuongTong,
                SoLuongHienCo = dto.SoLuongTong,
                GiaBia = dto.GiaBia,
                ViTri = dto.ViTri,
                AnhBia = null
            };
            _context.Sachs.Add(sach);
            await _context.SaveChangesAsync();

            // SỬA: Lấy file từ dto.AnhBiaUpload
            if (dto.AnhBiaUpload != null)
            {
                try
                {
                    string fileName = GenerateFileName(sach.IdSach, sach.TenSach);
                    string relativePath = await SaveImageFromFile(dto.AnhBiaUpload, fileName, HinhAnhPaths.UrlBooks.TrimStart('/'));
                    sach.AnhBia = relativePath;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _context.Sachs.Remove(sach);
                    await _context.SaveChangesAsync();
                    return StatusCode(500, $"Lỗi khi lưu ảnh: {ex.Message}");
                }
            }

            var detailDto = new SachDetailDto
            {
                IdSach = sach.IdSach,
                TenSach = sach.TenSach,
                AnhBiaUrl = GetFullImageUrl(sach.AnhBia)
            };
            return Ok(detailDto);
        }

        // SỬA: Dùng [FromForm]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSach(int id,
                    [FromForm] SachUpdateRequestDto dto)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            int soLuongDangMuon = sach.SoLuongTong - sach.SoLuongHienCo;
            if (dto.SoLuongTong < soLuongDangMuon)
            {
                return Conflict($"Số lượng tổng ({dto.SoLuongTong}) không thể nhỏ hơn số lượng đang cho thuê ({soLuongDangMuon}).");
            }

            var oldImagePath = sach.AnhBia;
            var oldTenSach = sach.TenSach;

            // SỬA: Lấy file và cờ Xóa từ DTO
            bool isImageUpload = dto.AnhBiaUpload != null;
            bool isNameChange = dto.TenSach != oldTenSach;

            sach.TenSach = dto.TenSach;
            sach.IdTheLoai = dto.IdTheLoai;
            sach.IdTacGia = dto.IdTacGia;
            sach.IdNhaXuatBan = dto.IdNhaXuatBan;
            sach.NamXuatBan = dto.NamXuatBan;
            sach.MoTa = dto.MoTa;
            sach.SoLuongTong = dto.SoLuongTong;
            sach.SoLuongHienCo = dto.SoLuongTong - soLuongDangMuon;
            sach.GiaBia = dto.GiaBia;
            sach.ViTri = dto.ViTri;

            string newFileName = GenerateFileName(sach.IdSach, sach.TenSach);
            string newRelativePath = $"/{HinhAnhPaths.UrlBooks.TrimStart('/')}/{newFileName}";

            if (isImageUpload)
            {
                DeleteOldImage(oldImagePath);
                // === SỬA LỖI CS8604 (Dòng 303) ===
                // Thêm '!' vì logic 'isImageUpload' đã đảm bảo file không null
                sach.AnhBia = await SaveImageFromFile(dto.AnhBiaUpload!, newFileName, HinhAnhPaths.UrlBooks.TrimStart('/'));
            }
            // SỬA: Lấy cờ Xóa từ DTO
            else if (dto.XoaAnhBia)
            {
                DeleteOldImage(oldImagePath);
                sach.AnhBia = null;
            }
            else if (isNameChange && !string.IsNullOrEmpty(oldImagePath))
            {
                RenameImage(oldImagePath, newRelativePath);
                sach.AnhBia = newRelativePath;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // (Hàm DeleteSach giữ nguyên logic)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSach(int id)
        {
            if (await _context.ChiTietPhieuThues.AnyAsync(ct => ct.IdSach == id && ct.NgayTraThucTe == null))
            {
                return Conflict("Không thể xóa sách. Sách này đang được khách hàng thuê.");
            }

            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            DeleteOldImage(sach.AnhBia); // <-- Xóa file ảnh

            _context.Sachs.Remove(sach);
            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion

        #region API Lịch sử, Tác giả, Thể loại, NXB
        // (Toàn bộ các hàm trong region này giữ nguyên)
        [HttpGet("rentals")]
        public async Task<IActionResult> GetRentalData()
        {
            var dto = new SachRentalsDto
            {
                SachQuaHan = await _context.Database.SqlQuery<BaoCaoSachTreHanDto>(@$"
                    SELECT
                        s.tenSach AS TenSach, kh.hoTen AS HoTen, kh.soDienThoai AS SoDienThoai,
                        pts.ngayThue AS NgayThue, ctpt.ngayHenTra AS NgayHenTra,
                        N'Trễ ' + CAST(DATEDIFF(DAY, ctpt.ngayHenTra, GETDATE()) AS NVARCHAR) + N' ngày' AS TinhTrang
                    FROM dbo.ChiTietPhieuThue ctpt
                    JOIN dbo.PhieuThueSach pts ON ctpt.idPhieuThueSach = pts.idPhieuThueSach
                    JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                    JOIN dbo.KhachHang kh ON pts.idKhachHang = kh.idKhachHang
                    WHERE ctpt.ngayTraThucTe IS NULL AND ctpt.ngayHenTra < GETDATE()
                    ORDER BY ctpt.ngayHenTra ASC;
                ").ToListAsync(),

                LichSuThue = await _context.Database.SqlQuery<LichSuThueDto>(@$"
                    SELECT TOP 50
                        s.tenSach AS TenSach, kh.hoTen AS TenKhachHang, pts.ngayThue AS NgayThue,
                        ctpt.ngayHenTra AS NgayHenTra, ctpt.ngayTraThucTe AS NgayTraThucTe,
                        ISNULL(ctpt.TienPhatTraTre, 0) AS TienPhat,
                        pts.trangThai AS TrangThai
                    FROM dbo.ChiTietPhieuThue ctpt
                    JOIN dbo.PhieuThueSach pts ON ctpt.idPhieuThueSach = pts.idPhieuThueSach
                    JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                    JOIN dbo.KhachHang kh ON pts.idKhachHang = kh.idKhachHang
                    ORDER BY pts.ngayThue DESC;
                ").ToListAsync()
            };
            return Ok(dto);
        }
        [HttpPost("tacgia")]
        public async Task<IActionResult> CreateTacGia([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.TacGias.FirstOrDefaultAsync(t => t.TenTacGia.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên tác giả đã tồn tại.");
            }
            var newEntity = new TacGia { TenTacGia = dto.Ten };
            _context.TacGias.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdTacGia, Ten = newEntity.TenTacGia });
        }

        [HttpPut("tacgia/{id}")]
        public async Task<IActionResult> UpdateTacGia(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.TacGias.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenTacGia = dto.Ten;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("tacgia/{id}")]
        public async Task<IActionResult> DeleteTacGia(int id)
        {
            if (await _context.Sachs.AnyAsync(s => s.IdTacGia == id))
            {
                return Conflict("Không thể xóa. Tác giả này đã được gán cho sách.");
            }
            var entity = await _context.TacGias.FindAsync(id);
            if (entity == null) return NotFound();
            _context.TacGias.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost("theloai")]
        public async Task<IActionResult> CreateTheLoai([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.TheLoais.FirstOrDefaultAsync(t => t.TenTheLoai.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên thể loại đã tồn tại.");
            }

            var newEntity = new TheLoai { TenTheLoai = dto.Ten };
            _context.TheLoais.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdTheLoai, Ten = newEntity.TenTheLoai });
        }

        [HttpPut("theloai/{id}")]
        public async Task<IActionResult> UpdateTheLoai(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.TheLoais.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenTheLoai = dto.Ten;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("theloai/{id}")]
        public async Task<IActionResult> DeleteTheLoai(int id)
        {
            if (await _context.Sachs.AnyAsync(s => s.IdTheLoai == id))
            {
                return Conflict("Không thể xóa. Thể loại này đã được gán cho sách.");
            }
            var entity = await _context.TheLoais.FindAsync(id);
            if (entity == null) return NotFound();
            _context.TheLoais.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost("nhaxuatban")]
        public async Task<IActionResult> CreateNhaXuatBan([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.NhaXuatBans.FirstOrDefaultAsync(t => t.TenNhaXuatBan.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên NXB đã tồn tại.");
            }

            var newEntity = new NhaXuatBan { TenNhaXuatBan = dto.Ten };
            _context.NhaXuatBans.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdNhaXuatBan, Ten = newEntity.TenNhaXuatBan });
        }

        [HttpPut("nhaxuatban/{id}")]
        public async Task<IActionResult> UpdateNhaXuatBan(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.NhaXuatBans.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenNhaXuatBan = dto.Ten;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("nhaxuatban/{id}")]
        public async Task<IActionResult> DeleteNhaXuatBan(int id)
        {
            if (await _context.Sachs.AnyAsync(s => s.IdNhaXuatBan == id))
            {
                return Conflict("Không thể xóa. NXB này đã được gán cho sách.");
            }
            var entity = await _context.NhaXuatBans.FindAsync(id);
            if (entity == null) return NotFound();
            _context.NhaXuatBans.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion
    }
}