using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App
{
    [Route("api/app/kho")]
    [ApiController]
    public class KhoController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public KhoController(CafebookDbContext context)
        {
            _context = context;
        }

        // --- TAB 1: TỒN KHO ---
        [HttpGet("tonkho")]
        public async Task<IActionResult> GetTonKho()
        {
            var tonKho = await _context.NguyenLieus
                .Select(nl => new NguyenLieuTonKhoDto
                {
                    IdNguyenLieu = nl.IdNguyenLieu,
                    TenNguyenLieu = nl.TenNguyenLieu,
                    TonKho = nl.TonKho,
                    DonViTinh = nl.DonViTinh,
                    TonKhoToiThieu = nl.TonKhoToiThieu,
                    TinhTrang = (nl.TonKho <= 0) ? "Hết hàng" : (nl.TonKho <= nl.TonKhoToiThieu ? "Sắp hết" : "Đủ dùng")
                })
                .OrderBy(nl => nl.TinhTrang)
                .ThenBy(nl => nl.TenNguyenLieu)
                .ToListAsync();
            return Ok(tonKho);
        }

        // --- TAB 2: QUẢN LÝ NGUYÊN LIỆU (CRUD) ---
        #region API Nguyên Liệu
        [HttpGet("nguyenlieu")]
        public async Task<IActionResult> GetAllNguyenLieu()
        {
            var data = await _context.NguyenLieus
                .Select(nl => new NguyenLieuCrudDto
                {
                    IdNguyenLieu = nl.IdNguyenLieu,
                    TenNguyenLieu = nl.TenNguyenLieu,
                    DonViTinh = nl.DonViTinh,
                    TonKhoToiThieu = nl.TonKhoToiThieu,
                    TonKho = nl.TonKho // Sửa lỗi CS0117
                })
                .OrderBy(nl => nl.TenNguyenLieu)
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("nguyenlieu")]
        public async Task<IActionResult> CreateNguyenLieu([FromBody] NguyenLieuUpdateRequestDto dto)
        {
            if (await _context.NguyenLieus.AnyAsync(nl => nl.TenNguyenLieu.ToLower() == dto.TenNguyenLieu.ToLower()))
            {
                return Conflict("Tên nguyên liệu đã tồn tại.");
            }
            var entity = new NguyenLieu
            {
                TenNguyenLieu = dto.TenNguyenLieu,
                DonViTinh = dto.DonViTinh,
                TonKhoToiThieu = dto.TonKhoToiThieu,
                TonKho = 0
            };
            _context.NguyenLieus.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("nguyenlieu/{id}")]
        public async Task<IActionResult> UpdateNguyenLieu(int id, [FromBody] NguyenLieuUpdateRequestDto dto)
        {
            var entity = await _context.NguyenLieus.FindAsync(id);
            if (entity == null) return NotFound();

            if (await _context.NguyenLieus.AnyAsync(nl => nl.TenNguyenLieu.ToLower() == dto.TenNguyenLieu.ToLower() && nl.IdNguyenLieu != id))
            {
                return Conflict("Tên nguyên liệu đã tồn tại.");
            }

            entity.TenNguyenLieu = dto.TenNguyenLieu;
            entity.DonViTinh = dto.DonViTinh;
            entity.TonKhoToiThieu = dto.TonKhoToiThieu;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("nguyenlieu/{id}")]
        public async Task<IActionResult> DeleteNguyenLieu(int id)
        {
            if (await _context.DinhLuongs.AnyAsync(d => d.IdNguyenLieu == id) ||
                await _context.ChiTietNhapKhos.AnyAsync(d => d.IdNguyenLieu == id))
            {
                return Conflict("Không thể xóa. Nguyên liệu này đã được sử dụng trong Định lượng hoặc Phiếu nhập kho.");
            }
            var entity = await _context.NguyenLieus.FindAsync(id);
            if (entity == null) return NotFound();
            _context.NguyenLieus.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
        #endregion

        // --- TAB 3: QUẢN LÝ NHẬP KHO ---
        #region API Nhập Kho
        [HttpGet("phieunhap")]
        public async Task<IActionResult> GetPhieuNhap([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.PhieuNhapKhos
                .Include(p => p.NhaCungCap)
                .Include(p => p.NhanVien)
                .OrderByDescending(p => p.NgayNhap)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.NgayNhap.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(p => p.NgayNhap.Date <= endDate.Value.Date);

            var data = await query.Select(p => new PhieuNhapDto
            {
                IdPhieuNhapKho = p.IdPhieuNhapKho,
                NgayNhap = p.NgayNhap,
                TenNhaCungCap = p.NhaCungCap != null ? p.NhaCungCap.TenNhaCungCap : "Nhập lẻ",
                TenNhanVien = p.NhanVien.HoTen,
                TongTien = p.TongTien,
                TrangThai = p.TrangThai
            }).ToListAsync();
            return Ok(data);
        }

        [HttpGet("phieunhap/{id}")]
        public async Task<IActionResult> GetChiTietPhieuNhap(int id)
        {
            var data = await _context.ChiTietNhapKhos
                .Where(ct => ct.IdPhieuNhapKho == id)
                .Include(ct => ct.NguyenLieu)
                .Select(ct => new ChiTietPhieuNhapDto
                {
                    IdNguyenLieu = ct.IdNguyenLieu,
                    TenNguyenLieu = ct.NguyenLieu.TenNguyenLieu,
                    SoLuongNhap = ct.SoLuongNhap,
                    DonGiaNhap = ct.DonGiaNhap,
                    ThanhTien = ct.ThanhTien
                }).ToListAsync();
            return Ok(data);
        }

        [HttpPost("phieunhap")]
        public async Task<IActionResult> CreatePhieuNhap([FromBody] PhieuNhapCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieuNhap = new PhieuNhapKho
                {
                    IdNhanVien = dto.IdNhanVien,
                    IdNhaCungCap = dto.IdNhaCungCap,
                    NgayNhap = dto.NgayNhap,
                    GhiChu = dto.GhiChu,
                    TrangThai = "Đã nhập",
                    TongTien = dto.ChiTiet.Sum(ct => ct.SoLuongNhap * ct.DonGiaNhap)
                };
                _context.PhieuNhapKhos.Add(phieuNhap);
                await _context.SaveChangesAsync();

                foreach (var ctDto in dto.ChiTiet)
                {
                    var chiTiet = new ChiTietNhapKho
                    {
                        IdPhieuNhapKho = phieuNhap.IdPhieuNhapKho,
                        IdNguyenLieu = ctDto.IdNguyenLieu,
                        SoLuongNhap = ctDto.SoLuongNhap,
                        DonGiaNhap = ctDto.DonGiaNhap
                    };
                    _context.ChiTietNhapKhos.Add(chiTiet);

                    var nguyenLieu = await _context.NguyenLieus.FindAsync(ctDto.IdNguyenLieu);
                    if (nguyenLieu != null)
                    {
                        nguyenLieu.TonKho += ctDto.SoLuongNhap;
                    }
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { IdPhieuNhapKho = phieuNhap.IdPhieuNhapKho });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }
        #endregion

        // --- TAB 4: QUẢN LÝ NHÀ CUNG CẤP (CRUD) ---
        #region API Nhà Cung Cấp
        [HttpGet("nhacungcap")]
        public async Task<IActionResult> GetAllNhaCungCap()
        {
            var data = await _context.NhaCungCaps
                .Select(ncc => new NhaCungCapDto
                {
                    IdNhaCungCap = ncc.IdNhaCungCap,
                    TenNhaCungCap = ncc.TenNhaCungCap,
                    SoDienThoai = ncc.SoDienThoai,
                    DiaChi = ncc.DiaChi,
                    Email = ncc.Email
                })
                .OrderBy(ncc => ncc.TenNhaCungCap)
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("nhacungcap")]
        public async Task<IActionResult> CreateNhaCungCap([FromBody] NhaCungCapDto dto)
        {
            var entity = new NhaCungCap
            {
                TenNhaCungCap = dto.TenNhaCungCap,
                SoDienThoai = dto.SoDienThoai,
                DiaChi = dto.DiaChi,
                Email = dto.Email
            };
            _context.NhaCungCaps.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpPut("nhacungcap/{id}")]
        public async Task<IActionResult> UpdateNhaCungCap(int id, [FromBody] NhaCungCapDto dto)
        {
            var entity = await _context.NhaCungCaps.FindAsync(id);
            if (entity == null) return NotFound();

            entity.TenNhaCungCap = dto.TenNhaCungCap;
            entity.SoDienThoai = dto.SoDienThoai;
            entity.DiaChi = dto.DiaChi;
            entity.Email = dto.Email;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("nhacungcap/{id}")]
        public async Task<IActionResult> DeleteNhaCungCap(int id)
        {
            if (await _context.PhieuNhapKhos.AnyAsync(p => p.IdNhaCungCap == id))
            {
                return Conflict("Không thể xóa. Nhà cung cấp này đã có lịch sử nhập hàng.");
            }
            var entity = await _context.NhaCungCaps.FindAsync(id);
            if (entity == null) return NotFound();
            _context.NhaCungCaps.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok();
        }
        #endregion

        // --- THÊM MỚI: API CHO XUẤT HỦY KHO ---
        #region API Xuất Hủy Kho

        [HttpGet("phieuxuathuy")]
        public async Task<IActionResult> GetPhieuXuatHuy([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.PhieuXuatHuys
                .Include(p => p.NhanVienXuat)
                .OrderByDescending(p => p.NgayXuatHuy)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.NgayXuatHuy.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(p => p.NgayXuatHuy.Date <= endDate.Value.Date);

            var data = await query.Select(p => new PhieuXuatHuyDto
            {
                IdPhieuXuatHuy = p.IdPhieuXuatHuy,
                NgayXuatHuy = p.NgayXuatHuy,
                TenNhanVien = p.NhanVienXuat.HoTen,
                LyDoXuatHuy = p.LyDoXuatHuy,
                TongGiaTriHuy = p.TongGiaTriHuy
            }).ToListAsync();
            return Ok(data);
        }

        [HttpGet("phieuxuathuy/{id}")]
        public async Task<IActionResult> GetChiTietPhieuXuatHuy(int id)
        {
            var data = await _context.ChiTietXuatHuys
                .Where(ct => ct.IdPhieuXuatHuy == id)
                .Include(ct => ct.NguyenLieu)
                .Select(ct => new ChiTietPhieuXuatHuyDto
                {
                    IdNguyenLieu = ct.IdNguyenLieu,
                    TenNguyenLieu = ct.NguyenLieu.TenNguyenLieu,
                    SoLuong = ct.SoLuong,
                    DonGiaVon = ct.DonGiaVon,
                    ThanhTien = ct.ThanhTien
                }).ToListAsync();
            return Ok(data);
        }

        [HttpPost("phieuxuathuy")]
        public async Task<IActionResult> CreatePhieuXuatHuy([FromBody] PhieuXuatHuyCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieuHuy = new PhieuXuatHuy
                {
                    IdNhanVienXuat = dto.IdNhanVien,
                    NgayXuatHuy = dto.NgayXuatHuy,
                    LyDoXuatHuy = dto.LyDoXuatHuy,
                    TongGiaTriHuy = 0 // Sẽ tính toán bên dưới
                };
                _context.PhieuXuatHuys.Add(phieuHuy);
                await _context.SaveChangesAsync(); // Lưu để lấy ID

                decimal tongGiaTri = 0;

                foreach (var ctDto in dto.ChiTiet)
                {
                    var nguyenLieu = await _context.NguyenLieus.FindAsync(ctDto.IdNguyenLieu);
                    if (nguyenLieu == null) continue;

                    // Lấy giá vốn trung bình (đơn giản)
                    var giaVon = await _context.ChiTietNhapKhos
                        .Where(ct => ct.IdNguyenLieu == ctDto.IdNguyenLieu && ct.DonGiaNhap > 0)
                        .Select(ct => (decimal?)ct.DonGiaNhap)
                        .AverageAsync() ?? 0;

                    var chiTiet = new ChiTietXuatHuy
                    {
                        IdPhieuXuatHuy = phieuHuy.IdPhieuXuatHuy,
                        IdNguyenLieu = ctDto.IdNguyenLieu,
                        SoLuong = ctDto.SoLuong,
                        DonGiaVon = giaVon,
                        // ThanhTien sẽ được tính tự động bởi CSDL (Computed Column)
                    };
                    _context.ChiTietXuatHuys.Add(chiTiet);

                    // Trừ tồn kho
                    nguyenLieu.TonKho -= ctDto.SoLuong;
                    tongGiaTri += (ctDto.SoLuong * giaVon);
                }

                phieuHuy.TongGiaTriHuy = tongGiaTri; // Cập nhật tổng tiền
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { idPhieuXuatHuy = phieuHuy.IdPhieuXuatHuy });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        #endregion

        // --- THÊM MỚI: API CHO KIỂM KHO (SỬA LỖI 404) ---
        #region API Kiểm Kho

        [HttpGet("phieukiemkho")]
        public async Task<IActionResult> GetPhieuKiemKho([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.PhieuKiemKhos
                .Include(p => p.NhanVienKiem)
                .OrderByDescending(p => p.NgayKiem)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.NgayKiem.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(p => p.NgayKiem.Date <= endDate.Value.Date);

            var data = await query.Select(p => new PhieuKiemKhoDto
            {
                IdPhieuKiemKho = p.IdPhieuKiemKho,
                NgayKiem = p.NgayKiem,
                TenNhanVienKiem = p.NhanVienKiem.HoTen,
                TrangThai = p.TrangThai
            }).ToListAsync();
            return Ok(data);
        }

        [HttpGet("phieukiemkho/{id}")]
        public async Task<IActionResult> GetChiTietPhieuKiemKho(int id)
        {
            var data = await _context.ChiTietKiemKhos
                .Where(ct => ct.IdPhieuKiemKho == id)
                .Include(ct => ct.NguyenLieu)
                .Select(ct => new ChiTietKiemKhoDto
                {
                    IdNguyenLieu = ct.IdNguyenLieu,
                    TenNguyenLieu = ct.NguyenLieu.TenNguyenLieu,
                    DonViTinh = ct.NguyenLieu.DonViTinh,
                    TonKhoHeThong = ct.TonKhoHeThong,
                    TonKhoThucTe = ct.TonKhoThucTe,
                    LyDoChenhLech = ct.LyDoChenhLech,
                    GiaTriChenhLech = ct.ChenhLech * (_context.ChiTietNhapKhos
                                                        .Where(c => c.IdNguyenLieu == ct.IdNguyenLieu)
                                                        .Select(c => (decimal?)c.DonGiaNhap).Average() ?? 0)
                }).ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// API đặc biệt: Lấy toàn bộ tồn kho hiện tại để bắt đầu kiểm kê
        /// </summary>
        [HttpGet("phieukiemkho/taomoi")]
        public async Task<IActionResult> GetCurrentStockForAudit()
        {
            var data = await _context.NguyenLieus
                .OrderBy(nl => nl.TenNguyenLieu)
                .Select(nl => new ChiTietKiemKhoDto
                {
                    IdNguyenLieu = nl.IdNguyenLieu,
                    TenNguyenLieu = nl.TenNguyenLieu,
                    DonViTinh = nl.DonViTinh,
                    TonKhoHeThong = nl.TonKho,
                    TonKhoThucTe = nl.TonKho, // Mặc định bằng nhau
                    LyDoChenhLech = ""
                })
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("phieukiemkho")]
        public async Task<IActionResult> CreatePhieuKiemKho([FromBody] PhieuKiemKhoCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phieuKiem = new PhieuKiemKho
                {
                    IdNhanVienKiem = dto.IdNhanVien,
                    NgayKiem = dto.NgayKiem,
                    GhiChu = dto.GhiChu,
                    TrangThai = "Hoàn thành"
                };
                _context.PhieuKiemKhos.Add(phieuKiem);
                await _context.SaveChangesAsync(); // Lưu để lấy ID

                foreach (var ctDto in dto.ChiTiet)
                {
                    var chiTiet = new ChiTietKiemKho
                    {
                        IdPhieuKiemKho = phieuKiem.IdPhieuKiemKho,
                        IdNguyenLieu = ctDto.IdNguyenLieu,
                        TonKhoHeThong = ctDto.TonKhoHeThong,
                        TonKhoThucTe = ctDto.TonKhoThucTe,
                        LyDoChenhLech = ctDto.LyDoChenhLech
                        // ChenhLech được tính tự động
                    };
                    _context.ChiTietKiemKhos.Add(chiTiet);

                    // Cân bằng kho: Cập nhật Tồn Kho thực tế
                    if (ctDto.ChenhLech != 0)
                    {
                        var nguyenLieu = await _context.NguyenLieus.FindAsync(ctDto.IdNguyenLieu);
                        if (nguyenLieu != null)
                        {
                            nguyenLieu.TonKho = ctDto.TonKhoThucTe;
                        }
                    }
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { idPhieuKiemKho = phieuKiem.IdPhieuKiemKho });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        #endregion
    }
}