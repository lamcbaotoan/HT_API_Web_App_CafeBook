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

// Cần các using này
using System.Collections.Generic;
using System.Net.Http;


namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/giaohang")]
    [ApiController]
    [Authorize] // Yêu cầu nhân viên phải đăng nhập
    public class GiaoHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public GiaoHangController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tải danh sách đơn giao hàng và người giao hàng
        /// </summary>
        [HttpGet("load")]
        public async Task<IActionResult> LoadGiaoHangData()
        {
            try
            {
                // 1. Lấy danh sách đơn hàng
                var donGiaoHang = await _context.HoaDons
                    .Where(h => h.LoaiHoaDon == "Giao hàng")
                    .Include(h => h.KhachHang)
                    .Include(h => h.NguoiGiaoHang)
                    .OrderByDescending(h => h.ThoiGianTao)
                    .Select(h => new GiaoHangItemDto
                    {
                        IdHoaDon = h.IdHoaDon,
                        // *** SỬA LỖI CS0019: Xóa '??' vì h.ThoiGianTao được coi là non-nullable
                        ThoiGianTao = h.ThoiGianTao,
                        TenKhachHang = h.DiaChiGiaoHang ?? h.KhachHang.HoTen ?? "Khách giao hàng",
                        SoDienThoaiGiaoHang = h.SoDienThoaiGiaoHang ?? h.KhachHang.SoDienThoai,
                        DiaChiGiaoHang = h.DiaChiGiaoHang ?? h.KhachHang.DiaChi,
                        // *** SỬA LỖI CS0019: Xóa '?? 0m' vì h.ThanhTien được coi là non-nullable
                        ThanhTien = h.ThanhTien,
                        TrangThaiThanhToan = h.TrangThai,
                        TrangThaiGiaoHang = h.TrangThaiGiaoHang,
                        IdNguoiGiaoHang = h.IdNguoiGiaoHang,
                        TenNguoiGiaoHang = h.NguoiGiaoHang.TenNguoiGiaoHang
                    })
                    .ToListAsync();

                // 2. Lấy danh sách người giao hàng
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

        /// <summary>
        /// Cập nhật trạng thái hoặc người giao hàng cho 1 đơn
        /// </summary>
        [HttpPost("update/{idHoaDon}")]
        public async Task<IActionResult> UpdateGiaoHang(int idHoaDon, [FromBody] GiaoHangUpdateRequestDto dto)
        {
            try
            {
                int idNhanVien = GetCurrentUserId();
                var hoaDon = await _context.HoaDons.FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

                if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");
                if (hoaDon.LoaiHoaDon != "Giao hàng") return BadRequest("Đây không phải là hóa đơn giao hàng.");

                hoaDon.TrangThaiGiaoHang = dto.TrangThaiGiaoHang;
                hoaDon.IdNguoiGiaoHang = dto.IdNguoiGiaoHang;

                if (dto.IdNguoiGiaoHang.HasValue && string.IsNullOrEmpty(dto.TrangThaiGiaoHang))
                {
                    hoaDon.TrangThaiGiaoHang = "Chờ giao";
                }

                if (dto.TrangThaiGiaoHang == "Hoàn thành" && hoaDon.TrangThai != "Đã thanh toán")
                {
                    hoaDon.TrangThai = "Đã thanh toán";
                    hoaDon.ThoiGianThanhToan = DateTime.Now;
                    hoaDon.PhuongThucThanhToan = "COD";
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ: {ex.Message}");
            }
        }

        // === HÀM HELPER ===
        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "IdNhanVien");
            if (idClaim != null && int.TryParse(idClaim.Value, out int idNhanVien))
            {
                return idNhanVien;
            }
            return 0;
        }
    }
}