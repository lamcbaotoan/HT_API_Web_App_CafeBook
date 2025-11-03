using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/sanpham")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public SanPhamController(CafebookDbContext context)
        {
            _context = context;
        }

        #region API Sản Phẩm (Giữ nguyên)
        [HttpGet("search")]
        public async Task<IActionResult> SearchSanPham([FromQuery] string? searchText, [FromQuery] int? danhMucId, [FromQuery] bool? trangThai)
        {
            var query = _context.SanPhams.Include(s => s.DanhMuc).AsQueryable();
            if (danhMucId.HasValue && danhMucId > 0)
            {
                query = query.Where(s => s.IdDanhMuc == danhMucId);
            }
            if (trangThai.HasValue)
            {
                query = query.Where(s => s.TrangThaiKinhDoanh == trangThai.Value);
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(s => s.TenSanPham.ToLower().Contains(searchLower));
            }
            var results = await query.Select(s => new SanPhamDto { IdSanPham = s.IdSanPham, TenSanPham = s.TenSanPham, GiaBan = s.GiaBan, TenDanhMuc = s.DanhMuc != null ? s.DanhMuc.TenDanhMuc : "N/A", TrangThaiKinhDoanh = s.TrangThaiKinhDoanh }).OrderBy(s => s.TenSanPham).ToListAsync();
            return Ok(results);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetSanPhamDetails(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();
            var dto = new SanPhamUpdateRequestDto { IdSanPham = sp.IdSanPham, TenSanPham = sp.TenSanPham, IdDanhMuc = sp.IdDanhMuc, GiaBan = sp.GiaBan, MoTa = sp.MoTa, TrangThaiKinhDoanh = sp.TrangThaiKinhDoanh, NhomIn = sp.NhomIn, HinhAnhBase64 = sp.HinhAnh };
            return Ok(dto);
        }
        #endregion

        /// <summary>
        /// API lấy dữ liệu cho các ComboBox lọc
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetSanPhamFilters()
        {
            // --- SỬA LỖI 500 TẠI ĐÂY ---
            // 1. Tải Danh Mucs (Như cũ)
            var danhMucs = await _context.DanhMucs
                .Select(t => new FilterLookupDto { Id = t.IdDanhMuc, Ten = t.TenDanhMuc })
                .OrderBy(t => t.Ten)
                .ToListAsync();

            // 2. Tải Nguyên Liệu (Sửa lỗi LINQ)
            var nguyenLieus = (await _context.NguyenLieus
                .Select(nl => new { nl.IdNguyenLieu, nl.TenNguyenLieu, nl.DonViTinh })
                .ToListAsync()) // 1. Tải về C#
                .Select(nl => new FilterLookupDto // 2. Nối chuỗi trong C#
                {
                    Id = nl.IdNguyenLieu,
                    Ten = nl.TenNguyenLieu + $" ({nl.DonViTinh})"
                })
                .OrderBy(nl => nl.Ten) // 3. Sắp xếp trong C#
                .ToList();

            // 3. Tải Đơn Vị Tính (Như cũ)
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

        /// <summary>
        /// API Thêm sản phẩm mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSanPham([FromBody] SanPhamUpdateRequestDto dto)
        {
            // (Giữ nguyên logic kiểm tra trùng lặp)
            if (await _context.SanPhams.AnyAsync(s =>
                s.TenSanPham.ToLower() == dto.TenSanPham.ToLower() &&
                s.IdDanhMuc == dto.IdDanhMuc))
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
                HinhAnh = dto.HinhAnhBase64
            };
            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();
            return Ok(sanPham); // Trả về sản phẩm đã tạo (chứa Id mới)
        }

        /// <summary>
        /// API Cập nhật sản phẩm
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSanPham(int id, [FromBody] SanPhamUpdateRequestDto dto)
        {
            // (Giữ nguyên hàm này)
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();
            if (await _context.SanPhams.AnyAsync(s => s.TenSanPham.ToLower() == dto.TenSanPham.ToLower() && s.IdDanhMuc == dto.IdDanhMuc && s.IdSanPham != id))
            {
                return Conflict("Tên sản phẩm này đã tồn tại trong danh mục đã chọn.");
            }
            sp.TenSanPham = dto.TenSanPham;
            sp.IdDanhMuc = dto.IdDanhMuc ?? 0;
            sp.GiaBan = dto.GiaBan;
            sp.MoTa = dto.MoTa;
            sp.TrangThaiKinhDoanh = dto.TrangThaiKinhDoanh;
            sp.NhomIn = dto.NhomIn;
            if (dto.HinhAnhBase64 != null)
            {
                sp.HinhAnh = string.IsNullOrEmpty(dto.HinhAnhBase64) ? null : dto.HinhAnhBase64;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// API Xóa sản phẩm
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSanPham(int id)
        {
            // (Giữ nguyên hàm này)
            if (await _context.ChiTietHoaDons.AnyAsync(ct => ct.IdSanPham == id))
            {
                return Conflict("Không thể xóa. Sản phẩm này đã tồn tại trong lịch sử hóa đơn.");
            }
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return NotFound();
            var dinhLuong = await _context.DinhLuongs.Where(d => d.IdSanPham == id).ToListAsync();
            if (dinhLuong.Any())
            {
                _context.DinhLuongs.RemoveRange(dinhLuong);
            }
            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- THÊM MỚI: API CHO DANH MỤC (SỬA LỖI TAB 2) ---
        #region API Danh Mục
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

        // --- API CHO ĐỊNH LƯỢNG (Đã Cập Nhật theo CSDL v2) ---
        #region API Định Lượng

        [HttpGet("{idSanPham}/dinhluong")]
        public async Task<IActionResult> GetDinhLuong(int idSanPham)
        {
            var data = await _context.DinhLuongs
                .Where(d => d.IdSanPham == idSanPham)
                .Include(d => d.NguyenLieu)
                .Include(d => d.DonViSuDung) // Join ĐVT
                .Select(d => new DinhLuongDto
                {
                    IdNguyenLieu = d.IdNguyenLieu,
                    TenNguyenLieu = d.NguyenLieu.TenNguyenLieu,
                    SoLuong = d.SoLuongSuDung, // Lấy từ cột mới
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
                // Cập nhật
                existing.SoLuongSuDung = dto.SoLuong;
                existing.IdDonViSuDung = dto.IdDonViSuDung;
            }
            else
            {
                // Thêm mới
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
            // (Giữ nguyên hàm này)
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