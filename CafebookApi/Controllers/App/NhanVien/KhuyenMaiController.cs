using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/khuyenmai")]
    [ApiController]
    public class KhuyenMaiController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public KhuyenMaiController(CafebookDbContext context)
        {
            _context = context;
        }

        [HttpGet("available/{idHoaDon}")]
        public async Task<IActionResult> GetAvailablePromotions(int idHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;

            // === SỬA LỖI TRANSLATE ===
            // 1. Tải toàn bộ KM về bộ nhớ (Client Evaluation)
            var allKms = await _context.KhuyenMais.ToListAsync();

            var resultList = new List<KhuyenMaiHienThiDto>();

            // 2. Dùng C# để lọc (Where) thay vì SQL
            foreach (var km in allKms)
            {
                // YÊU CẦU: "các khuyến mãi Tạm dừng , Hết hạn sẽ không hiển thị"
                if (!string.Equals(km.TrangThai, "Hoạt động", StringComparison.OrdinalIgnoreCase) ||
                    km.NgayBatDau > now ||
                    km.NgayKetThuc < now)
                {
                    continue; // Bỏ qua nếu không hợp lệ về trạng thái/ngày
                }

                var (isEligible, reason, discountValue) = CheckEligibility(km, hoaDon, now);

                var dto = new KhuyenMaiHienThiDto
                {
                    IdKhuyenMai = km.IdKhuyenMai,
                    MaKhuyenMai = km.MaKhuyenMai,
                    TenChuongTrinh = km.TenChuongTrinh,
                    DieuKienApDung = string.IsNullOrEmpty(km.DieuKienApDung) ? km.MoTa : km.DieuKienApDung,
                    LoaiGiamGia = km.LoaiGiamGia,
                    GiaTriGiam = km.GiaTriGiam,
                    GiamToiDa = km.GiamToiDa,

                    IsEligible = isEligible,
                    IneligibilityReason = reason,
                    CalculatedDiscount = discountValue
                };
                resultList.Add(dto);
            }
            // === KẾT THÚC SỬA LỖI ===

            return Ok(resultList
                .OrderByDescending(k => k.IsEligible)
                .ThenByDescending(k => k.CalculatedDiscount)
                .ThenBy(k => k.TenChuongTrinh));
        }

        // Sửa: Hàm kiểm tra trả về 3 giá trị
        private (bool IsEligible, string? Reason, decimal CalculatedDiscount) CheckEligibility(KhuyenMai km, HoaDon hoaDon, DateTime now)
        {
            decimal calculatedDiscount = 0m;

            // Logic dựa trên mô tả của bạn
            if (!string.Equals(km.TrangThai, "Hoạt động", StringComparison.OrdinalIgnoreCase))
                return (false, "Khuyến mãi đã bị tạm ngưng.", 0);
            if (km.NgayBatDau > now)
                return (false, $"Chưa tới ngày (Bắt đầu từ {km.NgayBatDau:dd/MM}).", 0);
            if (km.NgayKetThuc < now)
                return (false, $"Đã hết hạn (Kết thúc lúc {km.NgayKetThuc:dd/MM}).", 0);
            if (km.SoLuongConLai.HasValue && km.SoLuongConLai <= 0)
                return (false, "Đã hết lượt sử dụng.", 0);
            if (km.HoaDonToiThieu.HasValue && km.HoaDonToiThieu > 0 && hoaDon.TongTienGoc < km.HoaDonToiThieu.Value)
                return (false, $"Cần hóa đơn tối thiểu {km.HoaDonToiThieu.Value:N0} đ.", 0);
            if (km.GioBatDau.HasValue && km.GioKetThuc.HasValue && (now.TimeOfDay < km.GioBatDau || now.TimeOfDay > km.GioKetThuc))
                return (false, $"Chỉ áp dụng trong khung giờ {km.GioBatDau} - {km.GioKetThuc}.", 0);

            if (!string.IsNullOrWhiteSpace(km.NgayTrongTuan))
            {
                string homNay = "";
                switch (now.DayOfWeek)
                {
                    case DayOfWeek.Monday: homNay = "Thứ 2"; break;
                    case DayOfWeek.Tuesday: homNay = "Thứ 3"; break;
                    case DayOfWeek.Wednesday: homNay = "Thứ 4"; break;
                    case DayOfWeek.Thursday: homNay = "Thứ 5"; break;
                    case DayOfWeek.Friday: homNay = "Thứ 6"; break;
                    case DayOfWeek.Saturday: homNay = "Thứ 7"; break;
                    case DayOfWeek.Sunday: homNay = "Chủ nhật"; break;
                }
                var ngayHopLe = km.NgayTrongTuan.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim());
                if (!ngayHopLe.Contains(homNay, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"Chỉ áp dụng vào {km.NgayTrongTuan}.", 0);
                }
            }

            // Tính toán giá trị giảm
            decimal tongTienGocChoKM = hoaDon.TongTienGoc;
            if (km.IdSanPhamApDung.HasValue)
            {
                if (!hoaDon.ChiTietHoaDons.Any(c => c.IdSanPham == km.IdSanPhamApDung.Value))
                {
                    var sp = _context.SanPhams.Find(km.IdSanPhamApDung.Value);
                    return (false, $"Chỉ áp dụng cho sản phẩm '{sp?.TenSanPham}'.", 0);
                }
                // Nếu có, chỉ tính tiền các SP đó
                tongTienGocChoKM = hoaDon.ChiTietHoaDons
                    .Where(c => c.IdSanPham == km.IdSanPhamApDung.Value)
                    .Sum(c => c.ThanhTien);
            }

            // Tính toán
            if (string.Equals(km.LoaiGiamGia, "PhanTram", StringComparison.OrdinalIgnoreCase))
            {
                calculatedDiscount = tongTienGocChoKM * (km.GiaTriGiam / 100);
                if (km.GiamToiDa.HasValue && calculatedDiscount > km.GiamToiDa.Value)
                {
                    calculatedDiscount = km.GiamToiDa.Value;
                }
            }
            else // SoTien
            {
                calculatedDiscount = km.GiaTriGiam;
            }

            return (true, null, calculatedDiscount); // Hợp lệ
        }
    }
}