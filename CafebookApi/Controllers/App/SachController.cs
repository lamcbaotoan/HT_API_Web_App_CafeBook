using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/sach")]
    [ApiController]
    public class SachController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public SachController(CafebookDbContext context)
        {
            _context = context;
        }

        // --- CÁC API SÁCH (Đã có) ---
        #region API Sách

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

            var results = await query
                .Select(s => new SachDto
                {
                    IdSach = s.IdSach,
                    TenSach = s.TenSach,
                    TenTacGia = s.TacGia != null ? s.TacGia.TenTacGia : "N/A",
                    TenTheLoai = s.TheLoai != null ? s.TheLoai.TenTheLoai : "N/A",
                    ViTri = s.ViTri,
                    SoLuongTong = s.SoLuongTong,
                    SoLuongHienCo = s.SoLuongHienCo
                })
                .OrderBy(s => s.TenSach)
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetSachDetails(int id)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            var dto = new SachUpdateRequestDto
            {
                IdSach = sach.IdSach,
                TenSach = sach.TenSach,
                IdTheLoai = sach.IdTheLoai,
                IdTacGia = sach.IdTacGia,
                IdNhaXuatBan = sach.IdNhaXuatBan,
                NamXuatBan = sach.NamXuatBan,
                MoTa = sach.MoTa,
                SoLuongTong = sach.SoLuongTong,
                AnhBiaBase64 = sach.AnhBia,
                GiaBia = sach.GiaBia,
                ViTri = sach.ViTri
            };
            return Ok(dto);
        }

        [HttpGet("filters")]
        public async Task<IActionResult> GetSachFilters()
        {
            var dto = new SachFiltersDto
            {
                TheLoais = await _context.TheLoais
                    .Select(t => new FilterLookupDto { Id = t.IdTheLoai, Ten = t.TenTheLoai })
                    .OrderBy(t => t.Ten)
                    .ToListAsync(),
                TacGias = await _context.TacGias
                    .Select(t => new FilterLookupDto { Id = t.IdTacGia, Ten = t.TenTacGia })
                    .OrderBy(t => t.Ten)
                    .ToListAsync(),
                NhaXuatBans = await _context.NhaXuatBans
                    .Select(t => new FilterLookupDto { Id = t.IdNhaXuatBan, Ten = t.TenNhaXuatBan })
                    .OrderBy(t => t.Ten)
                    .ToListAsync()
            };
            return Ok(dto);
        }

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

        [HttpPost]
        public async Task<IActionResult> CreateSach([FromBody] SachUpdateRequestDto dto)
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
                AnhBia = dto.AnhBiaBase64,
                GiaBia = dto.GiaBia,
                ViTri = dto.ViTri
            };
            _context.Sachs.Add(sach);
            await _context.SaveChangesAsync();
            return Ok(sach);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSach(int id, [FromBody] SachUpdateRequestDto dto)
        {
            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            int soLuongDangMuon = sach.SoLuongTong - sach.SoLuongHienCo;
            if (dto.SoLuongTong < soLuongDangMuon)
            {
                return Conflict($"Số lượng tổng ({dto.SoLuongTong}) không thể nhỏ hơn số lượng đang cho thuê ({soLuongDangMuon}).");
            }

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

            if (dto.AnhBiaBase64 != null)
            {
                sach.AnhBia = dto.AnhBiaBase64;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSach(int id)
        {
            if (await _context.ChiTietPhieuThues.AnyAsync(ct => ct.IdSach == id && ct.NgayTraThucTe == null))
            {
                return Conflict("Không thể xóa sách. Sách này đang được khách hàng thuê.");
            }

            var sach = await _context.Sachs.FindAsync(id);
            if (sach == null) return NotFound();

            _context.Sachs.Remove(sach);
            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion

        // --- CÁC API MỚI CHO TÁC GIẢ, THỂ LOẠI, NXB ---
        #region API Tác Giả

        [HttpPost("tacgia")]
        public async Task<IActionResult> CreateTacGia([FromBody] FilterLookupDto dto)
        {
            if (string.IsNullOrEmpty(dto.Ten)) return BadRequest("Tên không được rỗng.");
            // Kiểm tra trùng lặp không phân biệt hoa thường
            var existing = await _context.TacGias.FirstOrDefaultAsync(t => t.TenTacGia.ToLower() == dto.Ten.ToLower());
            if (existing != null)
            {
                return Conflict("Tên tác giả đã tồn tại.");
            }

            var newEntity = new TacGia { TenTacGia = dto.Ten };
            _context.TacGias.Add(newEntity);
            await _context.SaveChangesAsync();
            // Trả về DTO
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

        #endregion

        #region API Thể Loại

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

        #endregion

        #region API Nhà Xuất Bản

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