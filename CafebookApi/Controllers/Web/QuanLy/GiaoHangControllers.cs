using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web.QuanLy
{
    [Route("api/web/quanly/giaohang")]
    [ApiController]
    [Authorize] // Yêu cầu đăng nhập (Nhân viên/Quản lý)
    public class GiaoHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IWebHostEnvironment _env;

        public GiaoHangController(CafebookDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Tải danh sách đơn hàng
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

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(status) && status != "Tất cả")
                {
                    if (status == "Đã Hủy")
                        query = query.Where(h => h.TrangThai == "Đã hủy");
                    else
                        query = query.Where(h => h.TrangThaiGiaoHang == status);
                }

                // Tìm kiếm
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    int.TryParse(search, out int idSearch);
                    query = query.Where(h =>
                        (h.KhachHang != null && h.KhachHang.HoTen.ToLower().Contains(searchLower)) ||
                        (h.SoDienThoaiGiaoHang != null && h.SoDienThoaiGiaoHang.Contains(search)) ||
                        h.IdHoaDon == idSearch
                    );
                }

                var donGiaoHang = await query.Select(h => new GiaoHangItemDto
                {
                    IdHoaDon = h.IdHoaDon,
                    ThoiGianTao = h.ThoiGianTao,
                    TenKhachHang = h.KhachHang.HoTen ?? h.DiaChiGiaoHang ?? "Khách lẻ",
                    SoDienThoaiGiaoHang = h.SoDienThoaiGiaoHang ?? h.KhachHang.SoDienThoai,
                    DiaChiGiaoHang = h.DiaChiGiaoHang ?? h.KhachHang.DiaChi,
                    ThanhTien = h.ThanhTien,
                    TrangThaiThanhToan = h.TrangThai,
                    TrangThaiGiaoHang = h.TrangThaiGiaoHang,
                    IdNguoiGiaoHang = h.IdNguoiGiaoHang,
                    TenNguoiGiaoHang = h.NguoiGiaoHang.TenNguoiGiaoHang,
                    GhiChu = h.GhiChu,

                    // --- THÊM DÒNG NÀY ---
                    IdNhanVien = h.IdNhanVien
                }).ToListAsync();

                var shippers = await _context.NguoiGiaoHangs
                    .Where(n => n.TrangThai == "Sẵn sàng")
                    .Select(n => new NguoiGiaoHangDto { IdNguoiGiaoHang = n.IdNguoiGiaoHang, TenNguoiGiaoHang = n.TenNguoiGiaoHang })
                    .ToListAsync();

                return Ok(new GiaoHangViewDto { DonGiaoHang = donGiaoHang, NguoiGiaoHangSanSang = shippers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // POST: Cập nhật trạng thái (Xác nhận, Giao, Hoàn thành + Ảnh)
        [HttpPost("update/{idHoaDon}")]
        public async Task<IActionResult> UpdateStatus(int idHoaDon, [FromForm] GiaoHangUpdateRequestDto dto)
        {
            var hoaDon = await _context.HoaDons.FindAsync(idHoaDon);
            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");

            // 1. Cập nhật Shipper (Công ty)
            if (dto.IdNguoiGiaoHang.HasValue) hoaDon.IdNguoiGiaoHang = dto.IdNguoiGiaoHang;

            // 2. --- THÊM MỚI: Cập nhật Nhân viên (Người Shipper cụ thể) ---
            if (dto.IdNhanVien.HasValue) hoaDon.IdNhanVien = dto.IdNhanVien;

            // 3. Cập nhật Trạng thái Giao hàng
            if (!string.IsNullOrEmpty(dto.TrangThaiGiaoHang))
            {
                hoaDon.TrangThaiGiaoHang = dto.TrangThaiGiaoHang;

                if (dto.TrangThaiGiaoHang == "Hủy")
                {
                    hoaDon.TrangThai = "Đã hủy";
                    if (hoaDon.IdBan.HasValue)
                    {
                        var ban = await _context.Bans.FindAsync(hoaDon.IdBan);
                        if (ban != null) ban.TrangThai = "Trống";
                    }
                }
                else if (dto.TrangThaiGiaoHang == "Hoàn thành")
                {
                    if (hoaDon.TrangThai != "Đã thanh toán")
                    {
                        hoaDon.TrangThai = "Đã thanh toán";
                        hoaDon.ThoiGianThanhToan = DateTime.Now;
                        if (string.IsNullOrEmpty(hoaDon.PhuongThucThanhToan)) hoaDon.PhuongThucThanhToan = "COD";
                    }

                    // --- XỬ LÝ LƯU ẢNH VÀO THƯ MỤC CHỈ ĐỊNH ---
                    if (dto.HinhAnhXacNhan != null && dto.HinhAnhXacNhan.Length > 0)
                    {
                        try
                        {
                            // Đường dẫn: wwwroot/images/anhgiaohang/
                            var folderName = Path.Combine("images", "anhgiaohang");
                            var pathToSave = Path.Combine(_env.WebRootPath, folderName);

                            if (!Directory.Exists(pathToSave)) Directory.CreateDirectory(pathToSave);

                            var fileName = $"GH_{idHoaDon}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(dto.HinhAnhXacNhan.FileName)}";
                            var fullPath = Path.Combine(pathToSave, fileName);

                            // Lưu file
                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                await dto.HinhAnhXacNhan.CopyToAsync(stream);
                            }

                            // Lưu đường dẫn ảnh vào GhiChu của Hóa Đơn
                            var dbPath = Path.Combine(folderName, fileName).Replace("\\", "/");
                            hoaDon.GhiChu = (hoaDon.GhiChu ?? "") + $" | Ảnh xác nhận: /{dbPath}";
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, $"Lỗi lưu ảnh: {ex.Message}");
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công." });
        }

        // POST: Xác nhận tất cả đơn chờ
        [HttpPost("confirm-all-pending")]
        public async Task<IActionResult> ConfirmAll()
        {
            var pendingOrders = await _context.HoaDons
                .Where(h => h.LoaiHoaDon == "Giao hàng" && (h.TrangThaiGiaoHang == "Chờ xác nhận" || h.TrangThaiGiaoHang == null))
                .ToListAsync();

            if (!pendingOrders.Any()) return Ok(new { message = "Không có đơn nào cần xác nhận." });

            foreach (var order in pendingOrders)
            {
                order.TrangThaiGiaoHang = "Đang chuẩn bị";
                await CreateKitchenTicket(order.IdHoaDon);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xác nhận {pendingOrders.Count} đơn hàng." });
        }

        // Helper: Tạo phiếu bếp (để báo bếp làm món)
        private async Task CreateKitchenTicket(int idHoaDon)
        {
            var details = await _context.ChiTietHoaDons
                .Include(c => c.SanPham)
                .Where(c => c.IdHoaDon == idHoaDon)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var item in details)
            {
                bool exists = await _context.TrangThaiCheBiens.AnyAsync(t => t.IdChiTietHoaDon == item.IdChiTietHoaDon);
                if (!exists)
                {
                    _context.TrangThaiCheBiens.Add(new TrangThaiCheBien
                    {
                        IdChiTietHoaDon = item.IdChiTietHoaDon,
                        IdHoaDon = idHoaDon,
                        IdSanPham = item.IdSanPham,
                        TenMon = item.SanPham.TenSanPham,
                        SoBan = "Giao hàng",
                        SoLuong = item.SoLuong,
                        GhiChu = item.GhiChu,
                        NhomIn = item.SanPham.NhomIn,
                        TrangThai = "Chờ làm",
                        ThoiGianGoi = now
                    });
                }
            }
            // Tạo thông báo cho bếp
            _context.ThongBaos.Add(new ThongBao
            {
                NoiDung = $"Đơn giao hàng #{idHoaDon} cần chuẩn bị.",
                LoaiThongBao = "PhieuGoiMon",
                IdLienQuan = idHoaDon,
                ThoiGianTao = now,
                DaXem = false
            });
        }

        [HttpGet("shipper-history")]
        public async Task<IActionResult> GetShipperHistory()
        {
            try
            {
                // --- SỬA ĐOẠN NÀY ---
                // Thử lấy ID từ các loại Claim phổ biến (NameIdentifier thường map với 'sub' trong JWT)
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("IdNhanVien")
                              ?? User.FindFirst("sub");

                int idNhanVien = 0;
                if (idClaim != null) int.TryParse(idClaim.Value, out idNhanVien);

                if (idNhanVien == 0) return Unauthorized();
                // --- KẾT THÚC SỬA ---

                var today = DateTime.Today;

                // (Phần code truy vấn bên dưới giữ nguyên)
                var query = await _context.HoaDons
                    .Where(h => h.IdNhanVien == idNhanVien &&
                                h.LoaiHoaDon == "Giao hàng" &&
                                h.ThoiGianTao >= today &&
                                (h.TrangThaiGiaoHang == "Hoàn thành" || h.TrangThaiGiaoHang == "Đã hủy"))
                    .Include(h => h.KhachHang)
                    .OrderByDescending(h => h.ThoiGianThanhToan ?? h.ThoiGianTao)
                    .ToListAsync();

                var historyDto = new ShipperHistorySummaryDto
                {
                    TongTienMatCam = query.Where(h => h.TrangThaiGiaoHang == "Hoàn thành" && h.PhuongThucThanhToan == "COD")
                                          .Sum(h => h.ThanhTien),

                    TongDonHoanThanh = query.Count(h => h.TrangThaiGiaoHang == "Hoàn thành"),

                    TongDonHuy = query.Count(h => h.TrangThai == "Đã hủy"),

                    LichSuDonHang = query.Select(h => new GiaoHangItemDto
                    {
                        IdHoaDon = h.IdHoaDon,
                        ThoiGianTao = h.ThoiGianTao,
                        TenKhachHang = h.KhachHang?.HoTen ?? h.DiaChiGiaoHang,
                        DiaChiGiaoHang = h.DiaChiGiaoHang,
                        ThanhTien = h.ThanhTien,
                        TrangThaiGiaoHang = h.TrangThaiGiaoHang,
                        GhiChu = h.PhuongThucThanhToan
                    }).ToList()
                };

                return Ok(historyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}