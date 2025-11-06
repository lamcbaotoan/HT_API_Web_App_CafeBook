using CafebookApi.Data;
using CafebookModel.Model.ModelApp;
using CafebookModel.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // <-- THÊM
using CafebookModel.Model.ModelApp.NhanVien; // <-- THÊM

namespace CafebookApi.Controllers.App
{
    [Route("api/app/donhang")]
    [ApiController]
    public class DonHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        // THÊM: Cần cho việc In lại
        private Dictionary<string, string> _settings = new Dictionary<string, string>();

        public DonHangController(CafebookDbContext context)
        {
            _context = context;
        }

        // ### THÊM MỚI: Helper (Copy từ ThanhToanController) ###
        private async Task LoadCaiDat()
        {
            _settings = await _context.CaiDats
                .Where(c =>
                    c.TenCaiDat == "TenQuan" ||
                    c.TenCaiDat == "DiaChi" ||
                    c.TenCaiDat == "SoDienThoai" ||
                    c.TenCaiDat == "Wifi_MatKhau")
                .AsNoTracking()
                .ToDictionaryAsync(c => c.TenCaiDat, c => c.GiaTri);
        }

        /// <summary>
        /// API lấy dữ liệu cho các ComboBox lọc
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetDonHangFilters()
        {
            var dto = new DonHangFiltersDto
            {
                NhanViens = await _context.NhanViens
                    .Select(nv => new FilterLookupDto { Id = nv.IdNhanVien, Ten = nv.HoTen })
                    .OrderBy(nv => nv.Ten)
                    .ToListAsync(),
                KhachHangs = await _context.KhachHangs
                    .Select(kh => new FilterLookupDto { Id = kh.IdKhachHang, Ten = kh.HoTen })
                    .OrderBy(kh => kh.Ten)
                    .ToListAsync()
            };
            return Ok(dto);
        }

        /// <summary>
        /// API Lấy danh sách Đơn hàng (có lọc)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchDonHang(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? trangThai,
            [FromQuery] int? nhanVienId,
            [FromQuery] string? searchText)
        {
            var query = _context.HoaDons
                .Include(h => h.NhanVien)
                .Include(h => h.KhachHang)
                .Include(h => h.Ban)
                .Where(h => h.ThoiGianTao >= startDate && h.ThoiGianTao < endDate.AddDays(1))
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(h => h.TrangThai == trangThai);
            }

            if (nhanVienId.HasValue && nhanVienId > 0)
            {
                query = query.Where(h => h.IdNhanVien == nhanVienId);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                query = query.Where(h =>
                    h.IdHoaDon.ToString().Contains(searchLower) ||
                    (h.KhachHang != null && h.KhachHang.HoTen.ToLower().Contains(searchLower)) ||
                    (h.KhachHang != null && h.KhachHang.SoDienThoai != null && h.KhachHang.SoDienThoai.Contains(searchLower))
                );
            }

            var results = await query
                .OrderByDescending(h => h.ThoiGianTao)
                .Select(h => new DonHangDto
                {
                    IdHoaDon = h.IdHoaDon,
                    ThoiGianTao = h.ThoiGianTao,
                    TenNhanVien = h.NhanVien.HoTen,
                    TenKhachHang = h.KhachHang != null ? h.KhachHang.HoTen : "Khách vãng lai",
                    SoBan = h.Ban != null ? h.Ban.SoBan : "Mang về/Giao",
                    ThanhTien = h.ThanhTien,
                    TrangThai = h.TrangThai,
                    LoaiHoaDon = h.LoaiHoaDon
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// API Lấy chi tiết một Đơn hàng (Món + Phụ thu)
        /// </summary>
        // ### SỬA: Thay đổi hàm này ###
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDonHangDetails(int id)
        {
            var items = await _context.ChiTietHoaDons
                .Where(ct => ct.IdHoaDon == id)
                .Include(ct => ct.SanPham)
                .Select(ct => new DonHangChiTietDto
                {
                    TenSanPham = ct.SanPham.TenSanPham,
                    SoLuong = ct.SoLuong,
                    ThanhTien = ct.ThanhTien
                })
                .ToListAsync();

            var surcharges = await _context.ChiTietPhuThuHoaDons
                .Where(pt => pt.IdHoaDon == id)
                .Include(pt => pt.PhuThu)
                .Select(pt => new PhuThuDto
                {
                    IdPhuThu = pt.IdPhuThu,
                    TenPhuThu = pt.PhuThu.TenPhuThu,
                    SoTien = pt.SoTien,
                    GiaTri = pt.PhuThu.GiaTri,
                    LoaiGiaTri = pt.PhuThu.LoaiGiaTri
                })
                .ToListAsync();

            return Ok(new DonHangFullDetailsDto
            {
                Items = items,
                Surcharges = surcharges
            });
        }

        // ### THÊM MỚI: API Lấy dữ liệu In lại ###
        [HttpGet("reprint-data/{id}")]
        public async Task<IActionResult> GetReprintData(int id)
        {
            await LoadCaiDat();

            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .Include(h => h.NhanVien)
                .Include(h => h.KhachHang)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == id);

            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");

            var items = await _context.ChiTietHoaDons
                .Where(ct => ct.IdHoaDon == id)
                .AsNoTracking()
                .Select(c => new ChiTietDto
                {
                    IdChiTietHoaDon = c.IdChiTietHoaDon,
                    IdSanPham = c.IdSanPham,
                    TenSanPham = c.SanPham.TenSanPham,
                    SoLuong = c.SoLuong,
                    DonGia = c.DonGia,
                    ThanhTien = c.ThanhTien
                }).ToListAsync();

            var surcharges = await _context.ChiTietPhuThuHoaDons
                .Where(pt => pt.IdHoaDon == id)
                .AsNoTracking()
                .Select(pt => new PhuThuDto
                {
                    IdPhuThu = pt.IdPhuThu,
                    TenPhuThu = pt.PhuThu.TenPhuThu,
                    SoTien = pt.SoTien,
                    GiaTri = pt.PhuThu.GiaTri,
                    LoaiGiaTri = pt.PhuThu.LoaiGiaTri
                }).ToListAsync();

            // Lấy giao dịch thanh toán (để lấy tiền khách đưa)
            var payment = await _context.GiaoDichThanhToans
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.IdHoaDon == id);

            decimal khachDua = (payment?.CongThanhToan == "Tiền mặt") ? payment.SoTien : hoaDon.ThanhTien;

            var previewData = new HoaDonPreviewDto
            {
                IsProvisional = false, // Luôn là hóa đơn cuối cùng
                TenQuan = _settings.GetValueOrDefault("TenQuan", "CafeBook"),
                DiaChi = _settings.GetValueOrDefault("DiaChi", "N/A"),
                SoDienThoai = _settings.GetValueOrDefault("SoDienThoai", "N/A"),
                WifiMatKhau = _settings.GetValueOrDefault("Wifi_MatKhau", "N/A"),

                IdHoaDon = hoaDon.IdHoaDon,
                SoBan = hoaDon.Ban?.SoBan ?? hoaDon.LoaiHoaDon,
                ThoiGianTao = hoaDon.ThoiGianTao,
                TenNhanVien = hoaDon.NhanVien?.HoTen ?? "N/A",
                TenKhachHang = hoaDon.KhachHang?.HoTen ?? "Khách vãng lai",

                Items = items,
                Surcharges = surcharges,

                TongTienGoc = hoaDon.TongTienGoc,
                TongPhuThu = hoaDon.TongPhuThu,
                // Gộp tất cả giảm giá vào KM (vì không biết cái nào là điểm)
                GiamGiaKM = hoaDon.GiamGia,
                GiamGiaDiem = 0,
                ThanhTien = hoaDon.ThanhTien,

                PhuongThucThanhToan = hoaDon.PhuongThucThanhToan ?? "N/A",
                KhachDua = khachDua,
                TienThoi = khachDua - hoaDon.ThanhTien
            };

            return Ok(previewData);
        }


        /// <summary>
        /// API Cập nhật trạng thái (Hủy, Giao hàng)
        /// </summary>
        [HttpPut("update-status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string newStatus)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban) // Giữ nguyên
                .FirstOrDefaultAsync(h => h.IdHoaDon == id);

            if (hoaDon == null) return NotFound();

            if (hoaDon.TrangThai == "Đã thanh toán")
            {
                return Conflict("Không thể cập nhật trạng thái cho hóa đơn đã thanh toán.");
            }

            if (newStatus == "Hủy")
            {
                hoaDon.TrangThai = "Đã hủy";

                if (hoaDon.Ban != null)
                {
                    hoaDon.Ban.TrangThai = "Trống";
                }
            }
            else if (newStatus == "Đang giao")
            {
                if (hoaDon.LoaiHoaDon != "Giao hàng")
                {
                    return BadRequest("Chỉ có thể giao hàng cho 'Đơn Giao Hàng'.");
                }
                if (string.IsNullOrEmpty(hoaDon.DiaChiGiaoHang) || string.IsNullOrEmpty(hoaDon.SoDienThoaiGiaoHang))
                {
                    return BadRequest("Không thể giao hàng. Thiếu địa chỉ hoặc SĐT.");
                }
                hoaDon.TrangThaiGiaoHang = "Đang giao";
            }
            else
            {
                return BadRequest("Trạng thái mới không hợp lệ.");
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}