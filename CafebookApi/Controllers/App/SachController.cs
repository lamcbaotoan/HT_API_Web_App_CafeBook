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
using Microsoft.AspNetCore.Http;
using System.Collections.Generic; // THÊM

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

        // === 5 HÀM HELPER XỬ LÝ FILE (Giữ nguyên) ===
        private string? GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }
        private string GenerateFileName(int id, string ten)
        {
            string slug = SlugifyUtil.GenerateSlug(ten);
            return $"{id}_{slug}.jpg";
        }
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
        private async Task<string> SaveImageFromFile(IFormFile file, string fileName, string relativeDir)
        {
            var saveDir = Path.Combine(_env.WebRootPath, relativeDir);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            var fullSavePath = Path.Combine(saveDir, fileName);
            await using (var stream = new FileStream(fullSavePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"/{relativeDir.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        }
        // =============================

        // SỬA: GetSachFilters (thêm MoTa/GioiThieu)
        [HttpGet("filters")]
        public async Task<IActionResult> GetSachFilters()
        {
            var theLoais = await _context.TheLoais
                .Select(t => new FilterLookupDto { Id = t.IdTheLoai, Ten = t.TenTheLoai, MoTa = t.MoTa })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var tacGias = await _context.TacGias
                .Select(t => new FilterLookupDto { Id = t.IdTacGia, Ten = t.TenTacGia, MoTa = t.GioiThieu })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            var nhaXuatBans = await _context.NhaXuatBans
                .Select(t => new FilterLookupDto { Id = t.IdNhaXuatBan, Ten = t.TenNhaXuatBan, MoTa = t.MoTa })
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

        #region API Sách (Sửa đổi cho Many-to-Many)

        // SỬA: SearchSach (dùng LINQ join chuỗi và lọc 'Any')
        [HttpGet("search")]
        public async Task<IActionResult> SearchSach(
            [FromQuery] string? searchText,
            [FromQuery] int? theLoaiId)
        {
            var query = _context.Sachs.AsQueryable();

            if (theLoaiId.HasValue && theLoaiId > 0)
            {
                // Lọc theo bảng nối
                query = query.Where(s => s.SachTheLoais.Any(stl => stl.IdTheLoai == theLoaiId));
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(s =>
                    s.TenSach.ToLower().Contains(searchLower) ||
                    // Lọc Tác giả theo bảng nối
                    s.SachTacGias.Any(stg => stg.TacGia.TenTacGia.ToLower().Contains(searchLower))
                );
            }

            var rawResults = await query
                .Select(s => new
                {
                    s.IdSach,
                    s.TenSach,
                    // Nối chuỗi Tác giả
                    TenTacGia = string.Join(", ", s.SachTacGias.Select(stg => stg.TacGia.TenTacGia)),
                    // Nối chuỗi Thể loại
                    TenTheLoai = string.Join(", ", s.SachTheLoais.Select(stl => stl.TheLoai.TenTheLoai)),
                    s.ViTri,
                    s.SoLuongTong,
                    s.SoLuongHienCo,
                    s.AnhBia
                })
                .OrderBy(s => s.TenSach)
                .ToListAsync();

            var results = rawResults.Select(s => new SachDto
            {
                IdSach = s.IdSach,
                TenSach = s.TenSach,
                TenTacGia = string.IsNullOrEmpty(s.TenTacGia) ? "N/A" : s.TenTacGia,
                TenTheLoai = string.IsNullOrEmpty(s.TenTheLoai) ? "N/A" : s.TenTheLoai,
                ViTri = s.ViTri,
                SoLuongTong = s.SoLuongTong,
                SoLuongHienCo = s.SoLuongHienCo,
                AnhBiaUrl = GetFullImageUrl(s.AnhBia)
            }).ToList();

            return Ok(results);
        }

        // SỬA: GetSachDetails (lấy List<int> từ các bảng nối)
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetSachDetails(int id)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            var dto = new SachDetailDto
            {
                IdSach = sach.IdSach,
                TenSach = sach.TenSach,
                // Lấy List<int> từ các bảng nối
                IdTheLoais = await _context.SachTheLoais.Where(st => st.IdSach == id).Select(st => st.IdTheLoai).ToListAsync(),
                IdTacGias = await _context.SachTacGias.Where(st => st.IdSach == id).Select(st => st.IdTacGia).ToListAsync(),
                IdNhaXuatBans = await _context.SachNhaXuatBans.Where(sn => sn.IdSach == id).Select(sn => sn.IdNhaXuatBan).ToListAsync(),
                NamXuatBan = sach.NamXuatBan,
                MoTa = sach.MoTa,
                SoLuongTong = sach.SoLuongTong,
                AnhBiaUrl = GetFullImageUrl(sach.AnhBia),
                GiaBia = sach.GiaBia,
                ViTri = sach.ViTri
            };
            return Ok(dto);
        }

        // SỬA: CreateSach (dùng Transaction, lưu vào bảng nối)
        [HttpPost]
        public async Task<IActionResult> CreateSach(
                    [FromForm] SachUpdateRequestDto dto)
        {
            // Dùng Transaction để đảm bảo an toàn dữ liệu
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sach = new Sach
                {
                    TenSach = dto.TenSach,
                    NamXuatBan = dto.NamXuatBan,
                    MoTa = dto.MoTa,
                    SoLuongTong = dto.SoLuongTong,
                    SoLuongHienCo = dto.SoLuongTong,
                    GiaBia = dto.GiaBia,
                    ViTri = dto.ViTri,
                    AnhBia = null
                };
                _context.Sachs.Add(sach);
                await _context.SaveChangesAsync(); // Lưu để lấy IdSach mới

                // Xử lý ảnh (nếu có)
                if (dto.AnhBiaUpload != null)
                {
                    string fileName = GenerateFileName(sach.IdSach, sach.TenSach);
                    string relativePath = await SaveImageFromFile(dto.AnhBiaUpload, fileName, HinhAnhPaths.UrlBooks.TrimStart('/'));
                    sach.AnhBia = relativePath;
                    // (Lưu thay đổi ảnh sẽ ở cuối transaction)
                }

                // SỬA: Thêm vào các bảng nối
                foreach (var id in dto.IdTacGias)
                {
                    _context.SachTacGias.Add(new SachTacGia { IdSach = sach.IdSach, IdTacGia = id });
                }
                foreach (var id in dto.IdTheLoais)
                {
                    _context.SachTheLoais.Add(new SachTheLoai { IdSach = sach.IdSach, IdTheLoai = id });
                }
                foreach (var id in dto.IdNhaXuatBans)
                {
                    _context.SachNhaXuatBans.Add(new SachNhaXuatBan { IdSach = sach.IdSach, IdNhaXuatBan = id });
                }

                await _context.SaveChangesAsync(); // Lưu bảng nối và ảnh
                await transaction.CommitAsync(); // Hoàn tất

                var detailDto = new SachDetailDto
                {
                    IdSach = sach.IdSach,
                    TenSach = sach.TenSach,
                    AnhBiaUrl = GetFullImageUrl(sach.AnhBia)
                };
                return Ok(detailDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Hoàn tác nếu lỗi
                return StatusCode(500, $"Lỗi khi tạo sách: {ex.Message}");
            }
        }

        // SỬA: UpdateSach (dùng Transaction, xóa liên kết cũ, thêm liên kết mới)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSach(int id,
                    [FromForm] SachUpdateRequestDto dto)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            // Dùng Transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int soLuongDangMuon = sach.SoLuongTong - sach.SoLuongHienCo;
                if (dto.SoLuongTong < soLuongDangMuon)
                {
                    return Conflict($"Số lượng tổng ({dto.SoLuongTong}) không thể nhỏ hơn số lượng đang cho thuê ({soLuongDangMuon}).");
                }

                var oldImagePath = sach.AnhBia;
                var oldTenSach = sach.TenSach;

                bool isImageUpload = dto.AnhBiaUpload != null;
                bool isNameChange = dto.TenSach != oldTenSach;

                // Cập nhật các trường đơn giản
                sach.TenSach = dto.TenSach;
                sach.NamXuatBan = dto.NamXuatBan;
                sach.MoTa = dto.MoTa;
                sach.SoLuongTong = dto.SoLuongTong;
                sach.SoLuongHienCo = dto.SoLuongTong - soLuongDangMuon;
                sach.GiaBia = dto.GiaBia;
                sach.ViTri = dto.ViTri;

                // Xử lý ảnh
                string newFileName = GenerateFileName(sach.IdSach, sach.TenSach);
                string newRelativePath = $"/{HinhAnhPaths.UrlBooks.TrimStart('/')}/{newFileName}";

                if (isImageUpload)
                {
                    DeleteOldImage(oldImagePath);
                    sach.AnhBia = await SaveImageFromFile(dto.AnhBiaUpload!, newFileName, HinhAnhPaths.UrlBooks.TrimStart('/'));
                }
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

                // SỬA: Cập nhật các bảng nối (Xóa cũ, thêm mới)
                // 1. Xóa liên kết cũ
                _context.SachTacGias.RemoveRange(_context.SachTacGias.Where(st => st.IdSach == id));
                _context.SachTheLoais.RemoveRange(_context.SachTheLoais.Where(st => st.IdSach == id));
                _context.SachNhaXuatBans.RemoveRange(_context.SachNhaXuatBans.Where(sn => sn.IdSach == id));

                // 2. Thêm liên kết mới
                foreach (var tacGiaId in dto.IdTacGias)
                {
                    _context.SachTacGias.Add(new SachTacGia { IdSach = id, IdTacGia = tacGiaId });
                }
                foreach (var theLoaiId in dto.IdTheLoais)
                {
                    _context.SachTheLoais.Add(new SachTheLoai { IdSach = id, IdTheLoai = theLoaiId });
                }
                foreach (var nxbId in dto.IdNhaXuatBans)
                {
                    _context.SachNhaXuatBans.Add(new SachNhaXuatBan { IdSach = id, IdNhaXuatBan = nxbId });
                }

                await _context.SaveChangesAsync(); // Lưu tất cả thay đổi (sách, ảnh, bảng nối)
                await transaction.CommitAsync(); // Hoàn tất

                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi khi cập nhật sách: {ex.Message}");
            }
        }

        // SỬA: DeleteSach (phải xóa các liên kết trong bảng nối TRƯỚC KHI xóa sách)
        // (Nếu CSDL của bạn đã thiết lập ON DELETE CASCADE thì không cần,
        // nhưng làm tường minh sẽ an toàn hơn)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSach(int id)
        {
            if (await _context.ChiTietPhieuThues.AnyAsync(ct => ct.IdSach == id && ct.NgayTraThucTe == null))
            {
                return Conflict("Không thể xóa sách. Sách này đang được khách hàng thuê.");
            }

            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            // Dùng Transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                DeleteOldImage(sach.AnhBia); // Xóa file ảnh

                // Xóa các liên kết (Không cần nếu có ON DELETE CASCADE)
                _context.SachTacGias.RemoveRange(_context.SachTacGias.Where(st => st.IdSach == id));
                _context.SachTheLoais.RemoveRange(_context.SachTheLoais.Where(st => st.IdSach == id));
                _context.SachNhaXuatBans.RemoveRange(_context.SachNhaXuatBans.Where(sn => sn.IdSach == id));

                // Xóa sách
                _context.Sachs.Remove(sach);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi khi xóa sách: {ex.Message}");
            }
        }

        #endregion

        #region API Lịch sử & CRUD Danh mục (Đã sửa)

        // SỬA: GetRentalData (Thêm lọc ngày)
        [HttpGet("rentals")]
        public async Task<IActionResult> GetRentalData(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var toDateEnd = toDate?.AddDays(1);
            var sqlParams = new List<SqlParameter>();

            // 1. SQL cho Sách Quá Hạn (không lọc ngày)
            string sqlQuaHan = @"
                SELECT
                    s.tenSach AS TenSach, kh.hoTen AS HoTen, kh.soDienThoai AS SoDienThoai,
                    pts.ngayThue AS NgayThue, ctpt.ngayHenTra AS NgayHenTra,
                    N'Trễ ' + CAST(DATEDIFF(DAY, ctpt.ngayHenTra, GETDATE()) AS NVARCHAR) + N' ngày' AS TinhTrang
                FROM dbo.ChiTietPhieuThue ctpt
                JOIN dbo.PhieuThueSach pts ON ctpt.idPhieuThueSach = pts.idPhieuThueSach
                JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                JOIN dbo.KhachHang kh ON pts.idKhachHang = kh.idKhachHang
                WHERE ctpt.ngayTraThucTe IS NULL AND ctpt.ngayHenTra < GETDATE()
                ORDER BY ctpt.ngayHenTra ASC;";

            // 2. SQL cho Lịch sử (có lọc ngày)
            var whereClauses = new List<string>();
            if (fromDate.HasValue)
            {
                whereClauses.Add("pts.ngayThue >= @fromDate");
                sqlParams.Add(new SqlParameter("@fromDate", fromDate.Value));
            }
            if (toDateEnd.HasValue)
            {
                whereClauses.Add("pts.ngayThue < @toDateEnd");
                sqlParams.Add(new SqlParameter("@toDateEnd", toDateEnd.Value));
            }
            string whereSqlLichSu = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            string sqlLichSu = $@"
                SELECT TOP 50
                    s.tenSach AS TenSach, kh.hoTen AS TenKhachHang, pts.ngayThue AS NgayThue,
                    ctpt.ngayHenTra AS NgayHenTra, ctpt.ngayTraThucTe AS NgayTraThucTe,
                    ISNULL(ctpt.TienPhatTraTre, 0) AS TienPhat,
                    pts.trangThai AS TrangThai
                FROM dbo.ChiTietPhieuThue ctpt
                JOIN dbo.PhieuThueSach pts ON ctpt.idPhieuThueSach = pts.idPhieuThueSach
                JOIN dbo.Sach s ON ctpt.idSach = s.idSach
                JOIN dbo.KhachHang kh ON pts.idKhachHang = kh.idKhachHang
                {whereSqlLichSu}
                ORDER BY pts.ngayThue DESC;";

            var dto = new SachRentalsDto
            {
                SachQuaHan = await _context.Database.SqlQueryRaw<BaoCaoSachTreHanDto>(sqlQuaHan).ToListAsync(),
                LichSuThue = await _context.Database.SqlQueryRaw<LichSuThueDto>(sqlLichSu, sqlParams.ToArray()).ToListAsync()
            };
            return Ok(dto);
        }

        // SỬA: CRUD Tác Giả (Thêm GioiThieu)
        [HttpPost("tacgia")]
        public async Task<IActionResult> CreateTacGia([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.TacGias.FirstOrDefaultAsync(t => t.TenTacGia.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên tác giả đã tồn tại.");
            }
            var newEntity = new TacGia
            {
                TenTacGia = dto.Ten,
                GioiThieu = dto.MoTa // <-- SỬA
            };
            _context.TacGias.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdTacGia, Ten = newEntity.TenTacGia, MoTa = newEntity.GioiThieu });
        }

        [HttpPut("tacgia/{id}")]
        public async Task<IActionResult> UpdateTacGia(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.TacGias.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenTacGia = dto.Ten;
            entity.GioiThieu = dto.MoTa; // <-- SỬA
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("tacgia/{id}")]
        public async Task<IActionResult> DeleteTacGia(int id)
        {
            // SỬA: Kiểm tra bảng nối
            if (await _context.SachTacGias.AnyAsync(s => s.IdTacGia == id))
            {
                return Conflict("Không thể xóa. Tác giả này đã được gán cho sách.");
            }
            var entity = await _context.TacGias.FindAsync(id);
            if (entity == null) return NotFound();
            _context.TacGias.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // SỬA: CRUD Thể Loại (Thêm MoTa)
        [HttpPost("theloai")]
        public async Task<IActionResult> CreateTheLoai([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.TheLoais.FirstOrDefaultAsync(t => t.TenTheLoai.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên thể loại đã tồn tại.");
            }

            var newEntity = new TheLoai
            {
                TenTheLoai = dto.Ten,
                MoTa = dto.MoTa // <-- SỬA
            };
            _context.TheLoais.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdTheLoai, Ten = newEntity.TenTheLoai, MoTa = newEntity.MoTa });
        }

        [HttpPut("theloai/{id}")]
        public async Task<IActionResult> UpdateTheLoai(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.TheLoais.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenTheLoai = dto.Ten;
            entity.MoTa = dto.MoTa; // <-- SỬA
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("theloai/{id}")]
        public async Task<IActionResult> DeleteTheLoai(int id)
        {
            // SỬA: Kiểm tra bảng nối
            if (await _context.SachTheLoais.AnyAsync(s => s.IdTheLoai == id))
            {
                return Conflict("Không thể xóa. Thể loại này đã được gán cho sách.");
            }
            var entity = await _context.TheLoais.FindAsync(id);
            if (entity == null) return NotFound();
            _context.TheLoais.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // SỬA: CRUD Nhà Xuất Bản (Thêm MoTa)
        [HttpPost("nhaxuatban")]
        public async Task<IActionResult> CreateNhaXuatBan([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            var existing = await _context.NhaXuatBans.FirstOrDefaultAsync(t => t.TenNhaXuatBan.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên NXB đã tồn tại.");
            }

            var newEntity = new NhaXuatBan
            {
                TenNhaXuatBan = dto.Ten,
                MoTa = dto.MoTa // <-- SỬA
            };
            _context.NhaXuatBans.Add(newEntity);
            await _context.SaveChangesAsync();
            return Ok(new FilterLookupDto { Id = newEntity.IdNhaXuatBan, Ten = newEntity.TenNhaXuatBan, MoTa = newEntity.MoTa });
        }

        [HttpPut("nhaxuatban/{id}")]
        public async Task<IActionResult> UpdateNhaXuatBan(int id, [FromBody] FilterLookupDto dto)
        {
            var entity = await _context.NhaXuatBans.FindAsync(id);
            if (entity == null) return NotFound();
            entity.TenNhaXuatBan = dto.Ten;
            entity.MoTa = dto.MoTa; // <-- SỬA
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("nhaxuatban/{id}")]
        public async Task<IActionResult> DeleteNhaXuatBan(int id)
        {
            // SỬA: Kiểm tra bảng nối
            if (await _context.SachNhaXuatBans.AnyAsync(s => s.IdNhaXuatBan == id))
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