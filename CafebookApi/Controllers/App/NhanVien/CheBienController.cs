// Tệp: CafebookApi/Controllers/App/NhanVien/CheBienController.cs
using CafebookApi.Data;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/chebien")]
    [ApiController]
    public class CheBienController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        public CheBienController(CafebookDbContext context)
        {
            _context = context;
        }

        // ... (Hàm LoadCheBienItems và StartItem giữ nguyên) ...
        [HttpGet("load")]
        public async Task<IActionResult> LoadCheBienItems()
        {
            var items = await _context.TrangThaiCheBiens
                .Where(cb => cb.TrangThai == "Chờ làm" || cb.TrangThai == "Đang làm")
                .OrderBy(cb => cb.ThoiGianGoi)
                .Select(cb => new CheBienItemDto
                {
                    IdTrangThaiCheBien = cb.IdTrangThaiCheBien,
                    IdSanPham = cb.IdSanPham,
                    TenMon = cb.TenMon,
                    SoLuong = cb.SoLuong,
                    SoBan = cb.SoBan,
                    GhiChu = cb.GhiChu,
                    TrangThai = cb.TrangThai,
                    ThoiGianGoi = cb.ThoiGianGoi,
                    NhomIn = cb.NhomIn ?? "Bếp"
                })
                 .ToListAsync();

            return Ok(items);
        }

        [HttpPost("start/{idTrangThaiCheBien}")]
        public async Task<IActionResult> StartItem(int idTrangThaiCheBien)
        {
            var item = await _context.TrangThaiCheBiens.FindAsync(idTrangThaiCheBien);
            if (item == null) return NotFound("Không tìm thấy món.");

            if (item.TrangThai == "Chờ làm")
            {
                item.TrangThai = "Đang làm";
                item.ThoiGianBatDau = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã bắt đầu làm món." });
            }
            return Conflict("Món đã được làm hoặc đã hoàn thành.");
        }

        /// <summary>
        /// Hoàn thành chế biến một món (ĐÃ NÂNG CẤP)
        /// </summary>
        [HttpPost("complete/{idTrangThaiCheBien}")]
        public async Task<IActionResult> CompleteItem(int idTrangThaiCheBien)
        {
            var item = await _context.TrangThaiCheBiens.FindAsync(idTrangThaiCheBien);
            if (item == null) return NotFound("Không tìm thấy món.");

            if (item.TrangThai == "Đang làm")
            {
                item.TrangThai = "Hoàn thành";
                item.ThoiGianHoanThanh = DateTime.Now;
                await _context.SaveChangesAsync();

                // =======================================
                // === CẬP NHẬT TRẠNG THÁI GIAO HÀNG ===
                // =======================================
                await UpdateGiaoHangStatusIfCompleted(item.IdHoaDon);

                return Ok(new { message = "Đã hoàn thành món." });
            }
            return Conflict("Món này chưa được bắt đầu làm.");
        }

        // =======================================
        // === HÀM HELPER (ĐÃ SỬA THEO YÊU CẦU 1) ===
        // =======================================
        private async Task UpdateGiaoHangStatusIfCompleted(int idHoaDon)
        {
            try
            {
                // 1. Kiểm tra xem còn món nào CHƯA hoàn thành không
                bool allDone = !await _context.TrangThaiCheBiens
                    .AnyAsync(cb => cb.IdHoaDon == idHoaDon && cb.TrangThai != "Hoàn thành");

                if (allDone)
                {
                    var hoaDon = await _context.HoaDons.FindAsync(idHoaDon);

                    // 2. Chỉ cập nhật nếu là đơn "Giao hàng" VÀ đang ở "Đang chuẩn bị"
                    if (hoaDon != null &&
                        hoaDon.LoaiHoaDon == "Giao hàng" &&
                        hoaDon.TrangThaiGiaoHang == "Đang chuẩn bị")
                    {
                        // SỬA: Chuyển thành "Chờ lấy hàng"
                        hoaDon.TrangThaiGiaoHang = "Chờ lấy hàng";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateGiaoHangStatusIfCompleted Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy công thức cho một món ăn (Giữ nguyên)
        /// </summary>
        [HttpGet("congthuc/{idSanPham}")]
        public async Task<IActionResult> GetCongThuc(int idSanPham)
        {
            var items = await _context.DinhLuongs
                .Where(d => d.IdSanPham == idSanPham)
                 .Include(d => d.NguyenLieu)
                 .Include(d => d.DonViSuDung)
                 .Select(d => new CongThucItemDto
                 {
                     TenNguyenLieu = d.NguyenLieu.TenNguyenLieu,
                     SoLuongSuDung = d.SoLuongSuDung,
                     TenDonVi = d.DonViSuDung.TenDonVi
                 })
                .ToListAsync();
            return Ok(items);
        }
    }
}