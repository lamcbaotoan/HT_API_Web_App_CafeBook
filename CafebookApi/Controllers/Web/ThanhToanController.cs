using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/thanhtoan")]
    [ApiController]
    [Authorize(Roles = "KhachHang")]
    public class ThanhToanController : ControllerBase
    {
        // ... (Constructor và GetCurrentUserId giữ nguyên) ...
        private readonly CafebookDbContext _context;

        public ThanhToanController(CafebookDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId)) { return userId; }
            return 0;
        }

        [HttpPost("dat-hang")]
        public async Task<IActionResult> SubmitOrder([FromBody] DatHangRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized("Không xác định được người dùng.");

            var sanPhamItems = request.Items.Where(i => i.Loai == "SanPham").ToList();
            var sachItems = request.Items.Where(i => i.Loai == "Sach").ToList();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (sanPhamItems.Any())
                {
                    await CreateHoaDon(sanPhamItems, request.ThongTinNhanHang, userId);
                }
                if (sachItems.Any())
                {
                    await CreatePhieuThue(sachItems, request.ThongTinNhanHang, userId);
                }
                await transaction.CommitAsync();
                return Ok(new { message = "Đặt hàng thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Lỗi máy chủ nội bộ: {ex.Message}");
            }
        }

        // === SỬA LỖI: Dùng tên PascalCase từ Entities.cs ===
        private async Task CreateHoaDon(List<CartItemDto> sanPhamItems, ThanhToanInputModel info, int userId)
        {
            var spIds = sanPhamItems.Select(i => i.Id).ToList();
            var productsInDb = await _context.SanPhams.Where(sp => spIds.Contains(sp.IdSanPham)).ToListAsync();
            decimal tongTien = 0;

            var hoaDon = new HoaDon
            {
                IdKhachHang = userId,
                // SỬA LỖI CS0037: Đổi từ 'null' sang '0' vì IdNhanVien là 'int', không phải 'int?'.
                // Lý tưởng nhất, bạn nên sửa Entities.cs, đổi 'int IdNhanVien' thành 'int? IdNhanVien' (nullable)
                IdNhanVien = 0, // Web online nên null (đã sửa Entities)
                ThoiGianTao = DateTime.Now, // Sửa lỗi CS0117
                TrangThai = "Chờ xác nhận",
                PhuongThucThanhToan = info.PhuongThucThanhToan, // Sửa lỗi CS0117
                LoaiHoaDon = "Giao hàng",
                DiaChiGiaoHang = info.DiaChi, // Sửa lỗi CS0117
                SoDienThoaiGiaoHang = info.SoDienThoai, // Sửa lỗi CS0117
                GhiChu = info.GhiChu
            };

            foreach (var item in sanPhamItems)
            {
                var product = productsInDb.FirstOrDefault(sp => sp.IdSanPham == item.Id);
                if (product == null) throw new Exception($"Sản phẩm ID {item.Id} không tồn tại.");
                var thanhTienItem = product.GiaBan * item.SoLuong;
                tongTien += thanhTienItem;
                hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                {
                    IdSanPham = item.Id,
                    SoLuong = item.SoLuong,
                    DonGia = product.GiaBan,
                    ThanhTien = thanhTienItem
                });
            }

            hoaDon.TongTienGoc = tongTien; // Sửa lỗi CS1061
            _context.HoaDons.Add(hoaDon);
            await _context.SaveChangesAsync();
        }

        private async Task CreatePhieuThue(List<CartItemDto> sachItems, ThanhToanInputModel info, int userId)
        {
            var sachIds = sachItems.Select(i => i.Id).ToList();

            // SỬA LỖI CS0103:
            var booksInDb = await _context.Sachs
                .Where(s => sachIds.Contains(s.IdSach)) // Thay 'item.Id' bằng 'sachIds.Contains'
                .ToListAsync();

            decimal tongCoc = 0;

            var phieuThue = new PhieuThueSach
            {
                IdKhachHang = userId,
                // SỬA LỖI CS0037: Đổi từ 'null' sang '0' vì IdNhanVien là 'int', không phải 'int?'.
                // Lý tưởng nhất, bạn nên sửa Entities.cs, đổi 'int IdNhanVien' thành 'int? IdNhanVien' (nullable)
                IdNhanVien = 0, // Web online nên null (đã sửa Entities)
                NgayThue = DateTime.Now, // Sửa lỗi CS0117
                TrangThai = "Chờ xác nhận",
                // SỬA LỖI CS0117: Thuộc tính 'DiaChiGiaoHang' không tồn tại trên 'PhieuThueSach'.
                // DiaChiGiaoHang = info.DiaChi,
                // SỬA LỖI CS0117: Thuộc tính 'PhuongThucThanhToan' không tồn tại trên 'PhieuThueSach'.
                // PhuongThucThanhToan = info.PhuongThucThanhToan
            };

            foreach (var item in sachItems)
            {
                var book = booksInDb.FirstOrDefault(s => s.IdSach == item.Id);
                if (book == null) throw new Exception($"Sách ID {item.Id} không tồn tại.");
                if (book.SoLuongHienCo < 1) throw new Exception($"Sách '{book.TenSach}' đã hết.");

                book.SoLuongHienCo -= 1;
                tongCoc += book.GiaBia ?? 0;
                phieuThue.ChiTietPhieuThues.Add(new ChiTietPhieuThue
                {
                    IdSach = item.Id,
                    TienCoc = book.GiaBia ?? 0
                });
            }

            phieuThue.TongTienCoc = tongCoc; // Sửa lỗi CS1061

            _context.PhieuThueSachs.Add(phieuThue);
            await _context.SaveChangesAsync();
        }
    }
}