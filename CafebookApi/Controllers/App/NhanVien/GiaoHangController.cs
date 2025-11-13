// Tệp: CafebookApi/Controllers/App/NhanVien/GiaoHangController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/giaohang")]
    [ApiController]
    [Authorize]
    public class GiaoHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private Dictionary<string, string> _settings = new Dictionary<string, string>();
        private readonly string _baseUrl;

        public GiaoHangController(CafebookDbContext context, IConfiguration config)
        {
            _context = context;
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url") ?? "http://127.0.0.1:5166";
        }

        private async Task LoadCaiDat()
        {
            _settings = await _context.CaiDats
                .Where(c =>
                    c.TenCaiDat == "TenQuan" ||
                    c.TenCaiDat == "DiaChi" ||
                    c.TenCaiDat == "SoDienThoai")
                .AsNoTracking()
                .ToDictionaryAsync(c => c.TenCaiDat, c => c.GiaTri);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int idNhanVien))
            {
                return idNhanVien;
            }
            return 0;
        }

        /// <summary>
        /// Tải danh sách đơn giao hàng (ĐÃ SỬA LỖI HIỂN THỊ TÊN)
        /// </summary>
        [HttpGet("load")]
        public async Task<IActionResult> LoadGiaoHangData([FromQuery] string? search, [FromQuery] string? status)
        {
            try
            {
                var query = _context.HoaDons
                    .Where(h => h.LoaiHoaDon == "Giao hàng")
                    .Include(h => h.KhachHang)
                    .Include(h => h.NguoiGiaoHang)
                    .OrderByDescending(h => h.ThoiGianTao)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && status != "Tất cả")
                {
                    if (status == "Đã Hủy")
                    {
                        query = query.Where(h => h.TrangThai == "Đã hủy");
                    }
                    else
                    {
                        query = query.Where(h => h.TrangThaiGiaoHang == status);
                    }
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    int.TryParse(search, out int idSearch);

                    query = query.Where(h =>
                        (h.KhachHang != null && h.KhachHang.HoTen.ToLower().Contains(searchLower)) ||
                        (h.SoDienThoaiGiaoHang != null && h.SoDienThoaiGiaoHang.Contains(search)) ||
                        (h.KhachHang != null && h.KhachHang.SoDienThoai != null && h.KhachHang.SoDienThoai.Contains(search)) ||
                        h.IdHoaDon == idSearch
                    );
                }

                var donGiaoHang = await query.Select(h => new GiaoHangItemDto
                {
                    IdHoaDon = h.IdHoaDon,
                    ThoiGianTao = h.ThoiGianTao,

                    // =======================================
                    // === SỬA LỖI HIỂN THỊ TÊN (YÊU CẦU 1) ===
                    // =======================================
                    TenKhachHang = h.KhachHang.HoTen ?? h.DiaChiGiaoHang ?? "Khách giao hàng",
                    SoDienThoaiGiaoHang = h.SoDienThoaiGiaoHang ?? h.KhachHang.SoDienThoai,
                    DiaChiGiaoHang = h.DiaChiGiaoHang ?? h.KhachHang.DiaChi,
                    // =======================================

                    ThanhTien = h.ThanhTien,
                    TrangThaiThanhToan = h.TrangThai,
                    TrangThaiGiaoHang = h.TrangThaiGiaoHang,
                    IdNguoiGiaoHang = h.IdNguoiGiaoHang,
                    TenNguoiGiaoHang = h.NguoiGiaoHang.TenNguoiGiaoHang
                })
                    .ToListAsync();

                var nguoiGiaoHang = await _context.NguoiGiaoHangs
                    .Where(n => n.TrangThai == "Sẵn sàng")
                    .Select(n => new NguoiGiaoHangDto
                    {
                        IdNguoiGiaoHang = n.IdNguoiGiaoHang,
                        TenNguoiGiaoHang = n.TenNguoiGiaoHang
                    })
                    .ToListAsync();

                var dto = new GiaoHangViewDto
                {
                    DonGiaoHang = donGiaoHang,
                    NguoiGiaoHangSanSang = nguoiGiaoHang
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        // ... (Các hàm UpdateGiaoHang, ConfirmAllPendingOrders, GetPrintData, CreateOrUpdateCheBienItems giữ nguyên) ...

        [HttpPost("update/{idHoaDon}")]
        public async Task<IActionResult> UpdateGiaoHang(int idHoaDon, [FromBody] GiaoHangUpdateRequestDto dto)
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                var hoaDon = await _context.HoaDons.FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

                if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");
                if (hoaDon.LoaiHoaDon != "Giao hàng") return BadRequest("Đây không phải là hóa đơn giao hàng.");

                string? trangThaiCu = hoaDon.TrangThaiGiaoHang;

                hoaDon.TrangThaiGiaoHang = dto.TrangThaiGiaoHang;
                hoaDon.IdNguoiGiaoHang = dto.IdNguoiGiaoHang;

                if (dto.IdNguoiGiaoHang.HasValue && string.IsNullOrEmpty(dto.TrangThaiGiaoHang))
                {
                    hoaDon.TrangThaiGiaoHang = "Chờ lấy hàng"; // Sửa: Chờ lấy hàng
                }

                if (dto.TrangThaiGiaoHang == "Hủy")
                {
                    hoaDon.TrangThai = "Đã hủy";
                }

                if (dto.TrangThaiGiaoHang == "Hoàn thành" && hoaDon.TrangThai != "Đã thanh toán")
                {
                    hoaDon.TrangThai = "Đã thanh toán";
                    hoaDon.ThoiGianThanhToan = DateTime.Now;
                    hoaDon.PhuongThucThanhToan = "COD";
                }

                if (dto.TrangThaiGiaoHang == "Đang chuẩn bị" && (trangThaiCu == "Chờ xác nhận" || trangThaiCu == null))
                {
                    await CreateOrUpdateCheBienItems(idHoaDon, idNhanVien);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        [HttpPost("confirm-all-pending")]
        public async Task<IActionResult> ConfirmAllPendingOrders()
        {
            var idNhanVien = GetCurrentUserId();
            if (idNhanVien == 0) return Unauthorized();

            try
            {
                var donHangCho = await _context.HoaDons
                    .Where(h => h.LoaiHoaDon == "Giao hàng" &&
                                (h.TrangThaiGiaoHang == "Chờ xác nhận" || h.TrangThaiGiaoHang == null))
                    .ToListAsync();

                if (!donHangCho.Any())
                {
                    return Ok(new { message = "Không có đơn nào 'Chờ xác nhận'." });
                }

                int count = 0;
                foreach (var hoaDon in donHangCho)
                {
                    hoaDon.TrangThaiGiaoHang = "Đang chuẩn bị";
                    await CreateOrUpdateCheBienItems(hoaDon.IdHoaDon, idNhanVien);
                    count++;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Đã chuyển {count} đơn hàng sang Bếp thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        [HttpGet("print-data/{idHoaDon}")]
        public async Task<IActionResult> GetPrintData(int idHoaDon)
        {
            await LoadCaiDat();
            var hoaDon = await _context.HoaDons
                 .Include(h => h.Ban)
                 .Include(h => h.NhanVien)
                 .Include(h => h.KhachHang)
                 .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.SanPham)
                 .AsNoTracking()
                 .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return NotFound();

            var dto = new PhieuGoiMonPrintDto
            {
                IdPhieu = $"HD{hoaDon.IdHoaDon:D6}",
                TenQuan = _settings.GetValueOrDefault("TenQuan", "Cafebook"),
                DiaChiQuan = _settings.GetValueOrDefault("DiaChi", "N/A"),
                SdtQuan = _settings.GetValueOrDefault("SoDienThoai", "N/A"),
                NgayTao = hoaDon.ThoiGianTao,
                TenNhanVien = hoaDon.NhanVien?.HoTen ?? "Web",
                SoBan = hoaDon.LoaiHoaDon,
                GhiChu = $"KH: {hoaDon.KhachHang?.HoTen ?? "N/A"}\nSĐT: {hoaDon.SoDienThoaiGiaoHang}\nĐịa chỉ: {hoaDon.DiaChiGiaoHang}\nGhi chú: {hoaDon.GhiChu}",

                ChiTiet = hoaDon.ChiTietHoaDons.Select(ct => new ChiTietDto
                {
                    TenSanPham = ct.SanPham.TenSanPham,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.ThanhTien
                }).ToList(),

                TongTienGoc = hoaDon.TongTienGoc,
                GiamGia = hoaDon.GiamGia,
                ThanhTien = hoaDon.ThanhTien
            };

            return Ok(dto);
        }

        private async Task<int> CreateOrUpdateCheBienItems(int idHoaDon, int idNhanVien)
        {
            var hoaDon = await _context.HoaDons
                 .Include(h => h.Ban)
                 .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return 0;

            var chiTietItems = await _context.ChiTietHoaDons
                .Include(c => c.SanPham)
                .Where(c => c.IdHoaDon == idHoaDon)
                .ToListAsync();

            string soBan = hoaDon.LoaiHoaDon;
            int itemsAdded = 0;
            var now = DateTime.Now;

            foreach (var item in chiTietItems)
            {
                bool daTonTai = await _context.TrangThaiCheBiens
                    .AnyAsync(cb => cb.IdChiTietHoaDon == item.IdChiTietHoaDon);

                if (!daTonTai)
                {
                    var newItem = new TrangThaiCheBien
                    {
                        IdChiTietHoaDon = item.IdChiTietHoaDon,
                        IdHoaDon = item.IdHoaDon,
                        IdSanPham = item.IdSanPham,
                        TenMon = item.SanPham.TenSanPham,
                        SoBan = soBan,
                        SoLuong = item.SoLuong,
                        GhiChu = item.GhiChu,
                        NhomIn = item.SanPham.NhomIn,
                        TrangThai = "Chờ làm",
                        ThoiGianGoi = now
                    };
                    _context.TrangThaiCheBiens.Add(newItem);
                    itemsAdded++;
                }
            }

            if (itemsAdded > 0)
            {
                var thongBao = new ThongBao
                {
                    IdNhanVienTao = idNhanVien,
                    NoiDung = $"Đơn Giao Hàng #{idHoaDon} cần chuẩn bị.",
                    ThoiGianTao = DateTime.Now,
                    LoaiThongBao = "PhieuGoiMon",
                    IdLienQuan = idHoaDon,
                    DaXem = false
                };
                _context.ThongBaos.Add(thongBao);
                await _context.SaveChangesAsync();
            }
            return itemsAdded;
        }
    }
}